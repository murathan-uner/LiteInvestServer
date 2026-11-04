class VirtualClusterScrollingTable {
    constructor(config) {

        this.scrollCallback = config.scrollCallback;

        this.datas = config.items === undefined ? [] : config.items;
        this.columns = config.columns;
        this.secId = config.secId;
        this.windowId = config.windowId;
        this.containerClass = config.containerClass;
        this.footerHeight = config.footerHeight;
        this.tableMinSize = config.tableMinSize;

        this.columnWidths = this._calculateColumnWidth();
        this.itemHeight = config.itemHeight;
        this.totalRows = config.totalRows;

        //const windowsDiv = document.getElementById(config.windowId);
        //const windowsPanel = windowsDiv.getElementsByClassName(config.containerWrapperClass)[0];
        //this.container = windowsPanel.getElementsByClassName(config.containerClass)[0];
        this.windows = document.getElementById(config.windowId);
        var escapedWindowId = CSS.escape(config.windowId);
        this.container = document.querySelector(`#${escapedWindowId} .${config.containerClass}`);
        this.footerTexts = this.columns.map(col => col.name);

        const totalHeight = this.itemHeight * this.totalRows;
        this.scroller = this._createScroller(totalHeight);
        this.tableBody = this._createBody();
        this.footer = this._createFooter();
        this.footerPNL = this._createFooterPNL();
        this.tableBody.appendChild(this.scroller);
        this.container.appendChild(this.tableBody);
        this.container.appendChild(this.footer);
        this.container.appendChild(this.footerPNL);

        //
        this.containerBound = this.container.getBoundingClientRect();
        this.gutterHandlers = [];
        for (let i = 0; i < 2; i++) {
            let gutter = document.createElement('div');
            //gutter.className = 'split-gutter';
            gutter.classList = 'split-gutter col-gutter';
            this.container.appendChild(gutter);

            const handler = (event) => this._startDraggingGutter(event, i);
            gutter.addEventListener('mousedown', handler);

            this.gutterHandlers.push({ gutter, handler });
            this._positionGutter(i);
        }

        this.mouseUpHandler = this._stopDraggingGutter.bind(this);
        this.mouseMoveHandler = this._dragGutter.bind(this);

        document.addEventListener("mouseup", this.mouseUpHandler);
        document.addEventListener("mousemove", this.mouseMoveHandler);

        this._observeBodyResize();
        //

        this.fromPos = 0;
        this.prevScrollTop = 0;
        this._isMouseOver = false;
        this._calcScreenItemsLen();

        this.scrollerHandler = this._onScroll.bind(this);
        this.tableBody.addEventListener('scroll', this.scrollerHandler);
    }

    destroyGutters() {
        if (this.gutterHandlers && this.gutterHandlers.length > 0) {
            this.gutterHandlers.forEach(({ gutter, handler }) => {
                gutter.removeEventListener("mousedown", handler);
                // DO NOT REMOVE FOR FUTURE OPTIMIZATION
                //gutter.remove();
            });
            this.gutterHandlers = null;
        }
    }

    destroy() {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }

        document.removeEventListener("mouseup", this.mouseUpHandler);
        document.removeEventListener("mousemove", this.mouseMoveHandler);

        this.tableBody.removeEventListener("scroll", this.scrollerHandler);
        this.footerPNL.removeEventListener("click", this.footerPNLClickHandler);

        this.destroyGutters();

        // DO NOT REMOVE FOR FUTURE OPTIMIZATION
        //this.scroller.replaceChildren();
        //this.tableBody.replaceChildren();
        //this.footer.replaceChildren();
        //this.footerPNL.replaceChildren();
        //this.container.replaceChildren();

        this.datas = null;
        this.columns = null;
        this.secId = null;
        this.windowId = null;
        this.containerClass = null;
        this.columnWidths = null;
        this.containerBound = null;

        this.footerTexts = null;
        this.scroller = null;
        this.tableBody = null;
        this.footer = null;
        this.footerPNL = null;
        this.container = null;
        this.windows = null;
        this.startWidths = null;

        this.mouseUpHandler = null;
        this.mouseMoveHandler = null;
        this.scrollerHandler = null;
        this.footerPNLClickHandler = null;
        this.gutterHandlers = null;

        this.scrollCallback = null;
    }

    _calcScreenItemsLen() {
        const realHeight = this.containerBound.height;
        this.screenItemsLen = Math.ceil((realHeight - this.footerHeight) / this.itemHeight) + 1;
        var screenItemsLen1 = Math.ceil(realHeight / this.itemHeight) + 1;
        this.halfScreenItemsLen = screenItemsLen1 / 2 | 0;
        this.cachedItemsLen = this.screenItemsLen * 3;
    }

    _positionGutter(index) {
        let tableBound = this.tables[index].getBoundingClientRect();
        this.gutterHandlers[index].gutter.style.left = `${tableBound.right - this.containerBound.left}px`;
    }

    _startDraggingGutter(e, index) {
        this.dragging = true;
        this.currentGutterIndex = index;
        this.startX = e.clientX;
        this.startWidths = [
            this.tables[index].offsetWidth,
            this.tables[index + 1].offsetWidth,
        ];

        // Disable text selection while dragging
        document.body.style.userSelect = 'none';
    }

    _dragGutter(e) {
        if (!this.dragging) return;

        let dx = e.clientX - this.startX;

        let newWidth1 = this.startWidths[0] + dx;
        let newWidth2 = this.startWidths[1] - dx;

        if (this.currentGutterIndex == 0) {
            if (newWidth1 <= this.tableMinSize) {
                // collapse 0
                this.tables[0].style.width = this.footerCells[0].style.width = '0';
                this.tables[1].style.width = this.footerCells[1].style.width = `${100 - this.columnWidths[2]}%`;

                this._positionGutter(0);
                return;
            }
        } else {
            if (this.currentGutterIndex == 1) {
                if (this.tables[0].offsetWidth == 0 && newWidth1 <= this.tableMinSize) {
                    // collapse 1
                    this.tables[1].style.width = this.footerCells[1].style.width = '0';
                    this.tables[2].style.width = this.footerCells[2].style.width = '100%';

                    this._positionGutter(1);
                    return;
                }
            }
        }

        if (newWidth2 < this.tableMinSize) {
            newWidth2 = this.tableMinSize;
            this.containerBound = this.container.getBoundingClientRect();
            if (this.currentGutterIndex == 1) {
                newWidth1 = this.containerBound.width - this.tables[0].offsetWidth - this.tableMinSize;
            } else if (this.currentGutterIndex == 0) {
                newWidth1 = this.containerBound.width - this.tables[2].offsetWidth - this.tableMinSize;
            }
        } else if (newWidth1 < 0) {
            this.containerBound = this.container.getBoundingClientRect();
            newWidth1 = this.tableMinSize;
            newWidth2 = this.containerBound.width - this.tables[0].offsetWidth - this.tableMinSize;
        }

        if (newWidth1 >= this.tableMinSize && newWidth2 >= this.tableMinSize) {
            this.tables[this.currentGutterIndex].style.width = this.footerCells[this.currentGutterIndex].style.width = `${newWidth1}px`;
            this.tables[this.currentGutterIndex + 1].style.width = this.footerCells[this.currentGutterIndex + 1].style.width = `${newWidth2}px`;

            this._positionGutter(this.currentGutterIndex); // Update gutter position
        }
    }

    _stopDraggingGutter(e) {
        if (!this.dragging) return;

        this.dragging = false;
        // Re-enable text selection
        document.body.style.userSelect = "";

        this.containerBound = this.container.getBoundingClientRect();
        this.columnWidths = this._rerecalculateColumnWidth();

        this.updateTableMetricUnit(this.currentGutterIndex);

        var windowsBound = this.windows.getBoundingClientRect();
        this.container.style.width = `${this.containerBound.width / windowsBound.width * 100}%`;

        if (this.startWidths[0] > 0 && this.tables[this.currentGutterIndex].offsetWidth == 0) {
            // all tables are collapsed
        }
        this.startWidths = null;
        this.currentGutterIndex = -1;
    }

    _bodyResizing() {

        var oldContainerHeight = this.containerBound.height;
        this.containerBound = this.container.getBoundingClientRect();
        var newWidth = this.containerBound.width;
        //console.log("cluster - bodyResizing: w = " + newWidth);

        if (oldContainerHeight != this.containerBound.height)
            this._calcScreenItemsLen();

        var distributionFromIndex = -1;

        if (newWidth < this.tableMinSize) {
            // collase 2, so all
            this.columnWidths[2] = 0;
            this.tables[2].style.width = this.footerCells[2].style.width = '0';

            this.container.style.width = '0';
            this.containerBound = this.container.getBoundingClientRect();

            this.gutterHandlers.forEach((_, i) => {
                this._positionGutter(i);
            });

        } else if (newWidth < this.tableMinSize * 3) {

            if (this.columnWidths[0] > 0) {

                // collapse 0, 1
                this.columnWidths[0] = 0;
                this.columnWidths[1] = 0;
                this.columnWidths[2] = 100;
                this.tables[0].style.width = this.footerCells[0].style.width = '0';
                this.tables[1].style.width = this.footerCells[1].style.width = '0';
                this.tables[2].style.width = this.footerCells[2].style.width = '100%';

                this._positionGutter(0);
                this._positionGutter(1);

            } else if (this.columnWidths[1] > 0) {

                // collapse 1
                this.columnWidths[1] = 0;
                this.columnWidths[2] = 100;
                this.tables[1].style.width = this.footerCells[1].style.width = '0';
                this.tables[2].style.width = this.footerCells[2].style.width = '100%';

                this._positionGutter(1);

            } else {
                // already collapsed 1, 0
                this.columnWidths[2] = 100;
                this.tables[2].style.width = this.footerCells[2].style.width = '100%';
            }

        } else if (newWidth < this.tableMinSize * 3) {

            if (this.columnWidths[0] > 0 && this.columnWidths[1] > 0) {

                // collapse 0
                this.columnWidths[0] = 0;
                this.columnWidths[1] = 100 - this.columnWidths[2];
                this.tables[0].style.width = this.footerCells[0].style.width = '0';
                this.tables[1].style.width = this.footerCells[1].style.width = `${this.columnWidths[1]}%`;

                this.gutterHandlers.forEach((_, i) => {
                    this._positionGutter(i);
                });

            } else if (this.columnWidths[0] == 0 && this.columnWidths[1] > 0) {

                // already collapsed 0
                distributionFromIndex = 1;

            } else {
                // already collapsed 1, 0
                this.columnWidths[2] = 100;
                this.tables[2].style.width = this.footerCells[2].style.width = '100%';
            }

        } else {

            distributionFromIndex = 0;
        }

        if (distributionFromIndex != -1) {
            let columnPixelWidths = [];
            this.tables.forEach((item, i) => {
                columnPixelWidths[i] = item.offsetWidth;
            });

            // distribute widths if small sized table detected

            var smallWidthIndexs = [];
            columnPixelWidths.forEach((item, i) => {
                if (item < this.tableMinSize) {
                    smallWidthIndexs.push(i);
                }
            });
            if (smallWidthIndexs.length > 0) {
                // small sized table(s) detected

                var columnPixelWidthsSum = newWidth;
                smallWidthIndexs.forEach((item) => {
                    if (this.columnWidths[item] != 0) {
                        columnPixelWidthsSum -= this.tableMinSize;
                    }
                });

                var columnWidthsSum = 0; // percentage sum
                this.columnWidths.forEach((item, i) => {
                    if (smallWidthIndexs.indexOf(i) == -1) {
                        columnWidthsSum += item;
                    }
                });
                this.tables.forEach((item, i) => {
                    if (smallWidthIndexs.indexOf(i) == -1) {
                        // item.style.width = this.footerCells[i].style.width = `${columnPixelWidthsSum * this.columnWidths[i] / columnWidthsSum}px`;
                        item.style.width = this.footerCells[i].style.width = `${100 / this.containerBound.width * (columnPixelWidthsSum * this.columnWidths[i] / columnWidthsSum)}%`;
                    } else {
                        if (this.columnWidths[smallWidthIndexs[i]] != 0) {
                            // item.style.width = this.footerCells[i].style.width = `${this.tableMinSize}px`;
                            item.style.width = this.footerCells[i].style.width = `${100 / this.containerBound.width * this.tableMinSize}%`;
                        }
                    }
                });

                this.columnWidths = this._rerecalculateColumnWidth();
            } else {

                for (let i = distributionFromIndex; i < 2; i++) {
                    this.tables[i].style.width = this.footerCells[i].style.width = `${this.columnWidths[i]}%`;
                }
            }

            for (let i = distributionFromIndex; i < 2; i++) {
                this._positionGutter(i);
            }
        }
    }

    _observeBodyResize() {
        this.resizeObserver = new ResizeObserver(() => {
            this._bodyResizing();
        });

        this.resizeObserver.observe(this.tableBody);
    }

    _createBody() {
        const tableBody = document.createElement('div');
        tableBody.className = 'algo-body';

        let tables = [];
        this.columns.forEach((item, i) => {
            const table = document.createElement('div');
            table.className = 'algo-table';
            table.style.width = `${this.columnWidths[i]}%`;
            tableBody.appendChild(table);
            tables[i] = table;
        });
        this.tables = tables;

        return tableBody;
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

    _onScroll(e) {

        let lastRepaintY;
        let maxBuffer = this.screenItemsLen * this.itemHeight;
        //if (!isScrolling) {
        //    isScrolling = true;
        //    onScrollStart();
        //}

        //clearTimeout(scrollTimeout);

        //scrollTimeout = setTimeout(() => {
        //    isScrolling = false;
        //    onScrollEnd();
        //}, 150);

        const prevScrollTop = this.tableBody.scrollTop;
        const scrollTop = e.target.scrollTop;

        let first = 0;

        //console.log("cluster - scroll: prevTop = " + prevScrollTop + " currentTop = " + scrollTop + " force = " + this.forceSynching);

        if (!this.forceSynching || prevScrollTop != scrollTop) {
            first = parseInt(scrollTop / this.itemHeight) - this.screenItemsLen;
            first = first < 0 ? 0 : first;
            this.upperVisibleIndex = first;
            if (!lastRepaintY || Math.abs(scrollTop - lastRepaintY) > maxBuffer) {
                this._renderChunk(this.tables, first, this.cachedItemsLen);
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

    // Optimized for DOM
    _renderChunk(nodes, upperVisibleIndex, howMany, index) {
        let currentScrollTop = this.tableBody.scrollTop;

        //let low = 0;
        //let up = nodes.length;
        //if (index >= 0) {
        //    low = index;
        //    up = index + 1;
        //}

        //if (this.datas.length < up) return;
        if (this.datas.length < 3) return;

        //for (let n = low; n < up; n++) {
        for (let n = 0; n < 3; n++) {
            const container = nodes[n];
            const visibleCount = Math.min(upperVisibleIndex + howMany, this.totalRows) - upperVisibleIndex;

            // 1. Ensure we have enough "recycled" rows
            var rowCount = container.children.length;
            while (rowCount < visibleCount) {
                const row = document.createElement('div');
                row.classList = 'table-row noselect';
                const span = document.createElement('span');
                row.appendChild(span);
                container.appendChild(row);
                rowCount++;
            }

            const dataN = this.datas[this.datas.length - n - 1];

            // 2. Recycle rows: reposition & reuse existing ones
            if (dataN.length < visibleCount) {

                for (let i = 0; i < visibleCount; i++) {
                    const rowIndex = upperVisibleIndex + i;
                    const row = container.children[i];
                    const span = row.firstChild;

                    span.textContent = '';
                    row.style.top = rowIndex * this.itemHeight + 'px';
                    //row.classList.remove('high', 'low');

                    row.dataset.rowIndex = rowIndex;
                }

            } else {

                for (let i = 0; i < visibleCount; i++) {
                    const rowIndex = upperVisibleIndex + i;
                    const row = container.children[i];
                    const span = row.firstChild;
                    const data = dataN[rowIndex];

                    let volumeStr;
                    let state;

                    if (data.bid != 0 && data.ask != 0) {
                        //const volume = data.volumeSum;
                        volumeStr = `${data.bid.toLocaleString()} | ${data.ask.toLocaleString()}`;
                        state = 2;
                    } else {
                        const volume = data.volume;
                        volumeStr = !volume ? '' : volume.toLocaleString();
                        state = !volume ? 0 : data.bid < data.ask ? 1 : 2;
                    }

                    if (span.textContent != volumeStr) {
                        span.textContent = volumeStr;
                    }

                    // TODO - TEST
                    //span.textContent = rowIndex;

                    if (row.dataset.rowIndex != rowIndex) {
                        row.style.top = rowIndex * this.itemHeight + 'px';
                        row.dataset.rowIndex = rowIndex;
                    }

                    if (row.dataset.state != state) {
                        row.classList.remove('high', 'low');
                        if (state === 1) row.classList.add('high');
                        if (state === 2) row.classList.add('low');
                        // DO NOT USE SAPN BACKGROUND HERE
                        //span.style.background = state === 1 ? COLOR_HIGH : state === 2 ? COLOR_LOW : 'none';
                        row.dataset.state = state;
                    }
                }

            }

            // 3. Optional: remove excess recycled rows if needed
            rowCount = container.children.length;
            while (rowCount > visibleCount) {
                container.removeChild(container.lastChild);
                rowCount--;
            }
        }

        requestAnimationFrame(() => {
            if (this.tableBody != null)
                this.tableBody.scrollTop = currentScrollTop;
        });
    }

    _calculateColumnWidth() {
        return this.columns.map(() => 100 / this.columns.length);
    }

    _rerecalculateColumnWidth() {
        if (this.containerBound.width == 0) {
            return this.columns.map(() => 100 / this.columns.length);
        } else {
            return this.columns.map((column, index) => 100 / this.containerBound.width * this.tables[index].offsetWidth);
        }
    }

    _updateFooter() {
        const footerCells = this.footer.querySelectorAll('.footer-cell span');
        const n = this.footerTexts.length;
        footerCells.forEach((span, i) => {
            span.innerHTML = this.footerTexts[n - i - 1];
        });
    }

    _createFooter() {
        const footer = document.createElement('div');
        footer.className = 'algo-footer';

        let footerCells = [];
        this.footerTexts.forEach((text, i) => {
            const footerCell = document.createElement('div');
            const footerName = document.createElement('span');
            footerName.innerHTML = text;
            footerCell.appendChild(footerName);
            footerCell.className = 'footer-cell';
            footerCell.style.width = `${this.columnWidths[i]}%`;
            footer.appendChild(footerCell);
            footerCells[i] = footerCell;
        });
        this.footerCells = footerCells;

        return footer;
    }

    _createFooterPNL() {
        const footer = document.createElement('div');
        footer.classList.add('algo-footer-pnl');
        footer.classList.add('algo-footer-pnl-price');
        footer.classList.add('d-none');
        footer.classList.add('noselect');
        return footer;
    }

    updateData(newData, index) {
        //console.log("new " + index);

        if (index >= 0)
            this.datas[index] = newData;
        else
            this.datas = newData;

        //this._renderChunk(this.tables, this.fromPos, this.cachedItemsLen, index);
        this._renderChunk(this.tables, this.upperVisibleIndex, this.cachedItemsLen, index);
    }

    updateDataBatch(newData, indics) {
        if (newData && indics && newData.length == indics.length) {
            indics.forEach((index, i) => {
                this.datas[index] = newData[i];
            });

            //this._renderChunk(this.tables, this.fromPos, this.cachedItemsLen, index);
            this._renderChunk(this.tables, this.upperVisibleIndex, this.cachedItemsLen, index);
        }
    }

    refreshScroll() {
        this.tableBody.scrollTop = this.prevScrollTop;
    }

    updateFooterText(newFooterTexts) {
        if (!Array.isArray(newFooterTexts) || newFooterTexts.length !== this.footerTexts.length) {
            console.error('Invalid footer text format');
            return;
        }

        this.footerTexts = newFooterTexts;
        this._updateFooter();
    }

    syncScroll(scrollTop, firstIndex) {
        //console.log("cluster - syncScroll: scrollTop = " + scrollTop);
        this.forceSynching = true;
        this.tableBody.scrollTop = scrollTop;
        this.prevScrollTop = scrollTop;
        //this.fromPos = firstIndex;
        this.upperVisibleIndex = firstIndex;
        this._renderChunk(this.tables, firstIndex, this.cachedItemsLen);
    }

    scrollBestBidIndex(bestBidIndex, offsetToFirstIndex) {
        this.forceSynching = true;
        var firstIndex = bestBidIndex - offsetToFirstIndex;
        firstIndex = firstIndex < 0 ? 0 : firstIndex;
        //console.log("quote - scrollByIndex: firstIndex = " + firstIndex + " scrollTop = " + firstIndex * this.itemHeight);
        const scrollTop = firstIndex * this.itemHeight;
        this.tableBody.scrollTop = scrollTop;
        this.prevScrollTop = scrollTop;
        //this.fromPos = firstIndex;
        this.upperVisibleIndex = firstIndex;
        this._renderChunk(this.tables, firstIndex, this.cachedItemsLen);
    }

    updateTableMetricUnit(gutterIndex) {
        if (gutterIndex == 0) {
            this.tables[0].style.width = this.footerCells[0].style.width = `${this.columnWidths[0]}%`;
            this.tables[1].style.width = this.footerCells[1].style.width = `${this.columnWidths[1]}%`;
        } else if (gutterIndex == 1) {
            this.tables[1].style.width = this.footerCells[1].style.width = `${this.columnWidths[1]}%`;
            this.tables[2].style.width = this.footerCells[2].style.width = `${this.columnWidths[2]}%`;
        } else {
            this.columnWidths.forEach((item, i) => {
                this.tables[i].style.width = this.footerCells[i].style.width = `${item}%`;
            });
        }
    }

    isMouseOver(_isMouseOver) {
        if (_isMouseOver === undefined)
            return this._isMouseOver;
        this._isMouseOver = _isMouseOver;
    }

    setFooterPNLVisibility(visible, enterPrice) {
        if (this.footerPNL) {
            if (visible) {
                this.footerPNL.classList.remove('d-none');
                this.footerPNL.textContent = enterPrice.toLocaleString(undefined, { maximumFractionDigits: 3 });
                this.tableBody.style.height = 'calc(100% - 45px)';
                this.gutterHandlers.forEach(({ gutter, handler }) => {
                    gutter.style.bottom = '30px';
                });
            } else {
                if (!this.footerPNL.classList.contains('d-none')) {
                    this.footerPNL.classList.add('d-none');
                    this.footerPNL.textContent = '';
                    this.tableBody.style.height = 'calc(100% - 15px)';
                    this.gutterHandlers.forEach(({ gutter, handler }) => {
                        gutter.style.bottom = '0';
                    });
                }
            }
        }
    }
}
