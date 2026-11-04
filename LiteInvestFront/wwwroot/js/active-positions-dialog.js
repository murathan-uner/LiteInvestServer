// Assumes AlgoDialog base class is already defined

class ActivePositionsDialog extends AlgoDialog {

    // tab: 'opened' | 'closed'
    // columns(opened): 'symbol' | 'enterPrice' | 'volume' | 'side' | 'secId' | 'close'
    //              secName | entryPrice(averageEntry) | volume(posValue) | lns(Side.Buy/Sell) | secId | icon
    // columns(closed): 'symbol' | 'PnL' | 'volume(posValue)' | 'side' | 'enterPrice' | 'exitPrice' | 'timestamp' | 'secId'
    //              secName | pnl | volume(posValue) | lns(Side.Buy/Sell) | entryPrice(averageEntry) | exitPrice(averageExit) | timestamp(OpenTime's timestamp') | secId

    constructor(data, config) {
        const activeTab = config.profile ? (config.profile.activeTab || 'opened') : 'opened';
        const contentHtml = ActivePositionsDialog.buildContentHtml(activeTab);
        super({
            title: __('active_positions'),
            contentHtml,
            customClass: 'active-positions-dialog',
            resizable: true,
            profile: config.profile,
            closingCallback: config.closingCallback,
            closedCallback: config.closedCallback
        });

        this.data = data || [];
        this._calculatePnLAll();

        this.secId = config.secId;
        this.windowId = config.windowId;
        this.changeTabCallback = config.changeTabCallback;
        this.closePositionCallback = config.closePositionCallback;

        this.activeTab = activeTab;
        this.sortColumn = config.profile ? config.profile.sortColumn : undefined;
        this.sortDirection = config.profile ? config.profile.sortDirection : undefined;

        this.pnlVisibleType = config.profile ? (config.profile.pnlVisibleType) : 0; // 0: P | 1: R | 2: Percent

        if (config.profile) {
            this.columnWidthsO = config.profile.columnWidthsO;
            this.columnWidthsC = config.profile.columnWidthsC;
        }

        this.textAlignsO = {
            'symbol': 'left',
            'PnL': 'right',
            'enterPrice': 'right',
            'volume': 'right',
            'side': 'center',
            'secId': 'right',
            'close': 'center'
        };
        this.textAlignsC = {
            'symbol': 'left',
            'PnL': 'right',
            'volume': 'right',
            'side': 'center',
            'enterPrice': 'Right',
            'exitPrice': 'Left',
            'timestamp': 'left',
            'secId': 'right'
        };

        this.init();
    }

    static buildContentHtml(activeTab) {
        return `
            <div class="algo-dialog-tabs active-positions-dialog-tabs">
                <button class="algo-dialog-tab${activeTab === 'opened' ? ' active' : ''}" data-tab="opened">${__('tab_opened')}</button>
                <button class="algo-dialog-tab${activeTab === 'closed' ? ' active' : ''}" data-tab="closed">${__('tab_closed')}</button>
            </div>
            <div class="algo-dialog-table-wrapper active-positions-dialog-table-wrapper">
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

        // Add context menu to table
        this.tableWrapper = base.querySelector('.algo-dialog-table-wrapper');

        // Create context menu items
        this._createContextMenuItems();

        this.tableWrapper.addEventListener('contextmenu', (e) => {
            e.preventDefault();
            this._setExportMenuItemVisibility(this.activeTab === 'closed');
            this.showContextMenu(e.clientX, e.clientY);
        });

        const dialogWidth = this.dialog.clientWidth;

        if (!this.columnWidthsO || this.columnWidthsO.length !== 7 ||
            this.columnWidthsO[0] < 40 || this.columnWidthsO[1] < 40 || this.columnWidthsO[2] < 40 ||
            this.columnWidthsO[3] < 40 || this.columnWidthsO[4] < 40 || this.columnWidthsO[5] < 40 || this.columnWidthsO[6] < 40) {
            this.columnWidthsO = [];
            this.columnWidthsO[0] = Math.round(dialogWidth * 20 / 100); // symbol
            this.columnWidthsO[1] = Math.round(dialogWidth * 10 / 100); // PnL
            this.columnWidthsO[2] = Math.round(dialogWidth * 12 / 100); // enterPrice
            this.columnWidthsO[3] = Math.round(dialogWidth * 12 / 100); // volume
            this.columnWidthsO[4] = Math.round(dialogWidth * 7 / 100); // side
            this.columnWidthsO[6] = 40; // close

            this.columnWidthsO[5] = dialogWidth - (this.columnWidthsO[0] + this.columnWidthsO[1] + this.columnWidthsO[2] +
                                    this.columnWidthsO[3] + this.columnWidthsO[4] + this.columnWidthsO[6]); // secId
        }

        if (!this.columnWidthsC || this.columnWidthsC.length !== 8 ||
            this.columnWidthsC[0] < 40 || this.columnWidthsC[1] < 40 || this.columnWidthsC[2] < 40 ||
            this.columnWidthsC[3] < 40 || this.columnWidthsC[4] < 40 || this.columnWidthsC[5] < 40 ||
            this.columnWidthsC[6] < 40 || this.columnWidthsC[7] < 40) {
            this.columnWidthsC = [];
            this.columnWidthsC[0] = Math.round(dialogWidth * 14 / 100); // symbol
            this.columnWidthsC[1] = Math.round(dialogWidth * 10 / 100); // PnL
            this.columnWidthsC[2] = Math.round(dialogWidth * 10 / 100); // volume
            this.columnWidthsC[3] = Math.round(dialogWidth * 7 / 100); // side
            this.columnWidthsC[4] = Math.round(dialogWidth * 12 / 100); // enterPrice
            this.columnWidthsC[5] = Math.round(dialogWidth * 12 / 100); // exitPrice
            this.columnWidthsC[7] = Math.round(dialogWidth * 11 / 100); // secId

            this.columnWidthsC[6] = dialogWidth - (this.columnWidthsC[0] + this.columnWidthsC[1] + this.columnWidthsC[2] +
                                    this.columnWidthsC[3] + this.columnWidthsC[4] + this.columnWidthsC[5] + this.columnWidthsC[7]); // timestamp
        }

        this.renderTable();
    }

    _textAligns() {
        return this.activeTab === 'opened' ? this.textAlignsO : this.textAlignsC;
    }

    _columnWidths() {
        return this.activeTab === 'opened' ? this.columnWidthsO : this.columnWidthsC;
    }

    _createContextMenuItems() {
        // Display Profit submenu with checkable items
        this.addContextMenuItem({
            text: __('display_profit'),
            submenu: [
                {
                    text: __('points'),
                    checkable: true,
                    checked: this.pnlVisibleType === 0,
                    group: 'pnl-display',
                    action: () => this.setPnlVisibleType(0)
                },
                {
                    text: __('points_volume'),
                    checkable: true,
                    checked: this.pnlVisibleType === 1,
                    group: 'pnl-display',
                    action: () => this.setPnlVisibleType(1)
                },
                {
                    text: __('percent'),
                    checkable: true,
                    checked: this.pnlVisibleType === 2,
                    group: 'pnl-display',
                    action: () => this.setPnlVisibleType(2)
                }
            ]
        });

        this.addContextMenuSeparator();

        // Export submenu (non-checkable)
        this.addContextMenuItem({
            text: __('export'),
            submenu: [
                {
                    text: 'CSV',
                    action: () => this.exportData('csv')
                },
                {
                    text: 'HTML',
                    action: () => this.exportData('html')
                },
                {
                    text: 'XML',
                    action: () => this.exportData('xml')
                },
                {
                    text: 'Excel',
                    action: () => this.exportData('excel')
                }
            ]
        });
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
            th.style.textAlign = this._textAligns()[columnKey] || 'left';

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

            if ((this.activeTab === 'opened' && columnKey !== 'secId' && columnKey !== 'close') || (this.activeTab === 'closed' && columnKey !== 'secId')) {
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

        if (this.activeTab === 'opened') {
            headerRow.appendChild(createHeaderCell(__('instrument'), 'symbol', 0));
            headerRow.appendChild(createHeaderCell(__('PnL'), 'PnL', 1));
            headerRow.appendChild(createHeaderCell(__('price'), 'enterPrice', 2));
            headerRow.appendChild(createHeaderCell(__('volume'), 'volume', 3));
            headerRow.appendChild(createHeaderCell(__('LnS'), 'side', 4));
            headerRow.appendChild(createHeaderCell(__('key'), 'secId', 5));
            headerRow.appendChild(createHeaderCell('', 'close', 6));
        } else {
            headerRow.appendChild(createHeaderCell(__('instrument'), 'symbol', 0));
            headerRow.appendChild(createHeaderCell(__('PnL'), 'PnL', 1));
            headerRow.appendChild(createHeaderCell(__('volume'), 'volume', 2));
            headerRow.appendChild(createHeaderCell(__('LnS'), 'side', 3));
            headerRow.appendChild(createHeaderCell(__('enter_price'), 'enterPrice', 4));
            headerRow.appendChild(createHeaderCell(__('exit_price'), 'exitPrice', 5));
            headerRow.appendChild(createHeaderCell(__('datetime'), 'timestamp', 6));
            headerRow.appendChild(createHeaderCell(__('key'), 'secId', 7));
        }

        this.thead.appendChild(headerRow);

        let filtered = [...this.data];
        //let filtered = [...this.data, ...this.data, ...this.data, ...this.data, ...this.data];

        if (this.sortColumn && this.sortDirection) {
            let key = this.sortColumn;
            if (key === 'volume' ||
                key === 'enterPrice' || key === 'exitPrice' || key === 'timestamp') {
                if (key === 'volume')
                    key = this.activeTab === 'opened' ? 'curPos' : 'closedVolume';
                filtered.sort((a, b) => {
                    const aValue = a[key] ?? 0;
                    const bValue = b[key] ?? 0;
                    //const aValue = parseFloat(a[key]);
                    //const bValue = parseFloat(b[key]);
                    return this.sortDirection === 'asc'
                        ? aValue - bValue
                        : bValue - aValue;
                });
            } else if (key === 'PnL') {
                filtered.sort((a, b) => {
                    const aValue = a['pnlP'] ?? 0;
                    const bValue = b['pnlP'] ?? 0;
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
            const textAligns = this._textAligns();
            const currentWidths = this._columnWidths();

            if (this.activeTab === 'opened') {
                for (const pos of filtered) {
                    const tr = document.createElement('tr');

                    tr.dataset.id = pos.secId;

                    const symbolTd = document.createElement('td');
                    symbolTd.textContent = pos.symbol;
                    symbolTd.style.width = `${currentWidths[0]}px`;
                    symbolTd.style.textAlign = textAligns['symbol'] || 'left';
                    tr.appendChild(symbolTd);

                    const pnlTd = document.createElement('td');
                    pnlTd.textContent = this.pnlVisibleType == 0 ? `${pos.pnlP.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlP')}` :
                        this.pnlVisibleType == 1 ? `${pos.pnlR.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlR')}` :
                            `${pos.pnlPercent.toLocaleString(undefined, { maximumFractionDigits: 3 })}%`;
                    pnlTd.style.width = `${currentWidths[1]}px`;
                    pnlTd.style.textAlign = textAligns['PnL'] || 'left';
                    tr.appendChild(pnlTd);

                    const priceTd = document.createElement('td');
                    priceTd.textContent = pos.enterPrice.toLocaleString(undefined, { maximumFractionDigits: 3 });
                    priceTd.style.width = `${currentWidths[2]}px`;
                    priceTd.style.textAlign = textAligns['enterPrice'] || 'left';
                    tr.appendChild(priceTd);

                    const volumeTd = document.createElement('td');
                    volumeTd.textContent = pos.curPos.toLocaleString();
                    volumeTd.style.width = `${currentWidths[3]}px`;
                    volumeTd.style.textAlign = textAligns['volume'] || 'left';
                    tr.appendChild(volumeTd);

                    const lnsTd = document.createElement('td');
                    lnsTd.textContent = pos.side; // Buy: L | Sell: S
                    lnsTd.style.width = `${currentWidths[4]}px`;
                    lnsTd.style.textAlign = textAligns['side'] || 'left';
                    tr.appendChild(lnsTd);

                    const secIdTd = document.createElement('td');
                    secIdTd.textContent = pos.secId;
                    secIdTd.style.width = `${currentWidths[5]}px`;
                    secIdTd.style.textAlign = textAligns['secId'] || 'left';
                    tr.appendChild(secIdTd);

                    const closeTd = document.createElement('td');
                    closeTd.className = 'has-button';
                    closeTd.style.width = `${currentWidths[6]}px`;
                    closeTd.style.textAlign = textAligns['close'] || 'left';
                    closeTd.style.padding = '0';
                    const closeSpan = document.createElement('span');
                    closeSpan.className = 'list-close-icon';
                    closeSpan.innerHTML = this.getCloseSvgRed();

                    closeSpan.onclick = (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        console.log("close position click");
                        if (this.closePositionCallback) {
                            this.closePositionCallback(pos.secId);
                        }
                    };

                    closeTd.appendChild(closeSpan);
                    tr.appendChild(closeTd);

                    if (pos.pnlP !== 0)
                        tr.className = pos.pnlP > 0 ? "low" : "high";

                    this.tbody.appendChild(tr);
                }

            } else {

                for (const pos of filtered) {
                    const tr = document.createElement('tr');

                    const symbolTd = document.createElement('td');
                    symbolTd.textContent = pos.symbol;
                    symbolTd.style.width = `${currentWidths[0]}px`;
                    symbolTd.style.textAlign = textAligns['symbol'] || 'left';
                    tr.appendChild(symbolTd);

                    const pnlTd = document.createElement('td');
                    pnlTd.textContent = this.pnlVisibleType == 0 ? `${pos.pnlP.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlP')}` :
                        this.pnlVisibleType == 1 ? `${pos.pnlR.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlR')}` :
                            `${pos.pnlPercent.toLocaleString(undefined, { maximumFractionDigits: 3 })}%`;
                    pnlTd.style.width = `${currentWidths[1]}px`;
                    pnlTd.style.textAlign = textAligns['PnL'] || 'left';
                    tr.appendChild(pnlTd);

                    const volumeTd = document.createElement('td');
                    volumeTd.textContent = pos.closedVolume.toLocaleString();
                    volumeTd.style.width = `${currentWidths[2]}px`;
                    volumeTd.style.textAlign = textAligns['volume'] || 'left';
                    tr.appendChild(volumeTd);

                    const lnsTd = document.createElement('td');
                    lnsTd.textContent = pos.side; // Buy: L | Sell: S
                    lnsTd.style.width = `${currentWidths[3]}px`;
                    lnsTd.style.textAlign = textAligns['side'] || 'left';
                    tr.appendChild(lnsTd);

                    const enterPriceTd = document.createElement('td');
                    enterPriceTd.textContent = pos.enterPrice.toLocaleString(undefined, { maximumFractionDigits: 3 });
                    enterPriceTd.style.width = `${currentWidths[4]}px`;
                    enterPriceTd.style.textAlign = textAligns['enterPrice'] || 'left';
                    tr.appendChild(enterPriceTd);

                    const exitPriceTd = document.createElement('td');
                    exitPriceTd.textContent = pos.exitPrice.toLocaleString(undefined, { maximumFractionDigits: 3 });
                    exitPriceTd.style.width = `${currentWidths[5]}px`;
                    exitPriceTd.style.textAlign = textAligns['exitPrice'] || 'left';
                    tr.appendChild(exitPriceTd);

                    const timestampTd = document.createElement('td');
                    timestampTd.textContent = _iFormatSmartDate(pos.timestamp);
                    timestampTd.style.width = `${currentWidths[6]}px`;
                    timestampTd.style.textAlign = textAligns['timestamp'] || 'left';
                    tr.appendChild(timestampTd);

                    const secIdTd = document.createElement('td');
                    secIdTd.textContent = pos.secId;
                    secIdTd.style.width = `${currentWidths[7]}px`;
                    secIdTd.style.textAlign = textAligns['secId'] || 'left';
                    tr.appendChild(secIdTd);

                    if (pos.pnlP !== 0)
                        tr.className = pos.pnlP > 0 ? "low" : "high";

                    this.tbody.appendChild(tr);
                }
            }
        }

        const headers = this.thead.querySelectorAll('th');
        const rows = this.tbody.querySelectorAll('tr');
        
        this.headers = headers;
        this.rows = rows;
    }

    startResize(e) {
        super.startResize(e);
        const iLastO = 6; // 7-1
        const iLastC = 8; // 8
        const dialogWidthO = this.dialog.clientWidth - this.columnWidthsO[iLastO]; // fixed 'close' column
        const dialogWidthC = this.dialog.clientWidth;
        if (!this.startColumnWidthRatiosO) this.startColumnWidthRatiosO = [];
        if (!this.startColumnWidthRatiosC) this.startColumnWidthRatiosC = [];

        for (let i = 0; i < iLastO; i++) {
            this.startColumnWidthRatiosO[i] = this.columnWidthsO[i] / dialogWidthO;
        }
        for (let i = 0; i < iLastC; i++) {
            this.startColumnWidthRatiosC[i] = this.columnWidthsC[i] / dialogWidthC;
        }
    }

    resize(e) {
        const delta = super.resize(e);
        if (delta.dx > 0) {
            // expanding width
            const iLastO = 5; // 7-2
            const iLastC = 7; // 8-1
            const dialogWidthO = this.dialog.clientWidth - this.columnWidthsO[iLastO + 1]; // fixed 'close' column
            const dialogWidthC = this.dialog.clientWidth;
            let widthSumO = 0;
            let widthSumC = 0;
            for (let i = 0; i < iLastO; i++) {
                widthSumO += this.columnWidthsO[i];
            }
            for (let i = 0; i < iLastC; i++) {
                widthSumC += this.columnWidthsC[i];
            }
            const lastWidthO = dialogWidthO - widthSumO;
            const lastWidthC = dialogWidthC - widthSumC;
            this.columnWidthsO[iLastO] = lastWidthO;
            this.columnWidthsC[iLastC] = lastWidthC;

            let nRows = this.rows.length;
            if (this.activeTab === 'opened') {
                this.headers[iLastO].style.width = `${lastWidthO}px`;
                for (let i = 0; i < nRows; i++) {
                    this.rows[i].cells[iLastO].style.width = `${lastWidthO}px`;
                }
            } else if (this.activeTab === 'closed') {
                this.headers[iLastC].style.width = `${lastWidthC}px`;
                for (let i = 0; i < nRows; i++) {
                    this.rows[i].cells[iLastC].style.width = `${lastWidthC}px`;
                }
            }
        } else if (delta.dx < 0) {
            // shrinking width
            const iLastO = 5; // 7-2
            const iLastC = 7; // 8-1
            const dialogWidthO = this.dialog.clientWidth - this.columnWidthsO[iLastO + 1]; // fixed 'close' column
            const dialogWidthC = this.dialog.clientWidth;
            let widthSumO = 0;
            let widthSumC = 0;
            let nRows = this.rows.length;
            for (let i = 0; i < iLastO; i++) {
                const widthO = Math.round(dialogWidthO * this.startColumnWidthRatiosO[i]);
                this.columnWidthsO[i] = widthO;
                widthSumO += widthO;
                if (this.activeTab === 'opened') {
                    this.headers[i].style.width = `${widthO}px`;
                    for (let j = 0; j < nRows; j++) {
                        this.rows[j].cells[i].style.width = `${widthO}px`;
                    }
                }
            }
            for (let i = 0; i < iLastC; i++) {
                const widthC = Math.round(dialogWidthC * this.startColumnWidthRatiosC[i]);
                this.columnWidthsC[i] = widthC;
                widthSumC += widthC;
                if (this.activeTab === 'closed') {
                    this.headers[i].style.width = `${widthC}px`;
                    for (let j = 0; j < nRows; j++) {
                        this.rows[j].cells[i].style.width = `${widthC}px`;
                    }
                }
            }
            const widthO = dialogWidthO - widthSumO;
            const widthC = dialogWidthC - widthSumC;
            this.columnWidthsO[iLastO] = widthO;
            this.columnWidthsC[iLastC] = widthC;
            if (this.activeTab === 'opened') {
                this.headers[iLastO].style.width = `${widthO}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[iLastO].style.width = `${widthO}px`;
                }
            } else if (this.activeTab === 'closed') {
                this.headers[iLastC].style.width = `${widthC}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[iLastC].style.width = `${widthC}px`;
                }
            }
        }
    }

    stopResize() {
        super.stopResize();
    }

    _calculatePnLAll() {
        if (!this.data) return;
        const isOpened = !this.activeTab || this.activeTab === 'opened';
        this.data.forEach((pos) => {
            const PnL = _iCalculatePnL(pos.enterPrice, pos.priceStep, pos.priceStepCost, pos.exitPrice, isOpened ? pos.curPos : pos.closedVolume);
            pos.pnlP = PnL.P;
            pos.pnlR = PnL.R;
            pos.pnlPercent = PnL.Percent;
        });
    }

    // Invoked if whole data is updated
    // - triggered by initial active positions loading
    // - triggered by active tab changed
    setData(data) {
        this.data = data || [];
        this._calculatePnLAll();
        this.renderTable();
    }

    //// Invoked if new positions are arrived
    //// - triggered by server service - SubscribeForPrivatePosition
    //setNewData(newPositions) {
    //    if (!newPositions || newPositions.length == 0) return;

    //    // Determine what state to keep based on activeTab
    //    let validState = this.activeTab === 'opened' ? true : false;

    //    // Step 1: Update existing positions (preserving position)
    //    const updatedData = [];
    //    const processedIds = new Set();

    //    if (this.data && this.data.length > 0) {
    //        for (const position of this.data) {
    //            const newPosition = newPositions.find(o => o.secId === position.secId);
    //            if (newPosition) {
    //                // If the new position matches the tab's criteria → include it
    //                if (newPosition.open === validState) {
    //                    updatedData.push(newPosition);
    //                    processedIds.add(newPosition.secId);
    //                }
    //                // Else: Remove it (do not push)
    //            } else {
    //                // If no update, keep the position only if it matches the tab's criteria
    //                if (newPosition.open === validState) {
    //                    updatedData.push(position);
    //                }
    //            }
    //        }
    //    }

    //    // Step 2: Add new positions not already in `this.data`
    //    for (const position of newPositions) {
    //        if (!processedIds.has(position.id) && position.open === validState) {
    //            updatedData.push(position);
    //        }
    //    }

    //    this.data = updatedData;

    //    this.renderTable();
    //}

    refreshData() {
        if (!this.tbody || !this.data) return;
        const trs = this.tbody.children;
        if (this.data.length != trs.length) return;

        this._calculatePnLAll();

        for (const tr of trs) {
            const id = tr.dataset.id;
            const pos = this.data.find(pos => pos.secId === id);
            const pnlTd = tr.children[1];
            pnlTd.textContent = this.pnlVisibleType == 0 ? `${pos.pnlP.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlP')}` :
                this.pnlVisibleType == 1 ? `${pos.pnlR.toLocaleString(undefined, { maximumFractionDigits: 3 })}${__('pnlR')}` :
                    `${pos.pnlPercent.toLocaleString(undefined, { maximumFractionDigits: 3 })}%`;
            if (pos.pnlP !== 0)
                tr.className = pos.pnlP > 0 ? "low" : "high";
        }
    }

    getActiveTab() {
        return this.activeTab;
    }

    setPnlVisibleType(type) {
        this.pnlVisibleType = type;
        this.renderTable();

        // Update checkmarks in context menu if it exists
        if (this.contextMenu) {
            const submenuItems = this.contextMenu.querySelectorAll('[data-group="pnl-display"]');
            submenuItems.forEach((item, index) => {
                if (index === type) {
                    item.classList.add('checked');
                } else {
                    item.classList.remove('checked');
                }
            });
        }
    }

    _setExportMenuItemVisibility(visible) {
        if (this.contextMenu) {
            const separator = this.contextMenu.children[1];
            const menuItem = this.contextMenu.children[2];
            if (visible) {
                separator.classList.remove('d-none');
                menuItem.classList.remove('d-none');
            }
            else {
                separator.classList.add('d-none');
                menuItem.classList.add('d-none');
            }
        }
    }

    exportData(type) {
        // csv | html | xml | excel
    }

    close() {
        this.changeTabCallback = null;
        this.closePositionCallback = null;
        this.startColumnWidthRatiosO = null;
        this.startColumnWidthRatiosC = null;
        super.close();
    }

    getProfile() {
        var bound = super.getProfile();
        const profile = {
            bound: bound,
            pnlVisibleType: this.pnlVisibleType,
            activeTab: this.activeTab,
            columnWidthsO: this.columnWidthsO,
            columnWidthsC: this.columnWidthsC
        };
        if (this.sortColumn && this.sortDirection) {
            profile.sortColumn = this.sortColumn;
            profile.sortDirection = this.sortDirection;
        }
        return profile;
    }
}
