class AlgoList {
    constructor(wrapperElement, options = {}) {
        this.wrapper = wrapperElement;

        // Create body elements
        this.bodyEl = document.createElement('div');
        this.bodyEl.className = 'list-body';
        this.wrapper.appendChild(this.bodyEl);

        // Events
        this.onChange = options.onChange || (() => { });
        this.selectedIndex = null;
        this.selectedValue = null;
        this.minValue = options.minValue;

        this.setItems(options.items || [], options.defaultIndex);
    }

    setItems(items, defaultIndex = null) {
        this.bodyEl.innerHTML = '';
        this.items = [];
        this._itemClickHandlers = [];

        items.forEach((item, index) => {
            const itemEl = document.createElement('div');
            itemEl.className = 'list-item';
            itemEl.setAttribute('data-index', index);
            itemEl.setAttribute('data-value', item.value);

            const col1 = document.createElement('div');
            col1.classList = 'list-col1 noselect';
            col1.textContent = item.value;

            itemEl.appendChild(col1);
            this.bodyEl.appendChild(itemEl);
            this.items.push(itemEl);

            const clickHandler = () => this._selectItem(itemEl);
            itemEl.addEventListener('click', clickHandler);
            this._itemClickHandlers.push(clickHandler);

            if (index === defaultIndex) {
                this._selectItem(itemEl, true);
            }
        });
    }

    _selectItem(itemEl, isRefresh) {
        this.items.forEach(i => i.classList.remove('selected'));
        itemEl.classList.add('selected');
        var selectedIndex = itemEl.dataset.index;
        var selectedValue = itemEl.dataset.value;
        this.selectedIndex = Number.parseInt(selectedIndex);
        this.selectedValue = Number.parseInt(selectedValue);
        if (!isRefresh)
            this.onChange(this.selectedIndex, this.selectedValue);
    }

    getValue() {
        return this.selectedValue;
    }

    setValue(index, value, isRefresh) {
        var itemEl = this.items[index];
        const listColEl = itemEl.querySelector('.list-col1');
        if (this.minValue && value < this.minValue) value = this.minValue;
        listColEl.textContent = value;
        itemEl.setAttribute('data-value', value);
        if (index == this.selectedIndex && value != this.selectedValue)
            this._selectItem(itemEl, isRefresh);
    }

    getIndex() {
        return this.selectedIndex;
    }

    destroy() {
        if (this.items && this._itemClickHandlers) {
            this.items.forEach((item, index) => {
                item.removeEventListener('click', this._itemClickHandlers[index]);
            });
        }

        // DO NOT REMOVE FOR FUTURE OPTIMIZATION
        //this.bodyEl.replaceChildren();

        this.items = null;
        this._itemClickHandlers = null;
        this.selectedValue = null;
        this.minValue = null;
    }
}
