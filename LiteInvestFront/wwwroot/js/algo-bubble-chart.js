class AlgoBubbleChart {
    constructor(windowId, canvas, scrollCallback, config = {}) {
        this.windowId = windowId;
        this.canvas = canvas;
        this.scrollCallback = scrollCallback;
        this.ctx = this.canvas.getContext('2d');
        this.container = this.canvas.closest('.chart-container');
        this.wrapper = this.canvas.closest('.chart-wrapper');
        
        // Configuration with defaults
        this.config = {
            baseRadius: 15,
            baseVolume: 200,
            minVolume: 100,
            maxPoints: 100,
            canvasWidth: 2000,
            marginX: 2,
            marginY: 30,
            gridColor: '#e0e0e0',
            lineColor: '#808080',
            bubbleOpacity: 1.0,
            outlineColor: '#000000',
            outlineWidth: 1,
            labelColor: '#000000',
            labelFont: '0.8rem Roboto',
            filterVolume: 0, // Default to show all
            showVolumeLabels: true, // Default to show labels
            showSmallBubbles: true, // Default to show small bubbles
            // displayMode: 'bubbles and lines', // 'bubbles', 'lines', or 'bubbles and lines'
            displayMode: 0, // bubbles and lines: 0 | bubbles: 1 | lines: 2
            colors: {
                buy: '#69cbab',  // Green for buy (side='L' | long)
                sell: '#ffa09e'   // Red for sell (side='S' | short)
            },
            ...config
        };
        
        // Data storage
        this.data = [];
        this.priceMin = 0;
        this.priceMax = 100;
        this.priceRange = 100;
        this.lastXPosition = this.config.canvasWidth - this.config.marginX;
        this.fixedSpacing = this.config.canvasWidth / this.config.maxPoints;
        this.totalShift = 0; // Track total shift amount

        // Initialize
        this.initCanvas();
        this.initResizeObserver();
        this.initEventListeners();
        this.draw();
    }
    
    // Setter methods for outline and label properties
    setOutlineColor(color) {
        this.config.outlineColor = color;
        this.draw();
    }
    
    setOutlineWidth(width) {
        this.config.outlineWidth = width;
        this.draw();
    }
    
    setLabelColor(color) {
        this.config.labelColor = color;
        this.draw();
    }
    
    // Setter for volume label visibility
    setVolumeLabelVisibility(show, filterVolume = 0) {
        this.config.showVolumeLabels = show;
        this.config.filterVolume = filterVolume;
        this.draw();
    }
    
    // Setter for small bubble visibility
    setSmallBubbleVisibility(show, filterVolume = 0) {
        this.config.showSmallBubbles = show;
        this.config.filterVolume = filterVolume;
        this.draw();
    }
    
    // Setter for display mode
    setDisplayMode(mode) {
        this.config.displayMode = mode;
        this.draw();
    }
    
    initCanvas() {
        this.canvas.height = this.wrapper.clientHeight;
        this.canvas.width = this.config.canvasWidth;
    }
    
    initResizeObserver() {
        this.resizeObserver = new ResizeObserver(entries => {
            this.canvas.height = this.wrapper.clientHeight;
            this.draw();
        });
        this.resizeObserver.observe(this.wrapper);
    }
    
    initEventListeners() {
        // Fixed scrolling direction
        //this.canvas.addEventListener('wheel', (e) => {
        //    e.preventDefault();
        //    const delta = -e.deltaY * this.priceRange * 0.001; // Inverted delta
        //    this.priceMin += delta;
        //    this.priceMax += delta;
        //    //console.log(this.priceMin, this.priceMax);
        //    if (this.scrollCallback)
        //        this.scrollCallback(this.windowId, this.priceMin, this.priceMax, e.deltaY);
        //    this.draw();
        //});
        
        this.containerResizeObserver = new ResizeObserver(entries => {
            this.wrapper.style.right = '0';
            this.draw();
        });
        this.containerResizeObserver.observe(this.container);
    }
    
    setPriceBounds(min, max) {
        this.priceMin = min;
        this.priceMax = max;
        this.priceRange = max - min;
        //console.log("++++ " + this.priceMin + " " + this.priceMax + " " + this.priceRange);
        this.draw();
    }
    
    addTick(tick) {
        // Calculate bubble radius for this tick
        const radius = this.volumeToRadius(tick.volume);
        var delta = 0;
        if (this.data.length > 0) {
            const radius0 = this.volumeToRadius(this.data[this.data.length - 1].volume);
            delta = (this.fixedSpacing + radius) - radius0;
        }
        
        // Shift all existing points left by the new bubble's diameter plus fixed spacing
        this.data.forEach(point => {
            point.x -= delta;
        });
        
        // Add new tick at the right edge
        this.data.push({
            price: tick.price,
            volume: tick.volume,
            side: tick.side,
            radius: radius,
            x: this.config.canvasWidth - this.config.marginX - radius // Start at right edge minus radius
        });
        
        // Track total shift
        this.totalShift += delta;
        
        // Remove points that have scrolled off the left edge
        while (this.data.length > 0 && 
               this.data[0].x + this.data[0].radius < 0) {
            this.data.shift();
        }
        
        // Remove oldest point if we've reached max points
        if (this.data.length > this.config.maxPoints) {
            this.data.shift();
        }
        
        this.draw();
    }
    
    addTicks(ticks) {
        ticks.forEach(tick => this.addTick(tick));
    }
    
    clear() {
        this.data = [];
        this.lastXPosition = this.config.canvasWidth - this.config.marginY;
        this.draw();
    }
    
    draw() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.drawGrid();
        if (this.data.length === 0) return;
        
        // Draw based on display mode
        if (this.config.displayMode === 2 || this.config.displayMode === 0) {
            this.drawLines();
        }
        if (this.config.displayMode === 1 || this.config.displayMode === 0) {
            this.drawBubbles();
        }
    }
    
    drawGrid() {
        this.ctx.strokeStyle = this.config.gridColor;
        this.ctx.lineWidth = 1;
        const gridStep = this.calculateGridStep();
        
        for (let price = Math.ceil(this.priceMin / gridStep) * gridStep; 
                price <= this.priceMax; 
                price += gridStep) {
            
            const y = this.priceToY(price);
            this.ctx.beginPath();
            this.ctx.moveTo(0, y);
            this.ctx.lineTo(this.canvas.width, y);
            this.ctx.stroke();
        }
    }
    
    calculateGridStep() {
        const range = this.priceRange;
        const log10 = Math.floor(Math.log10(range));
        const fraction = range / Math.pow(10, log10);
        
        if (fraction > 5) return Math.pow(10, log10);
        if (fraction > 2) return Math.pow(10, log10) / 2;
        return Math.pow(10, log10) / 5;
    }
    
    drawLines() {
        this.ctx.strokeStyle = this.config.lineColor;
        this.ctx.lineWidth = 1;
        this.ctx.beginPath();
        
        // Find the first visible point (not off left edge)
        let firstVisibleIndex = 0;
        while (firstVisibleIndex < this.data.length && 
               this.data[firstVisibleIndex].x + this.data[firstVisibleIndex].radius < 0) {
            firstVisibleIndex++;
        }
        
        if (firstVisibleIndex >= this.data.length) return;
        
        // Start with first visible point
        const firstPoint = this.data[firstVisibleIndex];
        this.ctx.moveTo(
            Math.max(0, firstPoint.x), 
            this.priceToY(firstPoint.price)
        );
        
        // Draw lines to remaining points with fixed spacing
        for (let i = firstVisibleIndex + 1; i < this.data.length; i++) {
            const point = this.data[i];
            this.ctx.lineTo(point.x, this.priceToY(point.price));
        }
        
        this.ctx.stroke();
    }

    drawBubbles() {
        // Draw from oldest to newest (left to right)
        for (let i = 0; i < this.data.length; i++) {
            const point = this.data[i];
            
            // Skip if volume is below filter and we're hiding small bubbles
            if (!this.config.showSmallBubbles && point.volume < this.config.filterVolume) {
                continue;
            }
            
            // Position bubbles with fixed spacing
            let x = point.x;
            const y = this.priceToY(point.price);
            const radius = point.radius;
            const color = point.side === "L" ? this.config.colors.buy : this.config.colors.sell;
            
            // Skip if completely off left edge
            if (x + radius < 0) continue;
            
            // Skip if completely off right edge
            if (x - radius > this.canvas.width) continue;
            
            // Draw bubble outline
            this.ctx.beginPath();
            this.ctx.arc(x, y, radius, 0, Math.PI * 2);
            this.ctx.strokeStyle = this.config.outlineColor;
            this.ctx.lineWidth = this.config.outlineWidth;
            this.ctx.stroke();
            
            // Draw bubble fill
            this.ctx.beginPath();
            this.ctx.arc(x, y, radius, 0, Math.PI * 2);
            this.ctx.fillStyle = color;
            this.ctx.globalAlpha = this.config.bubbleOpacity;
            this.ctx.fill();
            this.ctx.globalAlpha = 1;
            
            // Draw volume label if enabled and volume is above filter
            if (this.config.showVolumeLabels || 
                (this.config.filterVolume === 0 || point.volume >= this.config.filterVolume)) {
                this.ctx.fillStyle = this.config.labelColor;
                this.ctx.font = this.config.labelFont;
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'middle';
                this.ctx.fillText(point.volume.toString(), x, y + 1);
            }
        }
    }
    
    priceToY(price) {
        const clampedPrice = Math.min(this.priceMax, Math.max(this.priceMin, price));
        const ratio = (clampedPrice - this.priceMin) / this.priceRange;
        return this.canvas.height - this.config.marginY - (ratio * (this.canvas.height - 2 * this.config.marginY));
    }
    
    volumeToRadius(volume) {
        // Ensure volume is at least minVolume
        //const effectiveVolume = Math.max(volume, this.config.minVolume);
        //return (effectiveVolume / this.config.baseVolume) * this.config.baseRadius;

        const magnitude = volume === 0 ? 0 : Math.floor(Math.log10(volume));
        const scaleFactor = magnitude >= 1 ? magnitude * 0.6 : 0;
        const radius = this.config.baseRadius * (scaleFactor + 1);
        return radius;
    }
}

class VirtualScrollingChart {
    constructor(config) {
        this.secId = config.secId;
        this.windowId = config.windowId;
        this.orderVolumes = config.orderVolumes;
        this.selectedOrderVolumeIndex = config.selectedOrderVolumeIndex;
        this.scrollCallback = config.scrollCallback;
        this.orderVolumeUpdateCallback = config.orderVolumeUpdateCallback;
        this.orderVolumeSelectionChangedCallback = config.orderVolumeSelectionChangedCallback;
        this.containerClass = config.containerClass;

        this.windows = document.getElementById(config.windowId);
        var escapedWindowId = CSS.escape(config.windowId);
        this.container = document.querySelector(`#${escapedWindowId} .${config.containerClass}`);

        this.chartBody = this._createBody();
        this.scaleBody = this._createScaleBody();
        this.orderVolumeBody = this._createOrderVolumeBody();
        this.footerPNL = this._createFooterPNL();
        this.container.appendChild(this.chartBody);
        this.container.appendChild(this.scaleBody);
        this.container.appendChild(this.orderVolumeBody);
        this.container.appendChild(this.footerPNL);

        this.chart = this._createChart();
        this.setScale(config.scale);
    }

    _createBody() {
        const chartBody = document.createElement('div');
        chartBody.className = 'chart-wrapper-wrapper';

        const chartWrapper = document.createElement('div');
        chartWrapper.className = 'chart-wrapper';

        const chartCanvas = document.createElement('canvas');
        chartCanvas.className = 'chart-canvas';

        chartWrapper.appendChild(chartCanvas);
        chartBody.appendChild(chartWrapper);

        this.chartCanvas = chartCanvas;

        return chartBody;
    }

    _createChart() {
        const chart = new AlgoBubbleChart(this.windowId, this.chartCanvas, this.scrollCallback, {
            baseRadius: 7,
            baseVolume: 100,
            minVolume: 100,
            maxPoints: 100,
            canvasWidth: 2000,
            outlineColor: '#4d4d4d',
            outlineWidth: 2,
            labelColor: '#292b2c',
            labelFont: '0.9rem Roboto',
            filterVolume: 150,
            displayMode: 0 // 'bubbles and lines' // Default display mode
        });

        // Set initial price bounds
        //chart.setPriceBounds(22200, 22300);

        // Example of using the new setter methods:
        // chart.setOutlineColor('#ff0000');
        // chart.setOutlineWidth(2);
        // chart.setLabelColor('#ffffff');
        // chart.setVolumeLabelVisibility(false, 150); // Hide labels for volumes < 150
        // chart.setSmallBubbleVisibility(true, 150); // Hide bubbles for volumes < 150
        // chart.setDisplayMode(2); // Show only lines

        // Simulate real-time data updates
        //setInterval(() => {
        //    const lastPrice = chart.data.length > 0
        //        ? chart.data[chart.data.length - 1].price
        //        : 22250;

        //    const change = (Math.random() - 0.5) * 10;
        //    const newPrice = lastPrice + change;
        //    const side = Math.random() > 0.5 ? 1 : 0;
        //    const volume = Math.floor(10 + Math.random() * 300); // Some volumes will be below minVolume

        //    chart.addTick({
        //        price: newPrice,
        //        volume: volume,
        //        side: side
        //    });
        //}, 500);

        return chart;
    }

    _createScaleBody() {
        const scaleBody = document.createElement('div');
        scaleBody.className = 'label-scale';
        return scaleBody;
    }

    _createOrderVolumeBody() {
        const volumeBody = document.createElement('div');
        volumeBody.className = 'like-trades-volume-select-wrapper-wrapper';

        const volumeSelectWrapper = document.createElement('div');
        volumeSelectWrapper.classList = 'algo-select-wrapper like-trades-volume-select-wrapper d-none';

        volumeSelectWrapper.innerHTML = `<div class="algo-select small">
            <span class="selected-text"></span>
            <i class="bi bi-chevron-down icon"></i>
            </div>
            <div class="select-dropdown small"></div>`;

        const volumeListContainer = document.createElement('div');
        volumeListContainer.classList = 'algo-list small like-trades-volume-list';

        volumeBody.appendChild(volumeSelectWrapper);
        volumeBody.appendChild(volumeListContainer);

        //const orderAmountItems = [
        //    { title: 'F', value: 'F'},
        //    { title: 'D', value: 'D'},
        //    { title: 'L', value: 'L'}
        //];
        //var orderAmountSelectContainer = document.querySelector(`#${escapedWindowId} .${config.containerClass} .like-trades-volume-select-wrapper-wrapper .like-trades-volume-select-wrapper`);
        //this.orderAmountSelect = new AlgoSelect(orderAmountSelectContainer, {
        //    items: orderAmountItems,
        //    defaultValue: 'F',
        //    onChange: (value) => {
        //        console.log(value);
        //    }
        //});

        let orderVolumeListItems = [];
        this.orderVolumes.forEach((_, i) => {
            orderVolumeListItems[i] = ({ col1: `${i + 1}`, value: _ });
        });

        this.orderVolumeList = new AlgoList(volumeListContainer, {
            //header: { col1: '$', col2: 'Lots' },
            //items: [
            //    { col1: '50', col2: '3.7', value: 1 },
            //    { col1: '100', col2: '7.4', value: 2 },
            //    { col1: '150', col2: '11.1', value: 3 },
            //    { col1: '200', col2: '14.8', value: 4 },
            //    { col1: '250', col2: '18.6', value: 5 }
            //],
            items: orderVolumeListItems,
            defaultIndex: this.selectedOrderVolumeIndex,
            minValue: 1,
            onChange: (index, val) => {
                this.selectedOrderVolumeIndex = index;
                if (this.orderVolumeSelectionChangedCallback)
                    this.orderVolumeSelectionChangedCallback(this.windowId, index, val);
            }
        });

        this.orderVolumePopup = new AlgoPopup(this.windows, this.orderVolumeList, {
            minValue: 1,
            onAcceptInput: (index, value) => {
                console.log('Accepted:', index, value);
                this.orderVolumes[index] = value;
                if (this.orderVolumeUpdateCallback)
                    this.orderVolumeUpdateCallback(this.windowId, index, value);
            },
            onDiscardInput: () => {
                console.log('Discarded input');
            }
        });

        return volumeBody;
    }

    _createFooterPNL() {
        const footer = document.createElement('div');
        footer.classList.add('algo-footer-pnl');
        footer.classList.add('algo-footer-pnl-amount');
        footer.classList.add('d-none');
        footer.classList.add('noselect');
        return footer;
    }

    setFooterPNLVisibility(visible, amount) {
        if (this.footerPNL) {
            if (visible) {
                this.footerPNL.classList.remove('d-none');
                if (amount >= 0) {
                    if (!this.footerPNL.classList.contains('algo-footer-profit')) {
                        this.footerPNL.classList.add('algo-footer-profit');
                        this.footerPNL.classList.remove('algo-footer-loss');
                    }
                } else {
                    if (!this.footerPNL.classList.contains('algo-footer-loss')) {
                        this.footerPNL.classList.add('algo-footer-loss');
                        this.footerPNL.classList.remove('algo-footer-profit');
                    }
                }
                this.footerPNL.textContent = `${amount.toLocaleString()}${__('pnlL')}`;
                this.orderVolumeBody.style.bottom = '38px';
            } else {
                if (!this.footerPNL.classList.contains('d-none')) {
                    this.footerPNL.classList.add('d-none');
                    this.footerPNL.classList.remove('algo-footer-loss');
                    this.footerPNL.classList.remove('algo-footer-profit');
                    this.footerPNL.textContent = '';
                    this.orderVolumeBody.style.bottom = '8px';
                }
            }
        }
    }

    destroy() {
        // DO NOT FLIP THIS DESTROY ORDER
        this.orderVolumePopup.destroy();
        this.orderVolumeList.destroy();
        this.orderVolumeList = null;
        this.orderVolumePopup = null;
        this.orderVolumeUpdateCallback = null;
        this.orderVolumeSelectionChangedCallback = null;

        // DO NOT REMOVE FOR FUTURE OPTIMIZATION
        //this.footerPNL.replaceChildren();
        //this.container.replaceChildren();

        this.containerClass = null;
        this.windows = null;
        this.secId = null;
        this.windowId = null;
        this.container = null;
        this.footerPNL = null;
        this.chartBody = null;
        this.scaleBody = null;
        this.orderVolumeBody = null;
    }

    refreshOrderVolume(index, value) {
        if (this.orderVolumeList != null) {
            this.orderVolumeList.setValue(index, value, true);
        }
    }

    getSelectedOrderVolume() {
        if (!this.orderVolumes || this.selectedOrderVolumeIndex >= this.orderVolumes.length || this.selectedOrderVolumeIndex < 0)
            return 1;
        return this.orderVolumes[this.selectedOrderVolumeIndex];
    }

    setScale(scale) {
        this.scaleBody.textContent = `${scale}x`;
    }

    addTicks(ticks) {
        if (!this.chart) return;
        this.chart.addTicks(ticks);
    }

    setPriceBounds(min, max) {
        if (!this.chart) return;
        this.chart.setPriceBounds(min, max);
    }
}