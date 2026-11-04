class WindowsSplitter {

    constructor(container, callbackDrag) {

        this.minSize = 100;

        this.container = container;
        this.containerBound = new DOMRect();
        this.callbackDrag = callbackDrag;

        this.mouseUpHandler = null;
        this.mouseMoveHandler = null;
        this.gutterHandlers = null;

        this.panes = null;
        this.ratios = null;
        this.windowIds = null;

        this.initWindowsPanes();
        this.initWindowIds();
        this.initGutters();
        this.init();
    }

    init() {
        this.containerBound = this.container.getBoundingClientRect();

        this.mouseUpHandler = this.stopDragging.bind(this);
        this.mouseMoveHandler = this.drag.bind(this);
        document.addEventListener("mouseup", this.mouseUpHandler);
        document.addEventListener("mousemove", this.mouseMoveHandler);

        // Detect container resize using ResizeObserver
        this.observeContainerResize();
    }

    initializeWindows() {

        this.ratios = null;
        this.windowIds = null;

        this.initWindowsPanes();
        this.initWindowIds();
        this.initGutters();
        this.resizeHandler();
    }

    destroy() {
        document.removeEventListener("mouseup", this.mouseUpHandler);
        document.removeEventListener("mouseup", this.mouseMoveHandler);

        this.destroyGutters();

        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }

        this.container = null;
        this.containerBound = null;
        this.callbackDrag = null;
        this.mouseUpHandler = null;
        this.mouseMoveHandler = null;

        this.panes = null;
        this.ratios = null;
        this.windowIds = null;
    }

    positionGutter(index) {
        let rect = this.panes[index].getBoundingClientRect();
        this.gutterHandlers[index].gutter.style.left = Math.round(rect.right) + "px"; // Place it at pane's right
    }

    startDragging(event, index) {
        //event.preventDefault();
        //event.stopPropagation();

        this.dragging = true;
        this.currentGutterIndex = index;
        this.startX = this.gutterHandlers[index].gutter.offsetLeft;
        this.startWidths = [
            this.panes[index].offsetWidth,
            this.panes[index + 1].offsetWidth,
        ];
        this.startWindowIds = [
            this.windowIds[index],
            this.windowIds[index + 1]
        ];

        // Disable text selection while dragging
        document.body.style.userSelect = "none";

        this.gutterHandlers[index].gutter.classList.add("focus");

        //this.callbackDrag(true, this.startWindowIds[0], this.startWindowIds[1]);
    }

    drag(event) {
        if (!this.dragging) return;

        //event.preventDefault();
        //event.stopPropagation();

        let dx = this.startX - event.clientX;
        if (dx > 0 && this.startWidths[0] - dx <= this.minSize) {
            this.gutterHandlers[this.currentGutterIndex].gutter.style.left = (this.startX - this.startWidths[0] + this.minSize) + "px";
        } else if (dx < 0 && this.startWidths[1] + dx <= this.minSize) {
            this.gutterHandlers[this.currentGutterIndex].gutter.style.left = (this.startX + this.startWidths[1] - this.minSize) + "px";
        } else {
            this.gutterHandlers[this.currentGutterIndex].gutter.style.left = event.clientX + "px";
        }

        //console.log('drag ' + this.gutterHandlers[this.currentGutterIndex].gutter.style.left);

    }

    stopDragging() {
        if (this.dragging) {
            this.dragging = false;
            document.body.style.userSelect = ""; // Re-enable text selection

            this.gutterHandlers[this.currentGutterIndex].gutter.classList.remove("focus");
            //console.log('stopDragging ' + this.gutterHandlers[this.currentGutterIndex].gutter.style.left);

            this.adjustPanelSizes();
            if (this.callbackDrag)
                this.callbackDrag(false, this.startWindowIds[0], this.startWindowIds[1]);
        }
    }

    resizeHandler() {
        if (this.ratios != null && this.ratios.length > 0) {

            this.containerBound = this.container.getBoundingClientRect();
            let totalWidth = this.containerBound.width;

            // Adjust pane widths based on original ratios
            let ratioSum = this.ratios.reduce((x, y) => {
                return x + y;
            });

            for (let i = 0; i < this.panes.length; i++) {
                let paneWidth = totalWidth * this.ratios[i] / ratioSum;
                this.panes[i].style.width = paneWidth + "px";
            }

            // Reposition gutters
            this.gutterHandlers.forEach((_, i) => this.positionGutter(i));
        }
    }

    observeContainerResize() {

        let resizeTimeout1;
        this.resizeObserver = new ResizeObserver(() => {
            clearTimeout(resizeTimeout1);
            resizeTimeout1 = setTimeout(() => {
                //console.log("Resize finished");
                this.resizeHandler();
            }, 200);
        });
        this.resizeObserver.observe(this.container);
    }

    adjustPanelSizes() {
        // adjust panel sizes
        var dx = this.gutterHandlers[this.currentGutterIndex].gutter.offsetLeft - this.startX;
        this.panes[this.currentGutterIndex].style.width = (this.startWidths[0] + dx) + "px";
        this.panes[this.currentGutterIndex + 1].style.width = (this.startWidths[1] - dx) + "px";

        //console.log("start drag startX: " + this.startX + " w1: " + this.startWidths[0] + " w2: " + this.startWidths[1]);
        
        // recalculate the ratio
        for (let i = 0; i < this.panes.length; i++) {
            this.ratios[i] = this.panes[i].offsetWidth / this.containerBound.width;
        }
    }

    initWindowsPanes() {
        this.ratios = [];

        this.panes = Array.from(this.container.getElementsByClassName("windows"));
        if (this.panes) {
            for (let i = 0; i < this.panes.length; i++) {
                this.ratios[i] = 1;
            }
        }
    }

    initWindowIds() {
        if (!this.panes) return;
        this.windowIds = [];

        this.panes.forEach((pane) => {
            var windowsRootPanel = pane.getElementsByClassName("windows-root-panel")[0];
            if (windowsRootPanel != null) {
                var windowId = windowsRootPanel.id;
                this.windowIds.push(windowId);
            } else {
                this.windowIds.push("");
            }
        });
    }

    initGutters() {

        this.currentGutterIndex = -1;
        this.destroyGutters();

        if (!this.panes) return;

        this.gutterHandlers = [];

        //this.container.querySelectorAll(".board-split-gutter").forEach((el) => el.remove());

        // Create gutters dynamically
        for (let i = 0; i < this.panes.length - 1; i++) {
            let gutter = document.createElement("div");
            gutter.className = "split-gutter board-split-gutter";

            this.container.appendChild(gutter);
            const handler = (event) => this.startDragging(event, i);
            gutter.addEventListener("mousedown", handler);

            this.gutterHandlers.push({ gutter, handler });
            this.positionGutter(i);
        }
    }

    destroyGutters() {
        if (this.gutterHandlers && this.gutterHandlers.length > 0) {
            this.gutterHandlers.forEach(({ gutter, handler }) => {
                gutter.removeEventListener("mousedown", handler);
                gutter.remove();
            });
            this.gutterHandlers = null;
        }
    }

    updateGuttersOnWindowsDragStart(targetIndex) {
        this.gutterHandlers.forEach(({ gutter, handler }) => {
            gutter.style.display = 'none';
        });
    }

    updateGuttersOnWindowsDropEnd(oldIndex, newIndex) {
        if (oldIndex != newIndex) {
            this._shiftWindows(oldIndex, newIndex);
            
            let left = 0;
            this.gutterHandlers.forEach(({ gutter, handler }, i) => {
                let rect = this.panes[i].getBoundingClientRect();
                gutter.style.left = left + rect.width + "px";
                left += rect.width;
            });
        }

        this.gutterHandlers.forEach(({ gutter, handler }) => {
            gutter.style.display = 'block';
        });
    }

    _shiftWindows(oldIndex, newIndex) {

        var oldPane = this.panes[oldIndex];
        var oldClusterTable = this.windowIds[oldIndex];
        var oldQuoteTable = this.ratios[oldIndex];

        if (oldIndex < newIndex) {

            for (var i = oldIndex; i < newIndex; i++) {
                this.panes[i] = this.panes[i + 1];
                this.windowIds[i] = this.windowIds[i + 1];
                this.ratios[i] = this.ratios[i + 1];
            }

        } else if (oldIndex > newIndex) {

            for (var i = oldIndex - 1; i >= newIndex; i--) {
                this.panes[i + 1] = this.panes[i];
                this.windowIds[i + 1] = this.windowIds[i];
                this.ratios[i + 1] = this.ratios[i];
            }
        } else {
            return;
        }

        this.panes[newIndex] = oldPane;
        this.windowIds[newIndex] = oldClusterTable;
        this.ratios[newIndex] = oldQuoteTable;

        //setTimeout(() => {
        //    this.panes.forEach((_, i) => {
        //        var rect = _.getBoundingClientRect();
        //        console.log(`---> ${rect.left} ${rect.width}`);
        //    });
        //}, 2000);
    }
}
