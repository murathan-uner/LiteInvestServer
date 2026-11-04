const COLOR_HIGH = '#edc6c6'; // red
const COLOR_LOW = '#a0dbc6'; // green
const COLOR_HIGH_HIGHLIGHT = '#ffa09e'; // thick red
const COLOR_LOW_HIGHLIGHT = '#69caac'; // thick green
const COLOR_VOL_LOW = '#c7a805'; // yellow
const COLOR_VOL_HIGH = '#c85dd9'; // purple

const containerWidths = [30, 40, 30];
const containerMinWidths = [30, 8, 100];

let windowsSplitter = null;
let sortable = null;
let windowInstances = null;
let scaleList = null;

// Invoked from Menu after logged in & populated from Plaza
window.setActiveOrders = (orders) => {
    activeOrders = orders;

    // Populating active orders from Plaza maybe performed after opening the dialog
    if (activeOrdersDialog) {
        if (activeOrdersDialog.getActiveTab() === 'active')
            activeOrdersDialog.setData(activeOrders);
    }
    console.log("setActiveOrders >>> _iUpdateByOrderChanged", activeOrders);
    _iUpdateByOrderChanged();
};

window.setCanceledOrders = (orders) => {
    if (activeOrdersDialog) {
        if (activeOrdersDialog.getActiveTab() === 'canceled')
            activeOrdersDialog.setData(orders);
    }
};

window.setExecutedOrders = (orders) => {
    if (activeOrdersDialog) {
        if (activeOrdersDialog.getActiveTab() === 'executed')
            activeOrdersDialog.setData(orders);
    }
};

// Invoked when new orders arrived from server service - SubscribeForPrivateOrders
window.setNewOrders = (newOrders) => {
    if (!newOrders || newOrders.length == 0) return;

    if (!activeOrders) activeOrders = [];

    // Create a Map of new orders for O(1) lookups
    const newOrderMap = new Map(newOrders.map(order => [order.id, order]));

    // Update existing orders (if they appear in newOrders)
    activeOrders.forEach(order => {
        const updatedOrder = newOrderMap.get(order.id);
        if (updatedOrder) {
            // Overwrite the existing order with new data
            Object.assign(order, updatedOrder);
        }
    });

    // Add new orders (only if they're ACTIVE/PARTIAL and not already in activeOrders)
    const activeOrderIds = new Set(activeOrders.map(order => order.id));
    for (const order of newOrders) {
        if (
            !activeOrderIds.has(order.id) &&
            (order.state === ORDER_STATE_ACTIVE || order.state === ORDER_STATE_PARTIAL)
        ) {
            activeOrders.push(order);
        }
    }

    // Finally, filter out any orders that changed to non-active states (CANCELED/FAILED/EXECUTED)
    activeOrders = activeOrders.filter(
        order => order.state === ORDER_STATE_ACTIVE || order.state === ORDER_STATE_PARTIAL
    );

    //console.log(activeOrders);
    if (activeOrdersDialog)
        activeOrdersDialog.setNewData(newOrders);

    console.log("setNewOrders >>> _iUpdateByOrderChanged", activeOrders);
    //_iUpdateByOrderChanged(newOrders);
    _iUpdateByOrderChanged();
};

window.setOpenedPositions = (positions) => {
    openedPositions = positions;

    if (activePositionsDialog) {
        if (activePositionsDialog.getActiveTab() === 'opened')
            activePositionsDialog.setData(openedPositions);
        // TODO
        //else
        //    activePositionsDialog.setData(closedPositions);
    }

    console.log("setOpenedPositions >>> _iUpdateByOpenedPositions", openedPositions);
    _iUpdateByOpenedPositions();
};

window.setClosedPositions = (positions) => {
    if (activePositionsDialog) {
        if (activePositionsDialog.getActiveTab() === 'closed')
            activePositionsDialog.setData(positions);
    }
};

// Invoked from ClustersComponent component
window.updateClusterTableDataAll = function (windowId, newData) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.updateData(newData, -1);

        // Notify
        DOTNET_HOME.invokeMethodAsync('OnAcceptedClusterData', windowId);
    }
};

// Ticks2, Ticks3
// Invoked from ClustersComponent component
window.updateClusterTableDataPrevious = function (windowId, newData) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.updateDataBatch(newData, [1, 2]);

        // Notify
        DOTNET_HOME.invokeMethodAsync('OnAcceptedClusterData', windowId);
    }
};

// Ticks1
// Invoked from ClustersComponent component
window.updateClusterTableDataLast = function (windowId, newData) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.updateData(newData, 0);

        // Notify
        DOTNET_HOME.invokeMethodAsync('OnAcceptedClusterData', windowId);
    }
};

// Invoked from ClustersComponent component
window.updateClusterFooterText = function (windowId, newFooterTexts) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.updateFooterText(newFooterTexts);
    }
};

// Invoked from QuotesComponent component
window.updateQuoteTableData = function (windowId, newData, forceToMiddle, bestBidIndex, bestAskPrice, bestBidPrice, scale) {

    const window = _iGetWindowInstanceByWindowId(windowId);

    if (window) {
        const quoteInstance = window.quote;
        const clusterInstance = window.cluster;

        quoteInstance.bestBidIndex(bestBidIndex);
        quoteInstance.bestAskPrice(bestAskPrice);
        quoteInstance.bestBidPrice(bestBidPrice);
        quoteInstance.scale(scale);
        quoteInstance.updateData(newData);

        // update PnL
        if (openedPositions) {
            const pos = openedPositions.find(pos => pos.secId === window.secId);
            if (pos) {
                pos.exitPrice = pos.side === "L" ? bestBidPrice : bestAskPrice;
                const PnL = _iCalculatePnL(pos.enterPrice, pos.priceStep, pos.priceStepCost, pos.exitPrice, pos.curPos);
                window.cluster.setFooterPNLVisibility(true, pos.enterPrice);
                window.chart.setFooterPNLVisibility(true, pos.curPos);
                window.quote.setFooterPNLVisibility(true, PnL.P, PnL.R, PnL.Percent);
            } else {
                window.cluster.setFooterPNLVisibility(false);
                window.chart.setFooterPNLVisibility(false);
                window.quote.setFooterPNLVisibility(false);
            }
        }
        // TODO - if ActivePositions dialog needs to show PnL, call activePositionsDialog.setData(openedPositions)
        //
        if (activePositionsDialog) {
            if (activePositionsDialog.getActiveTab() === 'opened')
                activePositionsDialog.refreshData();
        }

        // scroll if necessary
        if (forceToMiddle === true) {
            //console.log('scroll to middle A-1');
            _evScrollBestBidIndex(windowId, window);
            //console.log('scroll to middle A-2');
        } else {
            // mouse cursor is out of quote/cluster &
            //      case 1: best bid index is out of range (10%, 90%)
            //      case 2: best bid ask is out of range (max price, min price)

            var isMouseOver = quoteInstance.isMouseOver();
            if (!isMouseOver) {
                isMouseOver = clusterInstance.isMouseOver();
            }

            if (!isMouseOver && quoteInstance.isAutomicCentering()) {
                var needScroll = false;

                var screenItemsLen = quoteInstance.getScreenItemsLen();
                var first = quoteInstance.getUpperVisibleIndex();
                var last = first + screenItemsLen - 1;

                // case 1
                var limitDelta = Number.parseInt(screenItemsLen * 10 / 100);
                var firstLimit = first + limitDelta;
                var lastLimit = last - limitDelta;

                if (bestBidIndex < firstLimit || bestBidIndex > lastLimit) {
                    //console.log(`best index-${bestBidIndex} upper-${first} lower-${last} upperL-${firstLimit} lowerL-${lastLimit}`);
                    console.log('bid index is out of range');
                    needScroll = true;
                }
                else {

                    // case 2
                    var maxVisiblePrice = quoteInstance.getPrice(first);
                    var minVisiblePrice = quoteInstance.getPrice(last);

                    if (bestAskPrice > maxVisiblePrice || bestAskPrice < minVisiblePrice) {
                        //console.log(`best ask-${bestAskPrice} upper-${first} lower-${last} upperPrice-${maxVisiblePrice} lowerPrice-${minVisiblePrice}`);
                        console.log('bid ask price is out of range');
                        needScroll = true;
                    }
                }

                if (needScroll) {
                    //console.log('scroll to middle B-1');
                    _evScrollBestBidIndex(windowId, window);
                    //console.log('scroll to middle B-2');
                }
            }
        }


        // Notify
        DOTNET_HOME.invokeMethodAsync('OnAcceptedQuoteData', windowId);
    }
};

// Invoked from Chart component
window.addChartTicks = function (windowId, ticks) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.chart.addTicks(ticks);

        // Notify
        DOTNET_HOME.invokeMethodAsync('OnAcceptedChartData', windowId);
    }
};

// Invoked from WorkWindowSettings component
window.orderVolumeUpdate = function (windowId, index, volume) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.chart.refreshOrderVolume(index, volume);
    }
}

// Callback function
// Invoked from quotes table | ActiveOrdersDialog
function _cancelOrderCallback(orderId) {
    DOTNET_WINDOWS.invokeMethodAsync('OnCancelOrder', orderId);
};

// Callback function
// Invoked from ActivePositionsDialog
function _closePositionCallback(secId) {
    DOTNET_WINDOWS.invokeMethodAsync('OnClosePosition', secId);
}

// Callback function
// Invoked from quotes table
function _orderClickLeftCallback(windowId, secId, price) {
    price = price === undefined ? 0 : parseFloat(price);

    var volume = 1;
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window)
        volume = window.chart.getSelectedOrderVolume();

    DOTNET_WINDOWS.invokeMethodAsync('OnSendOrderLong', secId, price, volume);
};

// Callback function
// Invoked from quotes table
function _orderClickRightCallback(windowId, secId, price) {
    price = price === undefined ? 0 : parseFloat(price);

    var volume = 1;
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window)
        volume = window.chart.getSelectedOrderVolume();

    DOTNET_WINDOWS.invokeMethodAsync('OnSendOrderShort', secId, price, volume);
};

// Callback function
// Invoked from chart
function _orderVolumeUpdateCallback(windowId, index, newVolume) {
    DOTNET_WINDOWS.invokeMethodAsync('OnUpdatedOrderVolume', windowId, index, newVolume);
};

// Callback function
// Invoked from chart
function _orderVolumeSelectionChangedCallback(windowId, index, volume) {
    DOTNET_WINDOWS.invokeMethodAsync('OnChangeOrderVolumeSelection', windowId, index, volume);
};

// Callback function
// Invoked from windows pane
function _closeCallback(windowId) {

    _iCloseWindows(windowId);

    // DO NOT REMOVE FOR FUTURE OPTIMIZATION
    //let windows = document.getElementById(windowId);
    //const windowsContainer = windows.parentElement;
    //if (windowsContainer) {
    //    windowsContainer.remove();
    //}

    DOTNET_WINDOWS.invokeMethodAsync('OnClosingWindow', windowId);

    //let boardParent = document.getElementById("board").parentElement;
    //if (boardParent) {
    //    if (windowsSplitter != null) {
    //        windowsSplitter.initializeWindows();
    //    }
    //}

    //new MutationObserver((_, observer) => {

    //    if (boardParent) {
    //        observer.disconnect();
    //    }
    //}).observe(document.body, { childList: true, subtree: true });
}

// Invoked from windows pane
function _openSettingCallback(windowId) {
    DOTNET_WINDOWS.invokeMethodAsync('OnOpenSetting', windowId);
}

// Event handler
function _evChangeScale(windowId, up) {
    if (!scaleList) return;

    let changed = false;
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (up) {
        if (window.scaleIndex < scaleList.length - 1) {
            window.scaleIndex++;
            changed = true;
        }
    } else {
        if (window.scaleIndex > 0) {
            window.scaleIndex--;
            changed = true;
        }
    }

    if (changed) {
        DOTNET_WINDOWS.invokeMethodAsync('OnScaleChanged', windowId, window.scaleIndex);
        window.chart.setScale(scaleList[window.scaleIndex]);
    }
}

// Event handler
function _evEnterToQuote(windowId) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window)
        window.quote.isMouseOver(true);
}

// Event handler
function _evLeaveFromQuote(windowId) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.isMouseOver(false);
        window.quote.isMouseOver(false);
        if (window.quote.isAutomicCentering()) { // mouse cursor's been left from the quote table
            // scroll to best bid index
            _evScrollBestBidIndex(windowId, window);
        }
    }
}

// Event handler
function _evEnterToCluster(windowId) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.isMouseOver(true);
    }
}

// Event handler
function _evLeaveFromCluster(windowId) {
    _evLeaveFromQuote(windowId);
}

// Event handler
function _evScrollBestBidIndex(windowId, window) {

    let _window;
    if (window === undefined) {
        _window = _iGetWindowInstanceByWindowId(windowId);
    } else {
        _window = window;
    }
    if (!_window) return;

    // force scroll QuoteTable
    _window.quote.scrollBestBidIndex();

    // force scroll ClusterTable
    var screenItemsLen = _window.quote.getScreenItemsLen();
    var halfScreenItemsLen = _window.quote.getScreenItemsHalfLen();
    var bestBidIndex = _window.quote.bestBidIndex();

    _window.cluster.scrollBestBidIndex(bestBidIndex, halfScreenItemsLen);

    // Notify Blazor to update the chart
    var first = bestBidIndex - halfScreenItemsLen;
    first = first < 0 ? 0 : first;
    last = first + screenItemsLen - 1;

    _iScrollChart(_window, first, last);
};

// Internal callback function
// Invoked in ClusterTable.OnScroll for synchronizing scroll of QuoteTable
function _iScrollCallbackCluster(windowId, scrollTop, first, screenItemsLen) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.quote.syncScroll(scrollTop, first);

        let _first = first + screenItemsLen;
        let _last = _first + screenItemsLen - 1; // last row index in visible area

        _iScrollChart(window, _first, _last);
    }
}

// Internal callback function
// Invoked in QuoteTable.OnScroll for synchronizing scroll of ClusterTable
function _iScrollCallbackQuote(windowId, scrollTop, first, screenItemsLen) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.syncScroll(scrollTop, first);

        let _first = first + screenItemsLen;
        let _last = _first + screenItemsLen - 1; // last row index in visible area

        _iScrollChart(window, _first, _last);
    }
}

// Internal callback function
// Invoked in QuoteTable.bodyResizing for synchronizing scroll of Chart
function _iPriceRangeChangeCallback(windowId, minPrice, maxPrice) {
    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.chart.setPriceBounds(minPrice, maxPrice);
    }
}

//function _iScrollCallbackChart(windowId, priceMin, priceMax, deltaY) {
//    const window = _iGetWindowInstanceByWindowId(windowId);
//    if (window) {
//        console.log(deltaY);
//        window.quote.syncScrollByPriceBounds(priceMin, priceMax, deltaY);
//    }
//}

// Internal callback function
function _iScrollChart(window, first, last) {
    var currMaxVisiblePrice = window.quote.getPrice(first);
    var prevMaxVisiblePrice = window.quote.maxVisiblePrice();

    //console.log('scrollChart 1');

    if (currMaxVisiblePrice != prevMaxVisiblePrice) {

        //console.log('scrollChart 2');
        var currMinVisiblePrice = window.quote.getPrice(last);

        window.quote.maxVisiblePrice(currMaxVisiblePrice);
        window.quote.minVisiblePrice(currMinVisiblePrice);

        // scroll chart
        window.chart.setPriceBounds(currMinVisiblePrice, currMaxVisiblePrice);
    }
}

// Internal function
function _iInitWindowsPane(windowId, initialData, totalRows) {

    let secId = initialData.secId;
    let automicCentering = initialData.automicCentering;
    let priceStep = initialData.priceStep;
    let orderVolumes = initialData.orderVolumes;
    let selectedOrderVolumeIndex = initialData.selectedOrderVolumeIndex;
    let scaleIndex = initialData.scaleIndex;

    // initialize cluster
    const clusterInstance = new VirtualClusterScrollingTable({
        scrollCallback: _iScrollCallbackCluster,
        secId: secId,
        windowId: windowId,
        containerWrapperClass: 'windows-panel',
        containerClass: 'cluster-container',
        itemHeight: 15,
        //items: [clusterData1, clusterData2, clusterData3],
        totalRows: totalRows,
        footerHeight: 15,
        tableMinSize: 30,
        columns: [
            { name: '', style: { textAlign: 'left' } },
            { name: '', style: { textAlign: 'left' } },
            { name: '', style: { textAlign: 'left' } }
        ]
    });

    //


    // initialize quote
    const quoteInstance = new VirtualQuoteScrollingTable({
        priceRangeChangeCallback: _iPriceRangeChangeCallback,
        scrollCallback: _iScrollCallbackQuote,
        cancelOrderCallback: _cancelOrderCallback,
        orderClickLeftCallback: _orderClickLeftCallback,
        orderClickRightCallback: _orderClickRightCallback,
        secId: secId,
        windowId: windowId,
        automicCentering: automicCentering,
        priceStep: priceStep,
        containerWrapperClass: 'windows-panel',
        containerClass: 'quote-container',
        itemHeight: 15,
        totalRows: totalRows
    });

    //


    // initialize chart
    const chartInstance = new VirtualScrollingChart({
        //scrollCallback: _iScrollCallbackChart,
        orderVolumeUpdateCallback: _orderVolumeUpdateCallback,
        orderVolumeSelectionChangedCallback: _orderVolumeSelectionChangedCallback,
        secId: secId,
        windowId: windowId,
        containerWrapperClass: 'windows-panel',
        containerClass: 'chart-container',
        orderVolumes: orderVolumes,
        selectedOrderVolumeIndex: selectedOrderVolumeIndex,
        scale: !scaleList ? 1 : scaleList[scaleIndex]
    });

    const windowsPane = new WindowsPane({
        secId: secId,
        windowId: windowId,
        openSettingCallback: _openSettingCallback,
        closeCallback: _closeCallback,
        openInstrumentPickerCallback: _openInstrumentPickerCallback,
        containerClasses: ['cluster-container', 'chart-container', 'quote-container'],
        containerWidths: containerWidths,
        containerMinWidths: containerMinWidths,
        clusterTable: clusterInstance,
        quoteTable: quoteInstance,
        chart: chartInstance
    });

    if (!windowInstances) windowInstances = [];
    windowInstances.push({
        windowId: windowId,
        secId: secId,
        cluster: clusterInstance,
        chart: chartInstance,
        quote: quoteInstance,
        pane: windowsPane,
        scaleIndex: scaleIndex
    });

    DOTNET_HOME.invokeMethodAsync('OnInitializedWindows', windowId);
}

// Internal function
function _iInitWindowsPaneAll(initialData, totalRows) {

    initialData.forEach((data) => {

        var windowId = data.windowId;

        _iInitWindowsPane(windowId, data, totalRows);
    });
}

// Internal function
function _iCloseWindows(windowId) {

    const window = _iGetWindowInstanceByWindowId(windowId);
    if (window) {
        window.cluster.destroy();
        window.chart.destroy();
        window.quote.destroy();
        window.pane.destroy();

        windowInstances = windowInstances.filter(i => i.windowId !== windowId);
    }
}

// Internal function
function _iShiftWindows(oldIndex, newIndex) {
    if (!windowInstances) return;

    var oldPane = windowInstances[oldIndex];
    
    if (oldIndex < newIndex) {

        for (var i = oldIndex; i < newIndex; i++) {
            windowInstances[i] = windowInstances[i + 1];
        }

    } else if (oldIndex > newIndex) {

        for (var i = oldIndex - 1; i >= newIndex; i--) {
            windowInstances[i + 1] = windowInstances[i];
        }
    } else {
        return;
    }

    windowInstances[newIndex] = oldPane;
}

// Internal function
// Invoked when new active orders arrived | load saved instruments | open new instrument | replace instruments
function _iUpdateByOrderChanged(newOrders) {
    if (!windowInstances || windowInstances.length == 0) return;

    let orders;
    let isActive;
    if (newOrders) {
        if (newOrders.length === 0) return;
        orders = newOrders;
        isActive = false;
    } else {
        if (!activeOrders || activeOrders.length === 0) {
            for (const windowInstance of windowInstances) {
                windowInstance.quote.setActiveOrders();
            }
            return;
        }
        orders = activeOrders;
        isActive = true;
    }

    if (!orders) return;

    // Group orders by security id
    const groupedOrders = new Map();
    for (const order of orders) {
        const key = order.secId;
        if (!groupedOrders.has(key)) {
            groupedOrders.set(key, new Map());
        }

        const orderMap = groupedOrders.get(key);
        const priceKey = order.price.toString();

        if (!orderMap.has(priceKey)) {
            orderMap.set(priceKey, []);
        }
        orderMap.get(priceKey).push(order);
    }

    // Update quotes table by new orders
    for (const windowInstance of windowInstances) {
        const ordersForSec = groupedOrders.get(windowInstance.secId);
        if (isActive)
            windowInstance.quote.setActiveOrders(ordersForSec);
        else
            windowInstance.quote.updateNewOrders(ordersForSec);
    }

    //
    // TODO - Another updates if necessary
    //
}

function _iUpdateByOpenedPositions() {
    if (!windowInstances || windowInstances.length == 0) return;

    if (!openedPositions || openedPositions.length == 0) {
        for (const window of windowInstances) {
            window.cluster.setFooterPNLVisibility(false);
            window.chart.setFooterPNLVisibility(false);
            window.quote.setFooterPNLVisibility(false);
        }
    } else {
        for (const window of windowInstances) {
            const pos = openedPositions.find(pos => pos.secId === window.secId);
            if (pos) {
                const PnL = _iCalculatePnL(pos.enterPrice, pos.priceStep, pos.priceStepCost, pos.exitPrice, pos.curPos);
                window.cluster.setFooterPNLVisibility(true, pos.enterPrice);
                window.chart.setFooterPNLVisibility(true, pos.curPos);
                window.quote.setFooterPNLVisibility(true, PnL.P, PnL.R, PnL.Percent);
            } else {
                window.cluster.setFooterPNLVisibility(false);
                window.chart.setFooterPNLVisibility(false);
                window.quote.setFooterPNLVisibility(false);
            }
        }
    }

    //window.cluster.setFooterPNLVisibility(true, 12345.789);
    //window.chart.setFooterPNLVisibility(true, -2);
    //window.quote.setFooterPNLVisibility(true, 1234567.123, 2345.67, 10.23);

    //
    // TODO - Another updates if necessary
    //
}

function _iGetWindowInstanceByWindowId(windowId) {
    if (!windowInstances) return null;
    return windowInstances.find(item => item.windowId === windowId);
}

function _iGetWindowInstancesBySecId(secId) {
    if (!windowInstances) return null;
    return windowInstances.filter(item => item.secId === secId);
}

// Internal helper function
function _iCalculatePnL(enterPrice /* pos.AverageEntry */,
                        priceStep /* pos.priceStep */,
                        priceStepCost /* pos.priceStepCost */,
                        bestPrice /* bestPrice | pos.AverageExit */,
                        volume /* pos.PosValue */) {
    if (volume === 0) {
        return { P: 0, R: 0, Percent: 0 };
    }
    let delta = bestPrice - enterPrice;
    if (delta === 0) {
        return { P: 0, R: 0, Percent: 0 };
    }

    let direction = volume > 0 ? 1 : -1;
    let pnlP = delta * direction;
    let pnlR = priceStep === 0 ? 0 : pnlP * Math.abs(volume) * priceStepCost / priceStep;
    let pnlPercent = enterPrice === 0 ? 0 : pnlP / enterPrice * 100;

    return { P: pnlP, R: pnlR, Percent: pnlPercent };
}

// Internal helper function
function _iFormatNumberWithCommas(number, maxFractionDigits = 3) {
    // Convert to number if it's a string
    const num = typeof number === 'string' ? parseFloat(number) : number;

    // Handle NaN cases (invalid numbers)
    if (isNaN(num)) return '';

    // Handle zero case
    if (num === 0) return '0';

    // Split into whole and decimal parts
    const parts = num.toString().split('.');
    let wholePart = parts[0];
    let decimalPart = parts.length > 1 ? parts[1] : '';

    // Add commas to whole number part
    wholePart = wholePart.replace(/\B(?=(\d{3})+(?!\d))/g, ',');

    // Handle decimal places
    if (decimalPart && maxFractionDigits > 0) {
        decimalPart = decimalPart.substring(0, maxFractionDigits);
        // Remove trailing zeros after decimal point
        decimalPart = decimalPart.replace(/0+$/, '');
        if (decimalPart) {
            return `${wholePart}.${decimalPart}`;
        }
    }

    return wholePart;
}

function _iFormatSmartDate(dateInput) {
    if (typeof dateInput !== 'number' || dateInput <= 0) {
        return '';
    }

    try {
        const date = new Date(dateInput);

        if (isNaN(date.getTime())) {
            return '';
        }

        const now = new Date();
        const isToday =
            date.getFullYear() === now.getFullYear() &&
            date.getMonth() === now.getMonth() &&
            date.getDate() === now.getDate();

        return isToday
            ? date.toLocaleTimeString() // Only time
            : date.toLocaleString();    // Full date and time
    } catch (e) {
        return '';
    }
}

window.getElementSizeById = (id) => {
    var el = document.getElementById(id);
    if (!el) return null;

    return {
        width: el.clientWidth,
        height: el.clientHeight
    };
};