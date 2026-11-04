class AlgoPopup {
    constructor(window, listInstance, options = {}) {
        this.window = window;
        this.list = listInstance;
        this.popup = this.window.querySelector('.algo-popup');
        this.input = this.popup.querySelector('.popup-input');

        this.minValue = options.minValue;
        this.onAcceptInput = options.onAcceptInput || (() => { });
        this.onDiscardInput = options.onDiscardInput || (() => { });
        this._isOpen = false;

        this._attachEvents();
    }

    _attachEvents() {
        // Show popup on list item double-click
        this.list.items.forEach(item => {
            item.addEventListener('dblclick', () => this.onDblClickListItem());
        });

        // Accept (Enter) or discard (Escape)
        this.input.addEventListener('keydown', (e) => this.onKeydownInput(e));

        // Click outside popup but inside window → accept
        this.window.addEventListener('click', (e) => {
            e.preventDefault();
            this.onClickOutside(e);
        });
    }

    onDblClickListItem() {
        this.popup.classList.remove('hidden');
        this._isOpen = true;
        this.input.setAttribute('data-index', this.list.getIndex());
        this.input.value = this.list.getValue() ?? '';
        this.input.focus();
        this.input.select();
    }

    onKeydownInput(e) {
        if (e.key === 'Enter') {
            this._accept();
        } else if (e.key === 'Escape') {
            this._discard();
        }
    }

    onClickOutside(e) {
        if (this._isOpen && !this.popup.contains(e.target) && !e.target.classList.contains('list-item')) {
            this._accept();
        }
    }

    _accept() {
        const index = Number.parseInt(this.input.dataset.index);
        let value = Number.parseInt(this.input.value);
        if (this.minValue && value < this.minValue) value = this.minValue;
        this.list.setValue(index, value);
        this.hide();
        this.onAcceptInput(index, value);
    }

    _discard() {
        this.hide();
        this.onDiscardInput();
    }

    hide() {
        this.popup.classList.add('hidden');
        this._isOpen = false;
    }

    destroy() {
        // Optionally clean up if needed later
        this.hide();
        this.list.items.forEach(item => item.removeEventListener('dblclick', this.onDblClickListItem));
        this.input.removeEventListener('keydown', this.onKeydownInput);
        this.window.removeEventListener('click', this.onClickOutside);
    }
}
