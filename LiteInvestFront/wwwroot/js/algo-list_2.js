class AlgoList {
    constructor(wrapperElement, options = {}) {
        this.wrapper = wrapperElement;

        // Create header and body elements
        this.headerEl = document.createElement('div');
        this.headerEl.className = 'list-header';

        const headerCol1 = document.createElement('div');
        headerCol1.classList = 'list-col1 noselect';
        headerCol1.textContent = options.header?.col1 || '';

        const headerCol2 = document.createElement('div');
        headerCol2.className = 'list-col2';
        headerCol2.textContent = options.header?.col2 || '';

        this.headerEl.appendChild(headerCol1);
        this.headerEl.appendChild(headerCol2);
        this.wrapper.appendChild(this.headerEl);

        this.bodyEl = document.createElement('div');
        this.bodyEl.className = 'list-body';
        this.wrapper.appendChild(this.bodyEl);

        // Events
        this.onHeaderClick = options.onHeaderClick || (() => { });
        this.onChange = options.onChange || (() => { });
        this.selectedValue = null;

        this._bindHeader();
        this.setItems(options.items || [], options.defaultValue);
    }

    _bindHeader() {
        if (this.headerEl) {
            this._headerClickHandler = () => this.onHeaderClick();
            this.headerEl.addEventListener('click', this._headerClickHandler);
        }
    }

    setItems(items, defaultValue = null) {
        this.bodyEl.innerHTML = '';
        this.items = [];
        this._itemClickHandlers = [];

        items.forEach((item, index) => {
            const itemEl = document.createElement('div');
            itemEl.className = 'list-item';
            itemEl.setAttribute('data-value', item.value);

            const col1 = document.createElement('div');
            col1.classList = 'list-col1';
            col1.textContent = item.col1;

            const col2 = document.createElement('div');
            col2.className = 'list-col2';
            col2.textContent = item.col2;

            itemEl.appendChild(col1);
            itemEl.appendChild(col2);
            this.bodyEl.appendChild(itemEl);
            this.items.push(itemEl);

            const clickHandler = () => this._selectItem(itemEl);
            itemEl.addEventListener('click', clickHandler);
            this._itemClickHandlers.push(clickHandler);

            if (item.value === defaultValue || (index === 0 && defaultValue === null)) {
                this._selectItem(itemEl);
            }
        });
    }

    _selectItem(itemEl) {
        this.items.forEach(i => i.classList.remove('selected'));
        itemEl.classList.add('selected');
        this.selectedValue = itemEl.dataset.value;
        this.onChange(this.selectedValue);
    }

    getValue() {
        return this.selectedValue;
    }

    setValue(value) {
        const match = this.items.find(i => i.dataset.value === value);
        if (match) this._selectItem(match);
    }

    destroy() {
        if (this._headerClickHandler) {
            this.headerEl.removeEventListener('click', this._headerClickHandler);
        }

        if (this.items && this._itemClickHandlers) {
            this.items.forEach((item, index) => {
                item.removeEventListener('click', this._itemClickHandlers[index]);
            });
        }

        this.bodyEl.innerHTML = '';
        this.items = null;
        this._itemClickHandlers = null;
        this.selectedValue = null;
    }
}
