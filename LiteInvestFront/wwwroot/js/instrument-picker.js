
// Assumes AlgoDialog base class is already defined

class InstrumentPicker extends AlgoDialog {

    // tab: 'all' | 'favorite' | 'recent'
    // columns: 'favorite' | 'symbol' | 'longName' | 'updatedAt'
    //          star icon toggle | Isin | FullName | DateUpdateFromMoex

    constructor(data, config) {
        const activeTab = config.profile ? (config.profile.activeTab || 'all') : 'all';
        const contentHtml = InstrumentPicker.buildContentHtml(activeTab);
        super({
            title: __('select_instrument'),
            contentHtml,
            customClass: 'instrument-dialog',
            resizable: true,
            profile: config.profile,
            closingCallback: config.closingCallback,
            closedCallback: config.closedCallback
        });

        this.allData = data.all || [];
        this.favData = data.fav || [];
        this.recentData = data.recent || [];
        this.secId = config.secId;
        this.windowId = config.windowId;
        this.selectInstrumentCallback = config.selectInstrumentCallback;
        this.favChangedCallback = config.favChangedCallback;

        this.activeTab = activeTab;
        this.sortColumn = config.profile ? config.profile.sortColumn : undefined;
        this.sortDirection = config.profile ? config.profile.sortDirection : undefined;

        if (config.profile) {
            this.columnWidths = config.profile.columnWidths;
        }

        this.init();
    }

    static buildContentHtml(activeTab) {
        return `
            <div class="algo-dialog-search-wrapper instrument-picker-search-wrapper">
                <div class="algo-dialog-search-icon">
                    <svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
                        <path d="M365.3 320h-22.7l-26.7-26.7C338.5 265.7 352 230.4 352 192c0-88.4-71.6-160-160-160S32 103.6 32 192s71.6 160 160 160c38.4 0 73.7-13.5 101.3-36.1l26.7 26.7v22.7L434.7 480l45.3-45.3zM64 192c0-70.7 57.3-128 128-128s128 57.3 128 128-57.3 128-128 128S64 262.7 64 192"></path>
                    </svg>
                </div>
                <input type="text" class="algo-dialog-search" placeholder="${__('search_symbol_or_name')}">
            </div>
            <div class="algo-dialog-tabs instrument-picker-tabs">
                <button class="algo-dialog-tab${activeTab === 'all' ? ' active' : ''}" data-tab="all">${__('tab_all')}</button>
                <button class="algo-dialog-tab${activeTab === 'favorite' ? ' active' : ''}" data-tab="favorite">${__('tab_favorite')}</button>
                <button class="algo-dialog-tab${activeTab === 'recent' ? ' active' : ''}" data-tab="recent">${__('tab_recent')}</button>
            </div>
            <div class="algo-dialog-table-wrapper instrument-picker-table-wrapper">
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
        this.searchInput = base.querySelector('.algo-dialog-search');
        this.tabs = base.querySelectorAll('.algo-dialog-tab');
        this.thead = base.querySelector('thead');
        this.tbody = base.querySelector('tbody');
        this.tempty = base.querySelector('.algo-dialog-table-empty');

        // Focus the search input when dialog opens
        this.searchInput.focus();

        this.searchInput.addEventListener('input', () => this.renderTable());
        this.tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                if (this.activeTab === e.target.dataset.tab) return;
                this.activeTab = e.target.dataset.tab;

                this.tabs.forEach(t => t.classList.remove('active'));
                e.target.classList.add('active');
                this.renderTable();
            });
        });

        base.querySelector('input').addEventListener('mousedown', (e) => {
            const input = e.currentTarget;
            input.setSelectionRange(0, 0);
        });

        const dialogWidth = this.dialog.clientWidth;

        if (!this.columnWidths || this.columnWidths.length !== 4 ||
            this.columnWidths[0] < 40 || this.columnWidths[1] < 40 || this.columnWidths[2] < 40) {
            this.columnWidths = [];
            this.columnWidths[0] = 40; // favorite
            this.columnWidths[1] = dialogWidth * 20 / 100; // symbol
            this.columnWidths[2] = dialogWidth * 55 / 100; // longName
            this.columnWidths[3] = dialogWidth - (this.columnWidths[0] + this.columnWidths[1] + this.columnWidths[2]); // updatedAt
        }

        this.renderTable();
    }

    renderTable() {

        this.thead.innerHTML = '';
        this.tbody.innerHTML = '';

        const headerRow = document.createElement('tr');

        const createHeaderCell = (label, columnKey, index) => {
            const th = document.createElement('th');
            th.innerHTML = label;
            if (columnKey === 'favorite')
                th.classList = 'sortable-header noborder-right';
            else
                th.className = 'sortable-header';
            th.style.width = `${this.columnWidths[index]}px`;

            // Track if we're resizing
            let isResizing = false;

            if (columnKey !== 'favorite') {
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

            if (columnKey !== 'updatedAt' && columnKey !== 'favorite') {
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

                    const currentWidths = this.columnWidths;
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

        headerRow.appendChild(createHeaderCell('', 'favorite', 0));
        headerRow.appendChild(createHeaderCell(__('symbol'), 'symbol', 1));
        headerRow.appendChild(createHeaderCell(__('long_name'), 'longName', 2));
        headerRow.appendChild(createHeaderCell(__('updated_at'), 'updatedAt', 3));
        this.thead.appendChild(headerRow);

        let filtered = [];
        if (this.activeTab === 'all') {
            filtered = [...this.allData];
        } else if (this.activeTab === 'favorite') {
            filtered = [...this.favData];
        } else if (this.activeTab === 'recent') {
            filtered = [...this.recentData];
        }

        const term = this.searchInput.value?.toLowerCase() || '';
        if (term) {
            filtered = filtered.filter(x => x.symbol.toLowerCase().startsWith(term));
        }

        if (this.sortColumn && this.sortDirection) {
            const key = this.sortColumn;
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

        if (filtered.length == 0) {
            this.tempty.classList.remove('d-none');
        } else {
            this.tempty.classList.add('d-none');
            const currentWidths = this.columnWidths;

            for (const item of filtered) {
                const tr = document.createElement('tr');
                //tr.ondblclick = () => this.selectRow(item);

                const favTd = document.createElement('td');
                favTd.innerHTML = item.favorite ? this.getStarFillSvg() : this.getStarOutlineSvg();
                favTd.style.width = `${currentWidths[0]}px`;
                favTd.onclick = (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.toggleFavorite(item);
                };
                tr.appendChild(favTd);

                const symbolTd = document.createElement('td');
                symbolTd.innerText = item.symbol;
                symbolTd.style.width = `${currentWidths[1]}px`;
                tr.appendChild(symbolTd);

                const longNameTd = document.createElement('td');
                longNameTd.innerText = item.longName;
                longNameTd.style.width = `${currentWidths[2]}px`;
                tr.appendChild(longNameTd);

                const updatedAtTd = document.createElement('td');
                updatedAtTd.innerText = item.updatedAt;
                updatedAtTd.style.width = `${currentWidths[3]}px`;
                tr.appendChild(updatedAtTd);

                symbolTd.ondblclick = () => this.selectRow(item);
                longNameTd.ondblclick = () => this.selectRow(item);
                updatedAtTd.ondblclick = () => this.selectRow(item);

                this.tbody.appendChild(tr);
            }
        }

        const headers = this.thead.querySelectorAll('th');
        const rows = this.tbody.querySelectorAll('tr');

        this.headers = headers;
        this.rows = rows;
    }

    _columnWidths() {
        return this.columnWidths;
    }

    startResize(e) {
        super.startResize(e);
        const iLast = 4;
        const dialogWidth = this.dialog.clientWidth - this.columnWidths[0]; // fixed 'star' column
        if (!this.startColumnWidthRatios) this.startColumnWidthRatios = [];

        for (let i = 1; i < iLast; i++) {
            this.startColumnWidthRatios[i] = this.columnWidths[i] / dialogWidth;
        }
    }

    resize(e) {
        const delta = super.resize(e);
        if (delta.dx > 0) {
            // expanding width
            const iLast = 3; // 4-1
            const dialogWidth = this.dialog.clientWidth - this.columnWidths[0]; // fixed 'star' column
            let widthSum = 0;
            for (let i = 1; i < iLast; i++) {
                widthSum += this.columnWidths[i];
            }
            const lastWidth = dialogWidth - widthSum;
            this.columnWidths[iLast] = lastWidth;

            let nRows = this.rows.length;
            this.headers[iLast].style.width = `${lastWidth}px`;
            for (let i = 0; i < nRows; i++) {
                this.rows[i].cells[iLast].style.width = `${lastWidth}px`;
            }
        } else if (delta.dx < 0) {
            // shrinking width
            const iLast = 3; // 4-1
            const dialogWidth = this.dialog.clientWidth - this.columnWidths[0]; // fixed 'star' column
            let widthSum = 0;
            let nRows = this.rows.length;
            for (let i = 1; i < iLast; i++) {
                const width = Math.round(dialogWidth * this.startColumnWidthRatios[i]);
                this.columnWidths[i] = width;
                widthSum += width;
                this.headers[i].style.width = `${width}px`;
                for (let j = 0; j < nRows; j++) {
                    this.rows[j].cells[i].style.width = `${width}px`;
                }
            }
            const width = dialogWidth - widthSum;
            this.columnWidths[iLast] = width;
            this.headers[iLast].style.width = `${width}px`;
            for (let j = 0; j < nRows; j++) {
                this.rows[j].cells[iLast].style.width = `${width}px`;
            }
        }
    }

    stopResize() {
        super.stopResize();
    }

    toggleFavorite(item) {
        item.favorite = !item.favorite;

        if (item.favorite) {

            // Add to favorites if not already there
            if (!this.favData.some(favItem => favItem.id === item.id)) {
                const favItem = { ...item };
                this.favData.push(favItem);
            }

            // Update in recent data if present
            const recentItem = this.recentData.find(recItem => recItem.id === item.id);
            if (recentItem) {
                recentItem.favorite = true;
            }

            if (this.activeTab !== 'all') {
                const allItem = this.allData.find(allItem => allItem.id === item.id);
                if (allItem) {
                    allItem.favorite = true;
                }
            }

        } else {
            // Remove from favorites
            //      DO NOT USE THIS METHOD TO REMOVE ITEMS FROM THE SHARED ARRAY DATA -----> this.favData = this.favData.filter(favItem => favItem.id !== item.id);
            // Instead of creating new array, modify the existing one
            const index = this.favData.findIndex(favItem => favItem.id === item.id);
            if (index >= 0) {
                this.favData.splice(index, 1); // Mutates the original array
            }

            // Update in recent data if present
            const recentItem = this.recentData.find(recItem => recItem.id === item.id);
            if (recentItem) {
                recentItem.favorite = false;
            }

            if (this.activeTab !== 'all') {
                const allItem = this.allData.find(allItem => allItem.id === item.id);
                if (allItem) {
                    allItem.favorite = false;
                }
            }
        }

        this.renderTable();

        if (this.favChangedCallback)
            this.favChangedCallback(item.id, item.favorite);
    }

    selectRow(item) {
        if (this.selectInstrumentCallback)
            this.selectInstrumentCallback(this.activeTab, this.secId, this.windowId, item);
        super.close();
    }

    close() {
        this.selectInstrumentCallback = null;
        this.startColumnWidthRatios = null;
        super.close();
    }

    getStarFillSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="16" height="16">
            <path d="M256 391.5 117.2 480 154 314.1 32 202.4 192.9 188 256 32l63.1 156L480 202.4 358 314.1 394.8 480z"></path>
        </svg>`;
    }

    getStarOutlineSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="16" height="16">
            <path d="M480 202.4 319.1 188 256 32l-63.1 156L32 202.4l122 111.7L117.2 480 256 391.5 394.8 480 358 314.1zM256 353.6l-90.8 57.9 24-108.5-82.1-75.2 108.1-9.7L256 117.3l40.8 100.8 108.1 9.7-82.1 75.2 24.1 108.5z"></path>
        </svg>`;
    }

    getProfile() {
        var bound = super.getProfile();
        const profile = {
            bound: bound,
            activeTab: this.activeTab,
            columnWidths: this.columnWidths
        };
        if (this.sortColumn && this.sortDirection) {
            profile.sortColumn = this.sortColumn;
            profile.sortDirection = this.sortDirection;
        }
        return profile;
    }
}
