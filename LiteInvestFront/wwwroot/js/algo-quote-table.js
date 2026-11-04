class VirtualQuoteScrollingTable {
    constructor(config) {
        this.scrollCallback = config.scrollCallback;
        this.priceRangeChangeCallback = config.priceRangeChangeCallback;
        this.cancelOrderCallback = config.cancelOrderCallback;
        this.orderClickLeftCallback = config.orderClickLeftCallback;
        this.orderClickRightCallback = config.orderClickRightCallback;

        this.datas = config.items === undefined ? [] : config.items;
        this.windowId = config.windowId;
        this.secId = config.secId;
        this._automicCentering = config.automicCentering;
        this._priceStep = config.priceStep;
        this.containerClass = config.containerClass;

        this.itemHeight = config.itemHeight;
        this.totalRows = config.totalRows;

        this.initActiveOrders = config.activeOrders;

        //const windowsDiv = document.getElementById(config.windowId);
        //const windowsPanel = windowsDiv.getElementsByClassName(config.containerWrapperClass)[0];
        //this.container = windowsPanel.getElementsByClassName(config.containerClass)[0];
        //this.container = document.querySelector(`#${config.windowId} .${config.containerClass}`);
        this.windows = document.getElementById(config.windowId);
        var escapedWindowId = CSS.escape(config.windowId);
        this.container = document.querySelector(`#${escapedWindowId} .${config.containerClass}`);

        this.containerBound = this.container.getBoundingClientRect();

        const totalHeight = this.itemHeight * this.totalRows;
        this.scroller = this._createScroller(totalHeight);
        this.tableBody = this._createBody();
        this.footerPNL = this._createFooterPNL();
        this.tableBody.appendChild(this.scroller);
        this.container.appendChild(this.tableBody);
        this.container.appendChild(this.footerPNL);
        
        this._observeBodyResize();
        
        this.fromPos = 0;
        this.prevScrollTop = 0;
        this._isMouseOver = false;

        this._bestBidIndex = 0;
        this._bestAskPrice = 0;
        this._bestBidPrice = 0;
        this._scale = 0;
        this._maxVisiblePrice = 0;
        this._minVisiblePrice = 0;
        this._pnlVisibleType = 0; // P/R/Percent

        this.pnlP = '';
        this.pnlR = '';
        this.pnlPercent = '';

        this._calcScreenItemsLen();

        this.scrollerHandler = this._onScroll.bind(this);
        this.tableBody.addEventListener('scroll', this.scrollerHandler);

        this.footerPNLClickHandler = this._onClickFooterPNL.bind(this);
        this.footerPNL.addEventListener('click', this.footerPNLClickHandler);
    }

    destroy() {

        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }

        this.tableBody.removeEventListener("scroll", this.scrollerHandler);
        this.footerPNL.removeEventListener("click", this.footerPNLClickHandler);

        // DO NOT REMOVE FOR FUTURE OPTIMIZATION
        //this.scroller.replaceChildren();
        //this.tableBody.replaceChildren();
        //this.footerPNL.replaceChildren();
        //this.container.replaceChildren();

        this.scrollCallback = null;
        this.cancelOrderCallback = null;
        this.orderClickLeftCallback = null;
        this.orderClickRightCallback = null;

        this.scrollerHandler = null;
        this.footerPNLClickHandler = null;

        this.datas = null;
        this.secId = null;
        this.windowId = null;
        this.containerClass = null;
        this.containerBound = null;

        this.scroller = null;
        this.tableBody = null;
        this.footerPNL = null;
        this.gutterHandlers = null;
        this.windows = null;
        this.container = null;

        this.pnlP = null;
        this.pnlR = null;
        this.pnlPercent = null;
    }

    _observeBodyResize() {
        this.resizeObserver = new ResizeObserver(() => {
            this._bodyResizing();
        });

        this.resizeObserver.observe(this.tableBody);
    }

    _calcScreenItemsLen() {
        const realHeight = this.containerBound.height;
        this.screenItemsLen = Math.ceil(realHeight / this.itemHeight) + 1;
        this.halfScreenItemsLen = this.screenItemsLen / 2 | 0;
        this.cachedItemsLen = this.screenItemsLen * 3;
    }

    _createBody() {
        const tableBody = document.createElement('div');
        tableBody.className = 'algo-body';

        const table = document.createElement('div');
        table.className = 'algo-table';
        tableBody.appendChild(table);
        this.table = table;

        return tableBody;
    }

    _createFooterPNL() {
        const footer = document.createElement('div');
        footer.classList.add('algo-footer-pnl');
        footer.classList.add('algo-footer-pnl-pnl');
        footer.classList.add('d-none');
        footer.classList.add('noselect');
        return footer;
    }

    _createScroller(height) {
        const scroller = document.createElement('div');
        scroller.style.height = height + 'px';
        scroller.style.left = 0;
        scroller.style.opacity = 0;
        scroller.style.position = 'absolute';
        scroller.style.top = 0;
        scroller.style.width = '1px';
        return scroller;
    }

    _onClickFooterPNL() {
        this._pnlVisibleType = (this._pnlVisibleType + 1) % 3; // P/R/Percent
        this.footerPNL.textContent = this._pnlVisibleType == 0 ? `${this.pnlP.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlP')}` :
            this._pnlVisibleType == 1 ? `${this.pnlR.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlR')}` :
                `${this.pnlPercent.toLocaleString(undefined, { maximumFractionDigits: 3 })}%`;
    }

    _onScroll(e) {
        let lastRepaintY;
        let maxBuffer = this.screenItemsLen * this.itemHeight;

        const prevScrollTop = this.tableBody.scrollTop;
        const scrollTop = e.target.scrollTop;

        let first = 0;

        //console.log("quote - scroll: prevTop = " + prevScrollTop + " currentTop = " + scrollTop + " force = " + this.forceSynching);

        if (!this.forceSynching || prevScrollTop != scrollTop) {
            first = parseInt(scrollTop / this.itemHeight) - this.screenItemsLen; // first row index in visible area
            first = first < 0 ? 0 : first;

            this.upperVisibleIndex = first;
            if (!lastRepaintY || Math.abs(scrollTop - lastRepaintY) > maxBuffer) {
                this._renderChunk(first, this.cachedItemsLen);
                lastRepaintY = scrollTop;
            }
        }

        if (this.forceSynching) {
            this.forceSynching = false;
            return;
        }

        this.scrollCallback(this.windowId, scrollTop, first, this.screenItemsLen);

        e.preventDefault && e.preventDefault();
    }

    _renderChunk(upperVisibleIndex, howMany) {
        const currentScrollTop = this.tableBody.scrollTop;

        if (this.datas.length == 0) return;

        const container = this.table;
        const visibleCount = Math.min(upperVisibleIndex + howMany, this.totalRows) - upperVisibleIndex;

        // Make sure we have enough recycled rows
        while (container.children.length < visibleCount) {
            const row = document.createElement('div');
            row.classList = 'table-row noselect';
            const spanVolume = document.createElement('span');
            const spanPrice = document.createElement('span');
            const spanRestVolume = document.createElement('span');
            const spanCancel = document.createElement('span');
            row.appendChild(spanVolume);
            row.appendChild(spanPrice);
            row.appendChild(spanRestVolume);
            row.appendChild(spanCancel);
            container.appendChild(row);
        }

        // Update or reuse rows
        let dataN = this.datas;

        const maxVolume = 9999;
        const maxVolume2 = 9999;

        let bestAskIndex = -1;
        let bestBidIndex = -1;

        // First pass to identify positions
        for (let i = 0; i < visibleCount; i++) {
            const rowIndex = upperVisibleIndex + i;
            const data = dataN[rowIndex];

            if (data.bid !== 0 && data.ask !== 0) {
                bestBidIndex = i;
                bestAskIndex = -1;
                break;
            }

            if (data.bid < data.ask && (data.bid !== 0 || data.ask !== 0)) {
                bestAskIndex = i;
            }
            if (bestBidIndex === -1 && data.bid > data.ask && (data.bid !== 0 || data.ask !== 0)) {
                bestBidIndex = i; // This will keep updating to the last valid occurrence
            }
        }

        // Second pass to apply flags
        for (let i = 0; i < visibleCount; i++) {
            const rowIndex = upperVisibleIndex + i;
            const row = container.children[i];
            const spanVolume = row.children[0];
            const spanPrice = row.children[1];
            const spanRestVolume = row.children[2];
            const spanCancel = row.children[3];

            const data = dataN[rowIndex];

            let volume;
            let volumeStr;
            let state;
            let isBestBid = false;
            let isBestAsk = false;
            const price = data.price;

            if (data.bid !== 0 && data.ask !== 0) {
                volume = data.volumeSum;
                volumeStr = `${data.bid.toLocaleString()} | ${data.ask.toLocaleString()}`;
                state = 2;
                isBestBid = true;
                isBestAsk = true;
            } else {
                volume = data.volume;
                volumeStr = volume.toLocaleString();
                state = volume === 0 ? 0 : data.bid < data.ask ? 1 : 2;

                if (state !== 0) {
                    isBestBid = (i === bestBidIndex);  // First bid < ask
                    isBestAsk = (i === bestAskIndex);  // Last bid > ask
                }
            }

            if (this.activeOrderMap) {
                const orderAtPrice = this.activeOrderMap.get(price.toString());
                if (orderAtPrice && orderAtPrice.length > 0)
                    data.restVolume = orderAtPrice[0].volume - orderAtPrice[0].volumeExecuted;
                else
                    data.restVolume = 0;
            } else {
                data.restVolume = 0;
            }

            const restVolume = data.restVolume;

            // price
            if (row.dataset.price !== price) {
                if (!price)
                    spanPrice.textContent = '';
                else
                    spanPrice.textContent = price.toLocaleString(undefined, { maximumFractionDigits: 3 });

                row.dataset.price = price;
            }

            // rest volume
            if (row.dataset.restVolume !== restVolume) {
                if (!restVolume) {
                    spanRestVolume.textContent = '';
                    spanCancel.innerHTML = '';
                }
                else {
                    spanRestVolume.textContent = restVolume.toLocaleString();
                    if (!spanCancel.hasChildNodes()) {
                        spanCancel.addEventListener("click", function (e) {
                            e.stopPropagation();
                            this._onClickCancelOrder(e);
                        }.bind(this));
                        spanCancel.addEventListener('contextmenu', (e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            console.log("contextmenu cancel");
                        });
                        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
                        svg.setAttribute("class", "cancel-order");
                        svg.setAttribute("width", "15");
                        svg.setAttribute("height", "7");
                        svg.setAttribute("viewBox", "-2 -2 15 15");

                        const line1 = document.createElementNS("http://www.w3.org/2000/svg", "line");
                        line1.setAttribute("x1", "0");
                        line1.setAttribute("y1", "0");
                        line1.setAttribute("x2", "10");
                        line1.setAttribute("y2", "10");
                        line1.setAttribute("stroke", "red");
                        line1.setAttribute("stroke-width", "2");

                        const line2 = document.createElementNS("http://www.w3.org/2000/svg", "line");
                        line2.setAttribute("x1", "0");
                        line2.setAttribute("y1", "10");
                        line2.setAttribute("x2", "10");
                        line2.setAttribute("y2", "0");
                        line2.setAttribute("stroke", "red");
                        line2.setAttribute("stroke-width", "2");

                        svg.appendChild(line1);
                        svg.appendChild(line2);
                        spanCancel.appendChild(svg);
                    }
                }

                row.dataset.restVolume = restVolume;
            }

            if (row.dataset.rowIndex != rowIndex) {
                row.style.top = rowIndex * this.itemHeight + 'px';
                row.dataset.rowIndex = rowIndex;
            }

            // volume
            //if (spanVolume.textContent != volumeStr) {
            if (row.dataset.state !== state) {

                const background = (isBestBid && isBestAsk) ? COLOR_LOW_HIGHLIGHT :
                            isBestBid ? COLOR_LOW_HIGHLIGHT :
                            isBestAsk ? COLOR_HIGH_HIGHLIGHT :
                            state === 1 ? COLOR_HIGH : state === 2 ? COLOR_LOW : 'none';

                spanPrice.style.background = background;
                if (restVolume) {
                    spanRestVolume.style.background = background;
                } else {
                    spanRestVolume.style.background = (isBestBid && isBestAsk) ? COLOR_LOW_HIGHLIGHT : isBestBid ? COLOR_LOW_HIGHLIGHT : isBestAsk ? COLOR_HIGH_HIGHLIGHT : 'none';
                }
                row.dataset.state = state;

                if (!volume) {

                    spanVolume.textContent = '';
                    spanVolume.style.background = 'none';

                } else {

                    spanVolume.textContent = volumeStr;

                    var percentage = volume / maxVolume * 100;
                    var percentage2 = volume / maxVolume2 * 100;
                    let color1, color2;

                    //const COLOR_HIGH = '#edc6c6'; // red
                    //const COLOR_LOW = '#a0dbc6'; // green
                    //const COLOR_VOL_LOW = '#c7a805'; // yellow
                    //const COLOR_VOL_HIGH = '#c85dd9'; // purple

                    if (percentage <= 100) {
                        color1 = COLOR_VOL_LOW; // yellow
                        color2 = data.type == 1 ? COLOR_HIGH : COLOR_LOW; // data.type == Side.SELL
                    } else {

                        if (percentage2 > 100) percentage2 = 100;
                        color1 = COLOR_VOL_HIGH; // purple
                        color2 = data.type == 1 ? COLOR_HIGH : COLOR_LOW; // data.type == Side.SELL
                    }

                    
                    //spanVolume.style.background = `linear-gradient(to right, ${color1} ${percentage}%, ${color2} ${percentage2}%)`;
                    spanVolume.style.background = `linear-gradient(to right, ${color1} ${percentage}%, ${background} ${percentage2}%)`;
                }
            }

            if (!row.hasEventListeners) {
                row.addEventListener('click', function (e) {
                    if (e.target.closest('.table-row > span:last-child')) return;

                    var price;
                    if (e.target.firstElementChild == null)
                        price = e.target.parentElement.dataset.price;
                    else
                        price = e.target.dataset.price;
                    console.log(`long order: ${price}`);
                    this.onClickRowLeft(price);
                }.bind(this));
                row.addEventListener('contextmenu', function (e) {
                    if (e.target.closest('.table-row > span:last-child') || e.target.closest('.table-row > span:last-child > svg')) return;

                    e.preventDefault();
                    var price;
                    if (e.target.firstElementChild == null)
                        price = e.target.parentElement.dataset.price;
                    else
                        price = e.target.dataset.price;
                    console.log(`short order: ${price}`);
                    this.onClickRowRight(price);
                }.bind(this));
                row.hasEventListeners = true;
            }

        }

        // Remove excess rows if any
        while (container.children.length > visibleCount) {
            container.removeChild(container.lastChild);
        }

        requestAnimationFrame(() => {
            if (this.tableBody != null)
                this.tableBody.scrollTop = currentScrollTop;
        });
    }

    //row.addEventListener('mouseenter', (e) => {
    //    const topValue = row.style.top;
    //    highlightRows(topValue, true);
    //});
    //row.addEventListener('mouseleave', (e) => {
    //    const topValue = row.style.top;
    //    highlightRows(topValue, false);
    //});

    _bodyResizing() {

        var oldContainerHeight = this.containerBound.height;
        this.containerBound = this.container.getBoundingClientRect();

        if (oldContainerHeight != this.containerBound.height) {
            this._calcScreenItemsLen();

            const scrollTop = this.tableBody.scrollTop;
            let first = parseInt(scrollTop / this.itemHeight);
            first = first < 0 ? 0 : first; // upperVisibleIndex
            let last = first + this.screenItemsLen - 1;
            this._maxVisiblePrice = this.getPrice(first);
            this._minVisiblePrice = this.getPrice(last);

            if (this.priceRangeChangeCallback)
                this.priceRangeChangeCallback(this.windowId, this._minVisiblePrice, this._maxVisiblePrice);
        }
    }

    _onClickCancelOrder = (e) => {
        e.stopPropagation();

        const span = e.target.closest("span");
        if (!span) return; // Safety check

        const row = span.closest("div");
        if (!row) return;

        if (!this.activeOrderMap) return;

        let price = row.dataset.price;
        if (!price) return;
        price = parseFloat(price);

        var order = activeOrders.find(o => o.price === price);
        if (order) {
            console.log(`cancel order: ${price}`);
            this.cancelOrderCallback(order.id);
        }
    }

    onClickRowLeft(price) {
        this.orderClickLeftCallback(this.windowId, this.secId, price);
    }

    onClickRowRight(price) {
        this.orderClickRightCallback(this.windowId, this.secId, price);
    }

    ////////////////////
    updateData(newData) {
        var firstTime = this.datas.length == 0;
        this.datas = newData;
        this._renderChunk(this.upperVisibleIndex, this.cachedItemsLen);
        if (firstTime)
            this.scrollBestBidIndex();
    }

    refreshScroll() {
        this.tableBody.scrollTop = this.prevScrollTop;
    }

    _refresh() {
        this._renderChunk(this.upperVisibleIndex, this.cachedItemsLen);
    }

    syncScroll(scrollTop, firstIndex) {
        //console.log("quote - syncScroll: scrollTop = " + scrollTop);
        this.forceSynching = true;
        this.tableBody.scrollTop = scrollTop;
        this.prevScrollTop = scrollTop;
        //this.fromPos = firstIndex;
        this.upperVisibleIndex = firstIndex;
        this._renderChunk(firstIndex, this.cachedItemsLen);
    }

    //syncScrollByPriceBounds(priceMin, priceMax, deltaY) {
    //    this.prevScrollTop = this.tableBody.scrollTop;
    //    this.tableBody.scrollTop += deltaY;
    //}

    scrollBestBidIndex() {
        this.forceSynching = true;

        var firstIndex = this._bestBidIndex - this.halfScreenItemsLen;
        firstIndex = firstIndex < 0 ? 0 : firstIndex;
        //console.log("quote - scrollByIndex: firstIndex = " + firstIndex + " scrollTop = " + firstIndex * this.itemHeight);
        const scrollTop = firstIndex * this.itemHeight;
        this.tableBody.scrollTop = scrollTop;
        this.prevScrollTop = scrollTop;
        //this.fromPos = firstIndex;
        this.upperVisibleIndex = firstIndex;
        //console.log(`quote-scrollBestBidIndex ${this._bestBidIndex}`);
        this._renderChunk(firstIndex, this.cachedItemsLen);
    }

    getScreenItemsLen() {
        return this.screenItemsLen;
    }

    getScreenItemsHalfLen() {
        return this.halfScreenItemsLen;
    }

    getUpperVisibleIndex() {
        //return this.fromPos;
        return this.upperVisibleIndex;
    }

    getPrice(index) {
        if (this.datas != undefined && this.datas.length > index)
            return this.datas[index].price;
        return 0;
    }

    bestBidIndex(index) {
        if (index === undefined)
            return this._bestBidIndex;
        this._bestBidIndex = index;
    }

    bestAskPrice(price) {
        if (price === undefined)
            return this._bestAskPrice;
        this._bestAskPrice = price;
    }

    bestBidPrice(price) {
        if (price === undefined)
            return this._bestBidPrice;
        this._bestBidPrice = price;
    }

    maxVisiblePrice(price) {
        if (price === undefined)
            return this._maxVisiblePrice;
        this._maxVisiblePrice = price;
    }

    minVisiblePrice(price) {
        if (price === undefined)
            return this._minVisiblePrice;
        this._minVisiblePrice = price;
    }

    scale(_scale) {
        if (_scale === undefined)
            return this._scale;
        this._scale = _scale;
    }

    priceStep(step) {
        if (step === undefined)
            return this._priceStep;
        this._priceStep = step;
    }

    toggleAutomicCentering() {
        this._automicCentering = !this._automicCentering;
    }

    isAutomicCentering() {
        return this._automicCentering;
    }

    isMouseOver(_isMouseOver) {
        if (_isMouseOver === undefined)
            return this._isMouseOver;
        this._isMouseOver = _isMouseOver;
    }

    setFooterPNLVisibility(visible, pnlP, pnlR, pnlPercent) {
        if (this.footerPNL) {
            if (visible) {
                this.pnlP = pnlP;
                this.pnlR = pnlR;
                this.pnlPercent = pnlPercent;
                this.footerPNL.textContent = this._pnlVisibleType == 0 ? `${this.pnlP.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlP')}` :
                    this._pnlVisibleType == 1 ? `${this.pnlR.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlR')}` :
                        `${this.pnlPercent.toLocaleString(undefined, { maximumFractionDigits: 3 })}%`;

                if (pnlP >= 0) {
                    if (!this.footerPNL.classList.contains('algo-footer-profit')) {
                        this.footerPNL.classList.add('algo-footer-profit');
                        this.footerPNL.classList.remove('algo-footer-loss');
                    }
                } else {
                    if (!this.footerPNL.classList.contains('algo-footer-loss')) {
                        this.footerPNL.classList.add('algo-footer-loss');
                        this.footerPNL.classList.remove('algo-footer-profit');
                    }
                }

                if (this.footerPNL.classList.contains('d-none')) {
                    this.footerPNL.classList.remove('d-none');
                    this.tableBody.style.height = 'calc(100% - 30px)';
                }
            } else {
                if (!this.footerPNL.classList.contains('d-none')) {
                    this.pnlP = '';
                    this.pnlR = '';
                    this.pnlPercent = '';
                    this.footerPNL.textContent = '';
                    this.footerPNL.classList.add('d-none');
                    this.footerPNL.classList.remove('algo-footer-loss');
                    this.footerPNL.classList.remove('algo-footer-profit');
                    this.tableBody.style.height = '100%';
                }
            }
        }
    }

    setActiveOrders(activeOrderMap) {
        console.log("setActiveOrders", this.activeOrderMap, activeOrderMap);
        if (this.activeOrderMap === activeOrderMap) return;
        console.log(`>>> setActiveOrders`);

        this.activeOrderMap = activeOrderMap;
        this._refresh();
    }

    updateNewOrders(newOrdersMap) {

    }

    getActiveOrders() {
        return this.activeOrderMap;
    }
}
