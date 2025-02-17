class MainSplitter {

    constructor(containerId) {

        this.paneSizeRatios = [1, 1.2, 0.6];
        this.minSizes = [30, 60, 60];


        this.container = document.getElementById(containerId);
        this.panes = Array.from(this.container.getElementsByClassName("main-split-pane"));
        this.gutters = [];
        this.containerBound = new DOMRect();
        this.ratios = [];
        this.init();
    }

    init() {
        this.containerBound = this.container.getBoundingClientRect();

        //let ratioSum = this.paneSizeRatios.reduce((x, y) => {
        //    return x + y;
        //})
        for (let i = 0; i < this.panes.length; i++) {
            //let paneWidth = this.containerBound.width * this.paneSizeRatios[i] / ratioSum;
            //this.panes[i].style.width = paneWidth + "px";
            this.ratios[i] = this.paneSizeRatios[i];
        }

        // Create gutters dynamically
        for (let i = 0; i < this.panes.length - 1; i++) {
            let gutter = document.createElement("div");
            gutter.className = "split-gutter";
            this.container.appendChild(gutter);
            this.gutters.push(gutter);
            this.positionGutter(i);

            gutter.addEventListener("mousedown", (event) => this.startDragging(event, i));
        }

        document.addEventListener("mouseup", () => this.stopDragging());
        document.addEventListener("mousemove", (event) => this.drag(event));

        // Detect window/container resize
        window.addEventListener("resize", () => this.resizeHandler());

        // Detect container resize using ResizeObserver
        this.observeContainerResize();
    }

    positionGutter(index) {
        let rect = this.panes[index].getBoundingClientRect();
        this.gutters[index].style.left = rect.right - this.containerBound.left + "px"; // Place it at pane's right
    }

    startDragging(event, index) {
        event.preventDefault(); // Prevent default behaviors
        event.stopPropagation(); // Stop event propagation

        this.dragging = true;
        this.currentGutterIndex = index;
        this.startX = event.clientX;
        this.startWidths = [
            this.panes[index].offsetWidth,
            this.panes[index + 1].offsetWidth,
        ];

        // Disable text selection while dragging
        document.body.style.userSelect = "none";
    }

    drag(event) {
        if (!this.dragging) return;

        event.preventDefault(); // Prevent default behaviors
        event.stopPropagation(); // Stop event propagation

        let dx = event.clientX - this.startX;
        let newWidth1 = this.startWidths[0] + dx;
        let newWidth2 = this.startWidths[1] - dx;

        if (this.currentGutterIndex == 0) {
            if (newWidth1 <= this.minSizes[0]) {
                this.panes[0].style.width = "1px";
                this.panes[1].style.width = (this.containerBound.width - this.panes[2].offsetWidth - 1) + "px";
                this.positionGutter(0);
                return;
            }
        } else {
            if (this.currentGutterIndex == 1) {
                if (this.panes[0].offsetWidth == 1 && newWidth1 <= this.minSizes[1]) {
                    this.panes[1].style.width = "1px";
                    this.panes[2].style.width = (this.containerBound.width - 1) + "px";
                    this.positionGutter(1);
                    return;
                }
            }
        }

        let minSize1 = 0, minSize2 = 0;
        if (this.currentGutterIndex == 0) {
            minSize1 = this.minSizes[0];
            minSize2 = this.minSizes[1];
        } else {
            minSize1 = this.minSizes[1];
            minSize2 = this.minSizes[2];
        }
        if (newWidth1 > minSize1 && newWidth2 > minSize2) {
            this.panes[this.currentGutterIndex].style.width = newWidth1 + "px";
            this.panes[this.currentGutterIndex + 1].style.width = newWidth2 + "px";
            this.positionGutter(this.currentGutterIndex); // Update gutter position
        }

    }

    stopDragging() {
        this.dragging = false;
        document.body.style.userSelect = ""; // Re-enable text selection
        this.recalculateOriginalRatio();
    }

    resizeHandler() {
        this.containerBound = this.container.getBoundingClientRect();
        let totalWidth = this.containerBound.width;

        // Adjust pane widths based on original ratios

        let ratioSum = this.ratios.reduce((x, y) => {
            return x + y;
        });

        let widthSum = 0;

        let paneWidth2 = this.containerBound.width * this.ratios[2] / ratioSum;
        if (paneWidth2 < this.minSizes[2]) {
            paneWidth2 = this.minSizes[2];
        }
        this.panes[2].style.width = paneWidth2 + "px";

        let paneWidth1 = this.containerBound.width * this.ratios[1] / ratioSum;
        if (paneWidth1 < this.minSizes[1]) {
            paneWidth1 = this.minSizes[1];
        }

        if (totalWidth - paneWidth1 - paneWidth2 < this.minSizes[0]) {
            this.panes[0].style.width = "1px";
            this.panes[1].style.width = (totalWidth - paneWidth2 - 1) + "px";
        } else {
            this.panes[0].style.width = (totalWidth - paneWidth1 - paneWidth2) + "px";
            this.panes[1].style.width = paneWidth1 + "px";
        }

        //for (let i = 0; i < this.panes.length; i++) {
        //    let paneWidth = this.containerBound.width * this.ratios[i] / ratioSum;
        //    this.panes[i].style.width = paneWidth + "px";
        //}

        // Reposition gutters
        this.gutters.forEach((_, i) => this.positionGutter(i));
    }

    observeContainerResize() {
        const resizeObserver = new ResizeObserver(() => {
            this.resizeHandler();
        });

        resizeObserver.observe(this.container);
    }

    recalculateOriginalRatio() {
        // recalculate the ratio
        for (let i = 0; i < this.panes.length; i++) {
            this.ratios[i] = this.panes[i].offsetWidth / this.containerBound.width;
        }
    }
}
