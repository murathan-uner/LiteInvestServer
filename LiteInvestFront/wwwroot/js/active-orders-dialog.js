// Assumes AlgoDialog base class is already defined

class ActiveOrdersDialog extends AlgoDialog {

    // tab: 'active' | 'canceled' | 'executed'
    // columns: 'symbol' | 'price' | 'volume' | 'timestamp' | 'side'(tab:executed) | 'secId' | 'close'(tab:active)
    //          symbol(security.Isin) | price | volume | timestamp(TimeCreate/TimeCancel/TimeDone's timestamp') | lns(Side.Buy/Sell, tab:executed) | id(ExchangeOrderId) | 'close'(tab:active)

    constructor(data, config) {
        const activeTab = config.profile ? (config.profile.activeTab || 'active') : 'active';
        const contentHtml = ActiveOrdersDialog.buildContentHtml(activeTab);
        super({
            title: __('active_orders'),
            contentHtml,
            customClass: 'active-orders-dialog',
            resizable: true,
            profile: config.profile,
            closingCallback: config.closingCallback,
            closedCallback: config.closedCallback
        });

        this.data = data || [];
        this.secId = config.secId;
        this.windowId = config.windowId;
        this.changeTabCallback = config.changeTabCallback;
        this.cancelOrderCallback = config.cancelOrderCallback;

        this.activeTab = activeTab;
        this.sortColumn = config.profile ? config.profile.sortColumn : undefined;
        this.sortDirection = config.profile ? config.profile.sortDirection : undefined;

        if (config.profile) {
            this.columnWidthsA = config.profile.columnWidthsA;
            this.columnWidthsC = config.profile.columnWidthsC;
            this.columnWidthsE = config.profile.columnWidthsE;
        }
        this.textAligns = {
            'symbol': 'left',
            'price': 'right',
            'volume': 'right',
            'timestamp': 'left',
            'side': 'center',
            'secId': 'right',
            'close': 'center'
        };

        this.init();
    }

    static buildContentHtml(activeTab) {
        return `
            <div class="algo-dialog-tabs active-orders-dialog-tabs">
                <button class="algo-dialog-tab${activeTab === 'active' ? ' active' : ''}" data-tab="active">${__('tab_active')}</button>
                <button class="algo-dialog-tab${activeTab === 'canceled' ? ' active' : ''}" data-tab="canceled">${__('tab_canceled')}</button>
                <button class="algo-dialog-tab${activeTab === 'executed' ? ' active' : ''}" data-tab="executed">${__('tab_executed')}</button>
            </div>
            <div class="algo-dialog-table-wrapper active-orders-dialog-table-wrapper">
                <table class="algo-dialog-table noselect">
                    <thead></thead>
                    <tbody></tbody>
                </table>
                <div class="algo-dialog-table-empty d-none">${__('table_empty')}</div>
            </div>
        `;
    }

    init() {
        const base = this.dialog;
        this.tabs = base.querySelectorAll('.algo-dialog-tab');
        this.thead = base.querySelector('thead');
        this.tbody = base.querySelector('tbody');
        this.tempty = base.querySelector('.algo-dialog-table-empty');

        this.tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                if (this.activeTab === e.target.dataset.tab) return;

                this.activeTab = e.target.dataset.tab;
                this.tabs.forEach(t => t.classList.remove('active'));
                e.target.classList.add('active');
                if (this.changeTabCallback)
                    this.changeTabCallback(this.activeTab);
                this.renderTable();
            });
        });

        const dialogWidth = this.dialog.clientWidth;
        
        if (!this.columnWidthsA|| this.columnWidthsA.length !== 6 ||
            this.columnWidthsA[0] < 40 || this.columnWidthsA[1] < 40 || this.columnWidthsA[2] < 40 ||
            this.columnWidthsA[3] < 40 || this.columnWidthsA[4] < 40 || this.columnWidthsA[5] < 40) {
            this.columnWidthsA = [];
            this.columnWidthsA[0] = dialogWidth * 20 / 100; // symbol
            this.columnWidthsA[1] = dialogWidth * 12 / 100; // price
            this.columnWidthsA[2] = dialogWidth * 12 / 100; // volume
            this.columnWidthsA[4] = dialogWidth * 14 / 100; // secId
            this.columnWidthsA[5] = 40; // close

            this.columnWidthsA[3] = dialogWidth - (this.columnWidthsA[0] + this.columnWidthsA[1] + this.columnWidthsA[2] +
                this.columnWidthsA[4] + this.columnWidthsA[5]); // timestamp
        }

        if (!this.columnWidthsC || this.columnWidthsC.length !== 5 ||
            this.columnWidthsC[0] < 40 || this.columnWidthsC[1] < 40 || this.columnWidthsC[2] < 40 ||
            this.columnWidthsC[3] < 40 || this.columnWidthsC[4] < 40) {
            this.columnWidthsC = [];
            this.columnWidthsC[0] = dialogWidth * 20 / 100; // symbol
            this.columnWidthsC[1] = dialogWidth * 12 / 100; // price
            this.columnWidthsC[2] = dialogWidth * 12 / 100; // volume
            this.columnWidthsC[4] = dialogWidth * 14 / 100; // secId

            this.columnWidthsC[3] = dialogWidth - (this.columnWidthsC[0] + this.columnWidthsC[1] + this.columnWidthsC[2] +
                this.columnWidthsC[4]); // timestamp
        }

        if (!this.columnWidthsE || this.columnWidthsE.length !== 6 ||
            this.columnWidthsE[0] < 40 || this.columnWidthsE[1] < 40 || this.columnWidthsE[2] < 40 ||
            this.columnWidthsE[3] < 40 || this.columnWidthsE[4] < 40 || this.columnWidthsE[5] < 40) {
            this.columnWidthsE = [];
            this.columnWidthsE[0] = dialogWidth * 20 / 100; // symbol
            this.columnWidthsE[1] = dialogWidth * 12 / 100; // price
            this.columnWidthsE[2] = dialogWidth * 12 / 100; // volume
            this.columnWidthsE[4] = dialogWidth * 7 / 100; // side
            this.columnWidthsE[5] = dialogWidth * 14 / 100; // secId

            this.columnWidthsE[3] = dialogWidth - (this.columnWidthsE[0] + this.columnWidthsE[1] + this.columnWidthsE[2] +
                this.columnWidthsE[4] + this.columnWidthsE[5]); // timestamp
        }

        this.renderTable();
    }

    _columnWidths() {
        return this.activeTab === 'active' ? this.columnWidthsA : this.activeTab === 'canceled' ? this.columnWidthsC : this.columnWidthsE;
    }

    renderTable() {

        this.thead.innerHTML = '';
        this.tbody.innerHTML = '';

        const headerRow = document.createElement('tr');

        const createHeaderCell = (label, columnKey, index) => {
            const th = document.createElement('th');
            th.innerHTML = label;
            if (columnKey === 'secId')
                th.classList = 'sortable-header noborder-right';
            else
                th.className = 'sortable-header';
            th.style.width = `${this._columnWidths()[index]}px`;
            th.style.textAlign = this.textAligns[columnKey] || 'left';

            // Track if we're resizing
            let isResizing = false;

            if (columnKey !== 'close') {
                th.style.cursor = 'pointer';

                if (this.sortColumn === columnKey && this.sortDirection) {
                    const icon = document.createElement('span');
                    icon.className = 'sort-icon';
                    icon.innerHTML = this.sortDirection === 'asc' ? this.getSortAscSvg() : this.getSortDescSvg();
                    th.appendChild(icon);
                }

                th.addEventListener('click', (e) => {
                    // Prevent sorting if we were just resizing
                    if (isResizing) {
                        isResizing = false;
                        return;
                    }

                    if (this.sortColumn !== columnKey) {
                        this.sortColumn = columnKey;
                        this.sortDirection = 'asc';
                    } else if (this.sortDirection === 'asc') {
                        this.sortDirection = 'desc';
                    } else {
                        this.sortColumn = null;
                        this.sortDirection = null;
                    }
                    this.renderTable();
                });
            }

            if ((this.activeTab === 'active' && columnKey !== 'close' && columnKey !== 'secId') || (this.activeTab !== 'active' && columnKey !== 'secId')) {
                const resizer = document.createElement('div');
                resizer.className = 'column-resizer';

                resizer.addEventListener('mousedown', (e) => {
                    isResizing = true;
                    e.stopPropagation();
                    e.preventDefault();

                    const dialogWidth = this.dialog.clientWidth;
                    const startX = e.pageX;
                    const headers = this.headers;
                    const rows = this.rows;

                    const currentWidths = this._columnWidths();
                    const currentIndex = index;
                    const nextIndex = currentIndex + 1;

                    const onMouseMove = (moveEvent) => {
                        const dx = moveEvent.pageX - startX;
                        const newCurrentWidth = currentWidths[currentIndex] + dx;
                        let widthSum = currentWidths.reduce((sum, w, i) => i !== nextIndex ? i === currentIndex ? sum + newCurrentWidth : sum + w : sum, 0);
                        const newNextWidth = dialogWidth - widthSum;
                        const minCurrent = 40;
                        const minNext = 40;

                        if (newCurrentWidth >= minCurrent && newNextWidth >= minNext) {
                            th.style.width = `${newCurrentWidth}px`;
                            headers[nextIndex].style.width = `${newNextWidth}px`;

                            const nRows = rows.length;
                            for (let i = 0; i < nRows; i++) {
                                const cells = rows[i].children;

                                // Update current column
                                if (cells[currentIndex]) {
                                    cells[currentIndex].style.width = `${newCurrentWidth}px`;
                                }

                                // Update next column
                                if (nextIndex < cells.length) {
                                    cells[nextIndex].style.width = `${newNextWidth}px`;
                                }
                            }
                        }
                    };

                    const onMouseUp = () => {
                        // Small timeout to ensure click event doesn't fire after resize
                        setTimeout(() => { isResizing = false; }, 100);
                        document.removeEventListener('mousemove', onMouseMove);
                        document.removeEventListener('mouseup', onMouseUp);

                        for (let i = 0; i < headers.length; i++) {
                            currentWidths[i] = headers[i].offsetWidth;
                        }
                    };

                    document.addEventListener('mousemove', onMouseMove);
                    document.addEventListener('mouseup', onMouseUp, { once: true });
                });

                th.appendChild(resizer);
            }

            return th;
        }

        headerRow.appendChild(createHeaderCell(__('instrument'), 'symbol', 0));
        headerRow.appendChild(createHeaderCell(__('price'), 'price', 1));
        headerRow.appendChild(createHeaderCell(__('volume'), 'volume', 2));
        headerRow.appendChild(createHeaderCell(__('datetime'), 'timestamp', 3));
        if (this.activeTab === 'executed') {
            headerRow.appendChild(createHeaderCell(__('LnS'), 'side', 4));
            headerRow.appendChild(createHeaderCell(__('key'), 'secId', 5));
        } else if (this.activeTab === 'active') {
            headerRow.appendChild(createHeaderCell(__('key'), 'secId', 4));
            headerRow.appendChild(createHeaderCell('', 'close', 5));
        } else if (this.activeTab === 'canceled') {
            headerRow.appendChild(createHeaderCell(__('key'), 'secId', 4));
        }
        this.thead.appendChild(headerRow);

        let filtered = [...this.data];

        if (this.sortColumn && this.sortDirection) {
            const key = this.sortColumn;
            if (key === 'price' || key === 'volume' || key === 'timestamp') {
                filtered.sort((a, b) => {
                    const aValue = a[key] ?? 0;
                    const bValue = b[key] ?? 0;
                    //const aValue = parseFloat(a[key]);
                    //const bValue = parseFloat(b[key]);
                    return this.sortDirection === 'asc'
                        ? aValue - bValue
                        : bValue - aValue;
                });
            } else {
                filtered.sort((a, b) => {
                    const valueA = a[key] ?? ''; // Use empty string if null or undefined
                    const valueB = b[key] ?? ''; // Use empty string if null or undefined

                    try {
                        return this.sortDirection === 'asc'
                            ? String(valueA).localeCompare(String(valueB))
                            : String(valueB).localeCompare(String(valueA));
                    } catch (e) {
                        // Fallback comparison if localeCompare fails
                        if (this.sortDirection === 'asc') {
                            return String(valueA) < String(valueB) ? -1 : 1;
                        } else {
                            return String(valueA) > String(valueB) ? -1 : 1;
                        }
                    }
                });
            }
        }

        if (filtered.length == 0) {
            this.tempty.classList.remove('d-none');
        } else {
            this.tempty.classList.add('d-none');
            const currentWidths = this._columnWidths();

            for (const item of filtered) {
                const tr = document.createElement('tr');

                const symbolTd = document.createElement('td');
                symbolTd.textContent = item.symbol;
                symbolTd.style.width = `${currentWidths[0]}px`;
                symbolTd.style.textAlign = this.textAligns['symbol'] || 'left';
                tr.appendChild(symbolTd);

                const priceTd = document.createElement('td');
                priceTd.textContent = item.price.toLocaleString(undefined, { maximumFractionDigits: 3 });
                priceTd.style.width = `${currentWidths[1]}px`;
                priceTd.style.textAlign = this.textAligns['price'] || 'left';
                tr.appendChild(priceTd);

                const volumeTd = document.createElement('td');
                volumeTd.textContent = item.volume.toLocaleString();
                volumeTd.style.width = `${currentWidths[2]}px`;
                volumeTd.style.textAlign = this.textAligns['volume'] || 'left';
                tr.appendChild(volumeTd);

                const timestampTd = document.createElement('td');
                timestampTd.textContent = _iFormatSmartDate(item.timestamp);
                timestampTd.style.width = `${currentWidths[3]}px`;
                timestampTd.style.textAlign = this.textAligns['timestamp'] || 'left';
                tr.appendChild(timestampTd);

                if (this.activeTab === 'executed') {
                    const lnsTd = document.createElement('td');
                    lnsTd.textContent = item.side; // Buy: L | Sell: S
                    lnsTd.style.width = `${currentWidths[4]}px`;
                    lnsTd.style.textAlign = this.textAligns['side'] || 'left';
                    tr.appendChild(lnsTd);
                }

                const secIdTd = document.createElement('td');
                secIdTd.textContent = item.id;
                secIdTd.style.textAlign = this.textAligns['secId'] || 'left';
                if (this.activeTab === 'executed')
                    secIdTd.style.width = `${currentWidths[5]}px`;
                else
                    secIdTd.style.width = `${currentWidths[4]}px`;
                tr.appendChild(secIdTd);

                if (this.activeTab === 'active') {
                    const closeTd = document.createElement('td');
                    closeTd.className = 'has-button';
                    closeTd.style.width = `${currentWidths[5]}px`;
                    closeTd.style.textAlign = this.textAligns['close'] || 'left';
                    closeTd.style.padding = '0';
                    const closeSpan = document.createElement('span');
                    closeSpan.className = 'list-close-icon';
                    closeSpan.innerHTML = this.getCloseSvgRed();

                    closeSpan.onclick = (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        console.log("cancel order click");
                        if (this.cancelOrderCallback)
                            this.cancelOrderCallback(item.id);
                    };

                    closeTd.appendChild(closeSpan);
                    tr.appendChild(closeTd);
                }

                tr.className = item.side === 'L' ? "low" : "high";

                this.tbody.appendChild(tr);
            }
        }

        const headers = this.thead.querySelectorAll('th');
        const rows = this.tbody.querySelectorAll('tr');

        this.headers = headers;
        this.rows = rows;
    }

    startResize(e) {
        super.startResize(e);
        const iLastA = 5; // 6-1
        const iLastC = 5; // 5
        const iLastE = 6; // 6
        const dialogWidthA = this.dialog.clientWidth - this.columnWidthsA[iLastA]; // fixed 'close' column
        const dialogWidthC = this.dialog.clientWidth;
        const dialogWidthE = this.dialog.clientWidth;
        if (!this.startColumnWidthRatiosA) this.startColumnWidthRatiosA = [];
        if (!this.startColumnWidthRatiosC) this.startColumnWidthRatiosC = [];
        if (!this.startColumnWidthRatiosE) this.startColumnWidthRatiosE = [];

        for (let i = 0; i < iLastA; i++) {
            this.startColumnWidthRatiosA[i] = this.columnWidthsA[i] / dialogWidthA;
        }
        for (let i = 0; i < iLastC; i++) {
            this.startColumnWidthRatiosC[i] = this.columnWidthsC[i] / dialogWidthC;
        }
        for (let i = 0; i < iLastE; i++) {
            this.startColumnWidthRatiosE[i] = this.columnWidthsE[i] / dialogWidthE;
        }
    }

    resize(e) {
        const delta = super.resize(e);
        if (delta.dx > 0) {
            // expanding width
            const iLastA = 4; // 6-2
            const iLastC = 4; // 5-1
            const iLastE = 5; // 6-1
            const dialogWidthA = this.dialog.clientWidth - this.columnWidthsA[iLastA + 1]; // fixed 'close' column
            const dialogWidthC = this.dialog.clientWidth;
            const dialogWidthE = this.dialog.clientWidth;
            let widthSumA = 0;
            let widthSumC = 0;
            let widthSumE = 0;
            for (let i = 0; i < iLastA; i++) {
                widthSumA += this.columnWidthsA[i];
            }
            for (let i = 0; i < iLastC; i++) {
                widthSumC += this.columnWidthsC[i];
            }
            for (let i = 0; i < iLastE; i++) {
                widthSumE += this.columnWidthsE[i];
            }
            const lastWidthA = dialogWidthA - widthSumA;
            const lastWidthC = dialogWidthC - widthSumC;
            const lastWidthE = dialogWidthE - widthSumE;
            this.columnWidthsA[iLastA] = lastWidthA;
            this.columnWidthsC[iLastC] = lastWidthC;
            this.columnWidthsE[iLastE] = lastWidthE;

            let nRows = this.rows.length;
            if (this.activeTab === 'active') {
                this.headers[iLastA].style.width = `${lastWidthA}px`;
                for (let i = 0; i < nRows; i++) {
                    this.rows[i].cells[iLastA].style.width = `${lastWidthA}px`;
                }
            } else if (this.activeTab === 'canceled') {
                this.headers[iLastC].style.width = `${lastWidthC}px`;
                for (let i = 0; i < nRows; i++) {
                    this.rows[i].cells[iLastC].style.width = `${lastWidthC}px`;
                }
            } else if (this.activeTab === 'executed') {
                this.headers[iLastE].style.width = `${lastWidthE}px`;
                for (let i = 0; i < nRows; i++) {
                    this.rows[i].cells[iLastE].style.width = `${lastWidthE}px`;
                }
            }
        } else if (delta.dx < 0) {
            // shrinking width
            const iLastA = 4; // 6-2
            const iLastC = 4; // 5-1
            const iLastE = 5; // 6-1
            const dialogWidthA = this.dialog.clientWidth - this.columnWidthsA[iLastA + 1]; // fixed 'close' column
            const dialogWidthC = this.dialog.clientWidth;
            const dialogWidthE = this.dialog.clientWidth;
            let widthSumA = 0;
            let widthSumC = 0;
            let widthSumE = 0;
            let nRows = this.rows.length;
            for (let i = 0; i < iLastA; i++) {
                const widthA = Math.round(dialogWidthA * this.startColumnWidthRatiosA[i]);
                this.columnWidthsA[i] = widthA;
                widthSumA += widthA;
                if (this.activeTab === 'active') {
                    this.headers[i].style.width = `${widthA}px`;
                    for (let j = 0; j < nRows; j++) {
                        this.rows[j].cells[i].style.width = `${widthA}px`;
                    }
                }
            }
            for (let i = 0; i < iLastC; i++) {
                const widthC = Math.round(dialogWidthC * this.startColumnWidthRatiosC[i]);
                this.columnWidthsC[i] = widthC;
                widthSumC += widthC;
                if (this.activeTab === 'canceled') {
                    this.headers[i].style.width = `${widthC}px`;
                    for (let j = 0; j < nRows; j++) {
                        this.rows[j].cells[i].style.width = `${widthC}px`;
                    }
                }
            }
            for (let i = 0; i < iLastE; i++) {
                const widthE = Math.round(dialogWidthE * this.startColumnWidthRatiosE[i]);
                this.columnWidthsE[i] = widthE;
                widthSumE += widthE;
                if (this.activeTab === 'executed') {
                    this.headers[i].style.width = `${widthE}px`;
                    for (let j = 0; j < nRows; j++) {
                        this.rows[j].cells[i].style.width = `${widthE}px`;
                    }
                }
            }
            const widthA = dialogWidthA - widthSumA;
            const widthC = dialogWidthC - widthSumC;
            const widthE = dialogWidthE - widthSumE;
            this.columnWidthsA[iLastA] = widthA;
            this.columnWidthsC[iLastC] = widthC;
            this.columnWidthsE[iLastE] = widthE;
            if (this.activeTab === 'active') {
                this.headers[iLastA].style.width = `${widthA}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[iLastA].style.width = `${widthA}px`;
                }
            } else if (this.activeTab === 'canceled') {
                this.headers[iLastC].style.width = `${widthC}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[iLastC].style.width = `${widthC}px`;
                }
            } else if (this.activeTab === 'executed') {
                this.headers[iLastO].style.width = `${widthO}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[iLastO].style.width = `${widthO}px`;
                }
            }
        }
    }

    stopResize() {
        super.stopResize();
        this.startColumnWidthRatios = null;
    }

    // Invoked if whole data is updated
    // - triggered by initial active orders loading
    // - triggered by active tab changed
    setData(data) {
        this.data = data || [];
        this.renderTable();
    }

    // Invoked if new orders are arrived
    // - triggered by server service - SubscribeForPrivateOrders
    setNewData(newOrders) {
        if (!newOrders || newOrders.length == 0) return;

        // Determine which states to keep based on activeTab
        let validStates;
        switch (this.activeTab) {
            case 'active':
                validStates = [ORDER_STATE_ACTIVE, ORDER_STATE_PARTIAL];
                break;
            case 'canceled':
                validStates = [ORDER_STATE_CANCELED, ORDER_STATE_FAILED];
                break;
            case 'executed':
                validStates = [ORDER_STATE_EXECUTED];
                break;
            default:
                validStates = [];
        }

        // Step 1: Update existing orders (preserving order)
        const updatedData = [];
        const processedIds = new Set();

        if (this.data && this.data.length > 0) {
            for (const order of this.data) {
                const newOrder = newOrders.find(o => o.id === order.id);
                if (newOrder) {
                    // If the new order matches the tab's criteria → include it
                    if (validStates.includes(newOrder.state)) {
                        updatedData.push(newOrder);
                        processedIds.add(newOrder.id);
                    }
                    // Else: Remove it (do not push)
                } else {
                    // If no update, keep the order only if it matches the tab's criteria
                    if (validStates.includes(order.state)) {
                        updatedData.push(order);
                    }
                }
            }
        }

        // Step 2: Add new orders not already in `this.data`
        for (const order of newOrders) {
            if (!processedIds.has(order.id) && validStates.includes(order.state)) {
                updatedData.push(order);
            }
        }

        this.data = updatedData;

        this.renderTable();
    }

    getActiveTab() {
        return this.activeTab;
    }

    close() {
        this.changeTabCallback = null;
        this.cancelOrderCallback = null;
        this.startColumnWidthRatiosA = null;
        this.startColumnWidthRatiosC = null;
        this.startColumnWidthRatiosE = null;
        super.close();
    }

    getProfile() {
        var bound = super.getProfile();
        const profile = {
            bound: bound,
            activeTab: this.activeTab,
            columnWidthsA: this.columnWidthsA,
            columnWidthsC: this.columnWidthsC,
            columnWidthsE: this.columnWidthsE,
        };
        if (this.sortColumn && this.sortDirection) {
            profile.sortColumn = this.sortColumn;
            profile.sortDirection = this.sortDirection;
        }
        return profile;
    }
}
