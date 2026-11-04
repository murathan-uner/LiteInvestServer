class AlgoSelect {
    constructor(wrapperElement, options = {}) {
        this.wrapper = wrapperElement;
        this.selectBox = wrapperElement.querySelector('.algo-select');
        this.dropdown = wrapperElement.querySelector('.select-dropdown');
        this.selectedText = this.selectBox.querySelector('.selected-text');
        this.onChange = options.onChange || (() => { });
        this.iconClass = options.iconClass || 'bi bi-check2';
        this.isOpen = false;

        this.setItems(options.items || [], options.defaultValue);

        this._bindEvents();
    }

    destroy() {
        // Remove click listener from select box
        if (this._selectBoxClickHandler) {
            this.selectBox.removeEventListener('click', this._selectBoxClickHandler);
        }

        // Remove document click listener
        if (this._outsideClickHandler) {
            document.removeEventListener('click', this._outsideClickHandler);
        }

        // Remove item listeners
        if (this._itemClickHandlers && this.items) {
            this.items.forEach((item, index) => {
                item.removeEventListener('click', this._itemClickHandlers[index]);
            });
        }

        // Clear DOM and references
        this.dropdown.innerHTML = '';
        this.items = null;
        this._itemClickHandlers = null;
        this._selectBoxClickHandler = null;
        this._outsideClickHandler = null;
        this.wrapper = null;
        this.selectBox = null;
        this.dropdown = null;
        this.selectedText = null;
        this.onChange = null;
        this.isOpen = false;
    }

    setItems(items, defaultValue = null) {
        this.itemsData = items;
        this.dropdown.innerHTML = '';
        this._itemClickHandlers = [];

        items.forEach((item, index) => {
            const itemEl = document.createElement('div');
            itemEl.classList.add('select-item');
            itemEl.setAttribute('data-value', item.value);

            const span = document.createElement('span');
            span.textContent = item.title;

            const icon = document.createElement('i');
            icon.className = `${this.iconClass} check-icon`;

            itemEl.appendChild(span);
            itemEl.appendChild(icon);
            this.dropdown.appendChild(itemEl);

            const clickHandler = () => this._selectItem(itemEl);
            itemEl.addEventListener('click', clickHandler);
            this._itemClickHandlers.push(clickHandler);

            if (item.value === defaultValue || (index === 0 && defaultValue === null)) {
                this._selectItem(itemEl, true);
            }
        });

        this.items = this.dropdown.querySelectorAll('.select-item');
    }

    _bindEvents() {
        this._selectBoxClickHandler = () => {
            this.isOpen = !this.isOpen;
            this.dropdown.classList.toggle('open', this.isOpen);
        };

        this.selectBox.addEventListener('click', this._selectBoxClickHandler);

        this._outsideClickHandler = (e) => {
            if (!this.wrapper.contains(e.target)) {
                this.isOpen = false;
                this.dropdown.classList.remove('open');
            }
        };

        document.addEventListener('click', this._outsideClickHandler);
    }

    _selectItem(item, init) {
        if (this.items)
            this.items.forEach(i => i.classList.remove('selected'));

        item.classList.add('selected');

        const selectedTitle = item.querySelector('span').textContent;
        this.selectedText.textContent = selectedTitle;

        this.isOpen = false;
        this.dropdown.classList.remove('open');

        if (!init) {
            const value = item.dataset.value;
            this.onChange(value);
        }
    }

    setValue(value) {
        const item = [...this.items].find(i => i.dataset.value === value);
        if (item) this._selectItem(item);
    }

    getValue() {
        const selected = this.wrapper.querySelector('.select-item.selected');
        return selected ? selected.dataset.value : null;
    }
}
