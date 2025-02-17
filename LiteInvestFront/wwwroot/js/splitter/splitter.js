class Splitter {

    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.panes = Array.from(this.container.getElementsByClassName("split-pane"));
        this.gutters = [];
        this.containerBound = new DOMRect();
        this.minSizes = [30, 30, 30];

        this.ratios = [];
        this.sizes = [];

        this.init();
    }

    init() {
        this.containerBound = this.container.getBoundingClientRect();
        for (let i = 0; i < this.panes.length; i++) {
            this.ratios[i] = 1;
            this.sizes[i] = 0;
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
                // collapse pane1
                this.panes[0].style.width = "1px";
                //this.panes[1].style.width = (this.containerBound.width - this.panes[2].offsetWidth - 1) + "px";
                this.panes[1].style.width = (this.containerBound.width - this.panes[2].offsetWidth) + "px";
                this.positionGutter(0);
                return;
            }
        } else {
            if (this.currentGutterIndex == 1) {
                if (this.panes[0].offsetWidth == 1 && newWidth1 <= this.minSizes[1]) {
                    // collapse pane2
                    this.panes[1].style.width = "1px";
                    //this.panes[2].style.width = (this.containerBound.width - 1) + "px";
                    this.panes[2].style.width = this.containerBound.width + "px";
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
        if (newWidth2 < minSize2) {
            newWidth2 = minSize2;
            this.containerBound = this.container.getBoundingClientRect();
            if (this.currentGutterIndex == 1) {
                newWidth1 = this.containerBound.width - this.panes[0].offsetWidth - minSize2;
            } else if (this.currentGutterIndex == 0) {
                newWidth1 = this.containerBound.width - this.panes[2].offsetWidth - minSize2;
            }
        }

        if (newWidth1 >= minSize1 && newWidth2 >= minSize2) {
            this.panes[this.currentGutterIndex].style.width = newWidth1 + "px";
            this.panes[this.currentGutterIndex + 1].style.width = newWidth2 + "px";
            this.positionGutter(this.currentGutterIndex); // Update gutter position
        }

    }

    stopDragging(event) {
        this.dragging = false;
        document.body.style.userSelect = ""; // Re-enable text selection
        this.recalculateOriginalRatio();
    }

    resizeHandler() {
        this.containerBound = this.container.getBoundingClientRect();
        let totalWidth = this.containerBound.width;

        let h = -1;
        let n = this.panes.length;
        let total = totalWidth;

        let allCollapsed = true;
        for (let i = 0; i < n; i++) {
            if (this.ratios[i] != 0) {
                allCollapsed = false;
                break;
            }
        }
        if (allCollapsed) {
            for (let i = 0; i < n; i++) {
                this.ratios[i] = 1;
            }
        }

        let ratioSum = 0;
        for (let i = 0; i < n; i++) {
            ratioSum += this.ratios[i];
        }
        for (let k = 0; k < n; k++) {
            this.sizes[k] = totalWidth * this.ratios[k] / ratioSum;
        }

        //console.log(totalWidth);

        while (h < n) {

            let need_collapse = false;

            // 1
            for (let k = h + 1; k < n; k++) {
                if (this.sizes[k] < this.minSizes[k]) {
                    this.sizes[k] = this.minSizes[k];

                    /////////////////////////
                    // distribute sizes
                    /////////////////////////

                    // calculate totalc, ratioSumc
                    let sizesSumTemp = 0;
                    let ratioSumc = 0;
                    for (let i = h + 1; i < n; i++) {
                        if (this.sizes[i] == this.minSizes[i]) {
                            sizesSumTemp += this.sizes[i];
                        } else {
                            ratioSumc += this.ratios[i];
                        }
                    }
                    let totalc = total - sizesSumTemp;

                    if (totalc < 0) {
                        need_collapse = true;
                    } else {
                        for (let p = h + 1; p < n; p++) {
                            if (this.sizes[p] != this.minSizes[p]) {
                                this.sizes[p] = totalc * this.ratios[p] / ratioSumc;
                            }
                        }

                        var existSmaller = false;
                        var existBigger = false;
                        for (let p = h + 1; p < n; p++) {
                            if (this.sizes[p] < this.minSizes[p]) {
                                existSmaller = true;
                            } else if (this.sizes[p] > this.minSizes[p]) {
                                existBigger = true;
                            }
                        }

                        if (existSmaller && existBigger) {
                            // check from beginning
                            k = h + 1 - 1; // for ++
                        }
                    }
                }
            }

            // 2
            if (!need_collapse) {
                for (let k = h + 1; k < n; k++) {
                    if (this.sizes[k] < this.minSizes[k]) {
                        need_collapse = true;
                        break;
                    }
                }
            }

            // 3
            if (need_collapse) {
                h++;
                this.sizes[h] = 1;
                this.ratios[h] = 0;
                //total = totalWidth - 1 * (h + 1);
                total = totalWidth;

                let ratioSumc = 0;
                for (let i = h + 1; i < n; i++) {
                    ratioSumc += this.ratios[i];
                }
                for (let k = h + 1; k < n; k++) {
                    this.sizes[k] = total * this.ratios[k] / ratioSumc;
                }
            } else {
                break;
            }
        }

        this.panes.forEach((pane, i) => {
            pane.style.width = this.sizes[i] + "px";
        });

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
