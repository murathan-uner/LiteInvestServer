class WindowsPane {
    constructor(config) {

        this.secId = config.secId;
        this.windowId = config.windowId;
        this.openSettingCallback = config.openSettingCallback;
        this.closeCallback = config.closeCallback;
        this.openInstrumentPickerCallback = config.openInstrumentPickerCallback;

        this.containerWidths = config.containerWidths;
        this.containerClasses = config.containerClasses;
        this.containerMinWidths = config.containerMinWidths;

        this.clusterTable = config.clusterTable;
        this.quoteTable = config.quoteTable;
        this.chart = config.chart;

        this.windows = document.getElementById(this.windowId);
        this.windowsBound = this.windows.getBoundingClientRect();

        var header = this.windows.getElementsByClassName('instrument-header')[0];
        this.settingButton = header.getElementsByClassName('settings-icon')[0];
        this.closeButton = header.getElementsByClassName('close-icon')[0];
        this.instrumentPickerButton = header.getElementsByClassName('instrument-header__title')[0];
        this.clickSettingHandler = this._onClickSetting.bind(this);
        this.clickCloseHandler = this._onClickClose.bind(this);
        this.clickInstrumentPickerHandler = this._onClickInstrumentPicker.bind(this);

        this.settingButton.addEventListener('click', this.clickSettingHandler);
        this.closeButton.addEventListener('click', this.clickCloseHandler);
        this.instrumentPickerButton.addEventListener('click', this.clickInstrumentPickerHandler);

        let containers = [];
        // let containerBounds = [];
        this.containerClasses.forEach((item, i) => {
            //var container = document.querySelector(`#${config.windowId} .${item}`);
            var escapedWindowId = CSS.escape(config.windowId);
            var container = document.querySelector(`#${escapedWindowId} .${item}`);
            containers[i] = container;
            // containerBounds[i] = container.getBoundingClientRect();
        });
        this.containers = containers;
        // this.containerBounds = containerBounds;

        this.gutterHandlers = [];
        for (let i = 0; i < 2; i++) {
            let gutter = document.createElement('div');
            gutter.classList = 'split-gutter windows-split-gutter';
            this.windows.appendChild(gutter);

            const handler = (event) => this._startDraggingGutter(event, i);
            gutter.addEventListener("mousedown", handler);

            this.gutterHandlers.push({ gutter, handler });
            this._positionGutter(i);
        }

        this.mouseUpHandler = this._stopDraggingGutter.bind(this);
        this.mouseMoveHandler = this._dragGutter.bind(this);
        document.addEventListener("mouseup", this.mouseUpHandler);
        document.addEventListener("mousemove", this.mouseMoveHandler);

        this._observeBodyResize();
    }

    destroy() {

        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }

        document.removeEventListener("mouseup", this.mouseUpHandler);
        document.removeEventListener("mouseup", this.mouseMoveHandler);
        this.settingButton.removeEventListener('click', this.clickSettingHandler);
        this.closeButton.removeEventListener('click', this.clickCloseHandler);
        this.instrumentPickerButton.removeEventListener('click', this.clickInstrumentPickerHandler);

        this.destroyGutters();

        // DO NOT REMOVE FOR FUTURE OPTIMIZATION
        //this.windows.replaceChildren();

        this.secId = null;
        this.windowId = null;
        this.windows = null;
        this.windowsBound = null;

        this.containerWidths = null;
        this.containerClasses = null;
        this.containerMinWidths = null;

        this.clusterTable = null;
        this.quoteTable = null;
        this.chart = null;

        this.containers = null;
        this.containerClasses = null;
        this.settingButton = null;
        this.closeButton = null;
        this.instrumentPickerButton = null;

        this.mouseUpHandler = null;
        this.mouseMoveHandler = null;
        this.clickSettingHandler = null;
        this.clickCloseHandler = null;
        this.clickInstrumentPickerHandler = null;

        this.openSettingCallback = null;
        this.closeCallback = null;
        this.openInstrumentPickerCallback = null;
    }

    _positionGutter(index) {
        let containerBound = this.containers[index].getBoundingClientRect();
        this.gutterHandlers[index].gutter.style.left = `${containerBound.right - this.windowsBound.left}px`;
        //console.log(`=========> ${containerBound.left} ${containerBound.right} ${containerBound.width} ${this.windowsBound.left}`);
    }

    _onClickSetting() {
        if (this.openSettingCallback)
            this.openSettingCallback(this.windowId);
    }

    _onClickClose() {
        if (this.closeCallback)
            this.closeCallback(this.windowId);
    }

    _onClickInstrumentPicker() {
        if (this.openInstrumentPickerCallback)
            this.openInstrumentPickerCallback(this.secId, this.windowId);
    }

    _startDraggingGutter(e, index) {
        this.dragging = true;
        this.currentGutterIndex = index;
        this.startX = e.clientX;

        this.startWidths = [
            this.containers[index].offsetWidth,
            this.containers[index + 1].offsetWidth,
        ];

        if (index == 0) {
            this.clusterTable.updateTableMetricUnit();
        }

        // Disable text selection while dragging
        document.body.style.userSelect = 'none';

        var rect = this.windows.getBoundingClientRect();
        //console.log(`===> ${rect.left} ${rect.width}`);
        //console.log(`===>w ${this.windowsBound.left} ${this.windowsBound.width}`);
        //console.log(`======> ${this.startWidths[0]} ${this.startWidths[1]}`);
    }

    _dragGutter(e) {
        if (!this.dragging) return;

        let dx = e.clientX - this.startX;

        let newWidth1 = this.startWidths[0] + dx;
        let newWidth2 = this.startWidths[1] - dx;

        if (this.currentGutterIndex == 0) {
            if (newWidth1 < this.containerMinWidths[0]) {
                this.containers[0].style.width = '0';
                this.containers[1].style.width = `${this.startWidths[0] + this.startWidths[1]}px`;
            } else if (newWidth2 < this.containerMinWidths[1]) {
                this.containers[0].style.width = `${this.startWidths[0] + this.startWidths[1] - this.containerMinWidths[1]}px`;
                this.containers[1].style.width = `${this.containerMinWidths[1]}px`;
            } else {
                this.containers[0].style.width = `${newWidth1}px`;
                this.containers[1].style.width = `${newWidth2}px`;
            }
        } else {
            if (newWidth1 < this.containerMinWidths[1]) {
                this.containers[1].style.width = `${this.containerMinWidths[1]}px`;
                this.containers[2].style.width = `${this.startWidths[0] + this.startWidths[1] - this.containerMinWidths[1]}px`;
            } else if (newWidth2 < this.containerMinWidths[2]) {
                this.containers[1].style.width = `${this.startWidths[0] + this.startWidths[1] - this.containerMinWidths[2]}px`;
                this.containers[2].style.width = `${this.containerMinWidths[2]}px`;
            } else {
                this.containers[1].style.width = `${newWidth1}px`;
                this.containers[2].style.width = `${newWidth2}px`;
            }
        }
        this._positionGutter(this.currentGutterIndex);
    }

    _stopDraggingGutter(e) {
        if (!this.dragging) return;

        this.dragging = false;
        // Re-enable text selection
        document.body.style.userSelect = "";

        this.windowsBound = this.windows.getBoundingClientRect();
        this.containerWidths = this._rerecalculateColumnWidth();
        this.containers.forEach((item, i) => {
            item.style.width = `${this.containerWidths[i]}%`;
        });

        if (this.currentGutterIndex == 0) {
            this.clusterTable.updateTableMetricUnit();
        }

        this.startWidths = null;
        this.currentGutterIndex = -1;
    }

    _bodyResizing() {

        this.windowsBound = this.windows.getBoundingClientRect();

        //console.log("windows - bodyResizing: w = " + this.windowsBound.width);

        if (this.windowsBound.width < this.containerMinWidths[0] + this.containerMinWidths[1] + this.containerMinWidths[2]) {
            this.containers[0].style.width = '0';
            var width2 = 100 / this.windowsBound.width * this.containerMinWidths[2];
            this.containers[2].style.width = `${width2}%`;
            this.containers[1].style.width = `${100 - width2}%`;

            this.containerWidths[0] = 0;
            this.containerWidths[2] = width2;
            this.containerWidths[1] = 100 - width2;

        } else {

            let quoteBound = this.containers[2].getBoundingClientRect();
            if (quoteBound.width <= this.containerMinWidths[2]) {
                var clusterBound = this.containers[0].getBoundingClientRect();
                var width2 = 100 / this.windowsBound.width * this.containerMinWidths[2];
                var width0 = 100 / this.windowsBound.width * clusterBound.width;
                this.containers[0].style.width = `${width0}%`;
                this.containers[1].style.width = `${100 - width0 - width2}%`;
                this.containers[2].style.width = `${width2}%`;

                this.containerWidths[0] = width0;
                this.containerWidths[1] = 100 - width0 - width2;
                this.containerWidths[2] = width2;

            } else {
                this.containers[0].style.width = `${this.containerWidths[0]}%`;
                this.containers[2].style.width = `${this.containerWidths[2]}%`;
                this.containers[1].style.width = `${100 - this.containerWidths[0] - this.containerWidths[2]}%`;

                this.containerWidths = this._rerecalculateColumnWidth();
            }
        }

        this.gutterHandlers.forEach((_, i) => {
            this._positionGutter(i);
        });
    }

    _observeBodyResize() {
        this.resizeObserver = new ResizeObserver(() => {
            this._bodyResizing();
        });

        this.resizeObserver.observe(this.windows);
    }

    _rerecalculateColumnWidth() {
        if (this.windowsBound.width == 0) {
            return this.containerWidths;
        } else {
            return this.containers.map((container) => 100 / this.windowsBound.width * container.offsetWidth);
        }
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

    refresh() {
        this.windowsBound = this.windows.getBoundingClientRect();
    }

    //setQuoteTable(quoteTable) {
    //    this.quoteTable = quoteTable;
    //}

    //setClusterTable(clusterTable) {
    //    this.clusterTable = clusterTable;
    //}

}
