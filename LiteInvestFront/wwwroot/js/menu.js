const RECENT_INSTRUMENTS_LIMIT = 40;

// Order.OrderStateType
const ORDER_STATE_ACTIVE = 1; // Active
const ORDER_STATE_PARTIAL = 4; // Partial
const ORDER_STATE_CANCELED = 6; // Cancel
const ORDER_STATE_FAILED = 5; // Fail
const ORDER_STATE_EXECUTED = 3; // Done

// Instrument Picker
let instruments = null;
let favInstruments = null;
let recentInstruments = null;

let instrumentPicker = null;
let instrumentPickerOpened = false;
let instrumentPickerProfile = null;
// active tab: 'all' | 'favorite' | 'recent'


// Active Orders Dialog
let activeOrders = null; // For better performance, always keeping active orders regardless of whether the selected tab or dialog's visibility
// But do not keep 'canceled' | 'executed' orders because these data gets growing huge

let activeOrdersDialog = null;
let activeOrdersDialogOpened = false;
let activeOrdersDialogProfile = null;
// active tab: 'active' | 'canceled' | 'executed'


// Active Positions Dialog
let openedPositions = null; // For better performance, always keeping active positions regardless of whether the selected tab or dialog's visibility
// But do not keep 'closed' positions because this data gets growing huge

let activePositionsDialog = null;
let activePositionsDialogOpened = false;
let activePositionsDialogProfile = null;
// active tab: 'opened' | 'closed'


// Finrez Dialog
let finrezDialog = null;
let finrezDialogOpened = false;


// Invoked from Menu after logged in
window.setInitialiData = (sessionToken, securityData, tabProfile, dialogsProfile) => {

    // initialize tab
    initializeTab(sessionToken);

    // initialize securityData
    const securities = securityData.securities;
    const favSecurities = securityData.favSecurities;
    const recentSecurities = securityData.recentSecurities;
    instruments = [];
    favInstruments = [];
    recentInstruments = [];

    if (securities?.length > 0) {
        const favSet = new Set(favSecurities || []);
        const recentSet = new Set((recentSecurities || []).map(item => item.id));

        securities.forEach((sec) => {
            const isFavorite = favSet.has(sec.id);
            const isRecent = recentSet.has(sec.id);
            const updatedAt = new Date(sec.updatedAt);
            var instrument = {
                id: sec.id,
                symbol: sec.symbol,
                longName: sec.fullName,
                type: sec.type,
                updatedAt: updatedAt.toLocaleDateString(),
                favorite: isFavorite
            };
            instruments.push(instrument);

            if (isFavorite) {
                // Create favorite instrument
                favInstruments.push({ ...instrument });
            }

            if (isRecent) {
                // Create recent instrument
                recentInstruments.push({ ...instrument });
            }
        });

        // Sort recent instruments by their original order in recentSecurities
        if (recentSecurities?.length > 0) {
            recentInstruments.sort((a, b) => {
                const aOrder = recentSecurities.find(item => item.id === a.id)?.OrderIndex || 0;
                const bOrder = recentSecurities.find(item => item.id === b.id)?.OrderIndex || 0;
                return aOrder - bOrder;
            });
        }
    }

    // initialize dialogs
    instrumentPickerProfile = dialogsProfile.instrumentPickerProfile;
    activeOrdersDialogProfile = dialogsProfile.activeOrdersDialogProfile;
    activePositionsDialogProfile = dialogsProfile.activePositionsDialogProfile;
};

// Invoked from WindowsRenderer component
window.loadWindows = (initialData, totalRows, _scaleList) => {

    let dotnet = DOTNET_WINDOWS;
    new MutationObserver((_, observer) => {

        let _board = document.getElementById("board");
        if (_board == null) return;

        let board = _board.parentElement; // must be parentElement to get the resize event on window's resizing
        let windows = document.getElementsByClassName("windows-root-panel");
        let clusterContainers = document.getElementsByClassName("cluster-container");
        let quoteContainers = document.getElementsByClassName("quote-container");
        let chartContainers = document.getElementsByClassName("chart-container");
        let nWindows = initialData.length;
        scaleList = _scaleList;

        if (board && windows && clusterContainers && quoteContainers && chartContainers &&
            windows.length == nWindows && clusterContainers.length == nWindows && quoteContainers.length == nWindows && chartContainers.length == nWindows) {

            observer.disconnect();

            let isMouseOverQuoteContainer = false;
            let isMouseOverClusterContainer = false;

            document.querySelectorAll('.windows-root-panel').forEach(div => {
                div.addEventListener("mouseenter", () => {
                    let instrumentHeader = div.querySelector(".instrument-header");
                    if (instrumentHeader) {
                        instrumentHeader.classList.add("focused");
                    }
                    div.focus();
                    dotnet.invokeMethodAsync('OnWindowFocused', div.id);
                });

                div.addEventListener("mouseleave", () => {
                    let instrumentHeader = div.querySelector(".instrument-header");
                    if (instrumentHeader) {
                        instrumentHeader.classList.remove("focused");
                    }
                });

                div.addEventListener('keydown', (e) => {
                    keyDownHandler(e, div.id, isMouseOverQuoteContainer);
                });
            });

            document.querySelectorAll('.quote-container').forEach(div => {
                let windowId = div.parentElement.parentElement.id;
                div.addEventListener("mouseenter", () => {
                    isMouseOverQuoteContainer = true;
                    _evEnterToQuote(windowId);
                });
                div.addEventListener("mouseleave", () => {
                    isMouseOverQuoteContainer = false;
                    _evLeaveFromQuote(windowId);
                });
            });

            document.querySelectorAll('.cluster-container').forEach(div => {
                let windowId = div.parentElement.parentElement.id;
                div.addEventListener("mouseenter", () => {
                    isMouseOverClusterContainer = true;
                    _evEnterToCluster(windowId);
                });
                div.addEventListener("mouseleave", () => {
                    isMouseOverClusterContainer = false;
                    _evLeaveFromCluster(windowId);
                });
            });

            windowsSplitter = new WindowsSplitter(board);
            setTimeout(() => {
                _iInitWindowsPaneAll(initialData, totalRows);
                console.log("loadWindows >>> _iUpdateByOrderChanged");
                _iUpdateByOrderChanged();
                _iUpdateByOpenedPositions();
                var el = document.getElementById('board');
                sortable = AlgoSortable.create(
                    el,
                    {
                        handle: '.instrument-header',
                        animation: 150,
                        //forceFallback: true,
                        //fallbackOnBody: true,
                        //fallbackClass: 'drag-clone',
                        onStart: function (evt) {

                            windowsSplitter.updateGuttersOnWindowsDragStart(evt.newIndex);

                            const draggedItem = evt.item;

                            //const clone = draggedItem.cloneNode(true);
                            //const originalHeight = getComputedStyle(draggedItem).height;
                            //clone.style.height = originalHeight;
                            //draggedItem.setAttribute('data-orig-height', originalHeight);

                            const panel = draggedItem.querySelector('.windows-panel');
                            if (panel) {
                                const overlay = document.createElement('div');
                                overlay.className = 'drag-overlay';
                                //overlay.innerHTML = '<span>Moving...</span>';
                                overlay.innerHTML = `
                                    <div class="drag-overlay-content">
                                        <i class="bi bi-arrows-move" style="margin-right: 8px;"></i> Moving...
                                    </div>
                                `;
                                panel.appendChild(overlay);
                            }

                        },
                        onEnd: function (evt) {

                            document.querySelectorAll('.drag-overlay').forEach(o => o.remove());

                            if (evt.oldIndex != evt.newIndex) {
                                _iShiftWindows(evt.oldIndex, evt.newIndex);

                                if (windowInstances) {
                                    windowInstances.forEach((_, i) => {
                                        _.pane.refresh();
                                    });
                                }

                                dotnet.invokeMethodAsync('OnWindowsOrderChanged', evt.oldIndex, evt.newIndex);
                            }
                            windowsSplitter.updateGuttersOnWindowsDropEnd(evt.oldIndex, evt.newIndex);

                            const innerDiv = evt.item.querySelector('.windows-root-panel');
                            const windowId = innerDiv ? innerDiv.id : null;

                            const window = _iGetWindowInstanceByWindowId(windowId);
                            if (window) {
                                window.cluster.refreshScroll();
                                window.quote.refreshScroll();
                            }
                        },
                        //onUpdate: function (evt) {
                        //    console.log("onUpdate - 3");
                        //},
                        //onSort: function (evt) {
                        //    console.log("onSort - 4");
                        //},
                        //onMove: function (evt) {
                        //    console.log("onMove - 1");
                        //},
                        //onChange: function (evt) {
                        //    console.log("onChange - 2");
                        //}
                    });
            }, 500);
        }
    }).observe(document.body, { childList: true, subtree: true });
};

// Invoked from WindowsRenderer component
window.openWindow = (initialData, totalRows, _scaleList) => {

    let dotnet = DOTNET_WINDOWS;
    let windowId = initialData.windowId;
    scaleList = _scaleList;

    new MutationObserver((_, observer) => {

        let _board = document.getElementById("board");
        if (_board == null) return;

        let board = _board.parentElement; // must be parentElement to get the resize event on window's resizing
        let windows = document.getElementById(windowId);
        var escapedWindowId = CSS.escape(windowId);
        let clusterContainer = document.querySelector(`#${escapedWindowId} .cluster-container`);
        let quoteContainer = document.querySelector(`#${escapedWindowId} .quote-container`);

        if (board && windows && clusterContainer && quoteContainer) {

            /* Event Listeners */
            windows.addEventListener("mouseenter", () => {
                let instrumentHeader = windows.querySelector(".instrument-header");
                if (instrumentHeader) {
                    instrumentHeader.classList.add("focused");
                }
                windows.focus();
                dotnet.invokeMethodAsync('OnWindowFocused', windowId);
            });

            windows.addEventListener("mouseleave", () => {
                let instrumentHeader = windows.querySelector(".instrument-header");
                if (instrumentHeader) {
                    instrumentHeader.classList.remove("focused");
                }
            });

            let isMouseOverQuoteContainer = false;
            quoteContainer.addEventListener("mouseenter", () => {
                isMouseOverQuoteContainer = true;
                _evEnterToQuote(windowId);
            });
            quoteContainer.addEventListener("mouseleave", () => {
                isMouseOverQuoteContainer = false;
                _evLeaveFromQuote(windowId);
            });

            windows.addEventListener('keydown', (e) => {
                //console.log(e.code, windowId);
                keyDownHandler(e, windowId, isMouseOverQuoteContainer);
            });
            /* Event Listeners */

            if (windowsSplitter == null) {
                windowsSplitter = new WindowsSplitter(board,
                    (startDragging, windowId1, windowId2) => {
                        if (startDragging == false) {
                            //console.log('OnWindowsSizeChangeEvent');
                        }
                    });
            } else {
                windowsSplitter.initializeWindows();
            }

            observer.disconnect();

            setTimeout(() => {
                _iInitWindowsPane(windowId, initialData, totalRows);
                console.log("openWindow >>> _iUpdateByOrderChanged");
                _iUpdateByOrderChanged();
                _iUpdateByOpenedPositions();
                var el = document.getElementById('board');
                sortable = AlgoSortable.create(
                    el,
                    {
                        handle: '.instrument-header',
                        animation: 150,
                        onStart: function (evt) {

                            windowsSplitter.updateGuttersOnWindowsDragStart(evt.newIndex);

                        },
                        onEnd: function (evt) {

                            if (evt.oldIndex != evt.newIndex) {
                                _iShiftWindows(evt.oldIndex, evt.newIndex);

                                if (windowInstances) {
                                    windowInstances.forEach((_, i) => {
                                        _.pane.refresh();
                                    });
                                }

                            }
                            windowsSplitter.updateGuttersOnWindowsDropEnd(evt.oldIndex, evt.newIndex);

                            const innerDiv = evt.item.querySelector('.windows-root-panel');
                            const windowId = innerDiv ? innerDiv.id : null;

                            const window = _iGetWindowInstanceByWindowId(windowId);
                            if (window) {
                                window.cluster.refreshScroll();
                                window.quote.refreshScroll();
                            }
                        }
                    });
            }, 500);

        }
    }).observe(document.body, { childList: true, subtree: true });
}

// Invoked from WindowsRenderer component
window.closedWindow = (closedSecId) => {

    console.log("closedWindow 1");

    if (!recentInstruments.some(recItem => recItem.id === closedSecId)) {
        // add as a new recent instrument to top
        const item = instruments.find(item => item.id === closedSecId);
        if (item) {
            const recItem = { ...item };
            recentInstruments.unshift(recItem);  // Add to beginning

            if (recentInstruments.length > RECENT_INSTRUMENTS_LIMIT) {  // Adjust limit as needed
                recentInstruments.pop();  // Remove oldest
            }
        }
    } else {
        // move existing item to top
        const index = recentInstruments.findIndex(recItem => recItem.id === closedSecId);
        if (index >= 0) {
            const [existingItem] = recentInstruments.splice(index, 1);  // Remove from current position
            recentInstruments.unshift(existingItem);  // Add to beginning
        }
    }

    // TODO
    if (instrumentPicker != null) {
        instrumentPicker.renderTable();
    }

    let didRun = false;

    //requestAnimationFrame(() => {
    let observer = new MutationObserver(() => {
        if (didRun) return;
        didRun = true;

        console.log("closedWindow 2");
        let boardParent = document.getElementById("board")?.parentElement;
        if (boardParent && windowsSplitter) {
            windowsSplitter.initializeWindows();
        }

        if (boardParent && observer)
            observer.disconnect();

    }).observe(document.body, { childList: true, subtree: true });
    //});

    setTimeout(() => {
        if (!didRun) {
            console.log("closedWindow 2 [fallback]");
            let boardParent = document.getElementById("board")?.parentElement;
            if (boardParent && windowsSplitter) {
                windowsSplitter.initializeWindows();
            }
            if (boardParent && observer)
                observer.disconnect();
        }
    }, 500);
}

// Invoked from WindowsRenderer component
window.replaceWindow = (initialData, totalRows) => {

    let dotnet = DOTNET_WINDOWS;
    let windowId = initialData.windowId;

    new MutationObserver((_, observer) => {

        let board = document.getElementById("board").parentElement; // must be parentElement to get the resize event on window's resizing
        let windows = document.getElementById(windowId);

        var escapedWindowId = CSS.escape(windowId);
        let clusterContainer = document.querySelector(`#${escapedWindowId} .cluster-container`);
        let quoteContainer = document.querySelector(`#${escapedWindowId} .quote-container`);

        if (board && windows && clusterContainer && quoteContainer) {

            /* Event Listeners */
            windows.addEventListener("mouseenter", () => {
                let instrumentHeader = windows.querySelector(".instrument-header");
                if (instrumentHeader) {
                    instrumentHeader.classList.add("focused");
                }
                windows.focus();
                dotnet.invokeMethodAsync('OnWindowFocused', windowId);
            });

            windows.addEventListener("mouseleave", () => {
                let instrumentHeader = windows.querySelector(".instrument-header");
                if (instrumentHeader) {
                    instrumentHeader.classList.remove("focused");
                }
            });

            let isMouseOverQuoteContainer = false;
            quoteContainer.addEventListener("mouseenter", () => {
                isMouseOverQuoteContainer = true;
                _evEnterToQuote(windowId);
            });
            quoteContainer.addEventListener("mouseleave", () => {
                isMouseOverQuoteContainer = false;
                _evLeaveFromQuote(windowId);
            });

            windows.addEventListener('keydown', (e) => {
                //console.log(e.code, windowId);
                keyDownHandler(e, windowId, isMouseOverQuoteContainer);
            });
            /* Event Listeners */

            if (windowsSplitter == null) {
                windowsSplitter = new WindowsSplitter(board,
                    (startDragging, windowId1, windowId2) => {
                        if (startDragging == false) {
                            //console.log('OnWindowsSizeChangeEvent');
                        }
                    });
            } else {
                windowsSplitter.initializeWindows();
            }

            observer.disconnect();

            setTimeout(() => {
                _iInitWindowsPane(windowId, initialData, totalRows);
                console.log("replaceWindow >>> _iUpdateByOrderChanged");
                _iUpdateByOrderChanged();
                _iUpdateByOpenedPositions();
                var el = document.getElementById('board');
                sortable = AlgoSortable.create(
                    el,
                    {
                        handle: '.instrument-header',
                        animation: 150,
                        onStart: function (evt) {

                            windowsSplitter.updateGuttersOnWindowsDragStart(evt.newIndex);

                        },
                        onEnd: function (evt) {

                            if (evt.oldIndex != evt.newIndex) {
                                _iShiftWindows(evt.oldIndex, evt.newIndex);

                                if (windowInstances) {
                                    windowInstances.forEach((_, i) => {
                                        _.pane.refresh();
                                    });
                                }

                            }
                            windowsSplitter.updateGuttersOnWindowsDropEnd(evt.oldIndex, evt.newIndex);

                            const innerDiv = evt.item.querySelector('.windows-root-panel');
                            const windowId = innerDiv ? innerDiv.id : null;

                            const window = _iGetWindowInstanceByWindowId(windowId);
                            if (window) {
                                window.cluster.refreshScroll();
                                window.quote.refreshScroll();
                            }
                        }
                    });
            }, 500);
        }
    }).observe(document.body, { childList: true, subtree: true });
};

// Invoked from Menu component
window.openActiveOrdersDialog = () => {
    _openActiveOrdersDialog();
};

// Invoked from Menu component
window.openActivePositionsDialog = () => {
    _openActivePositionsDialog();
};

// Invoked from Menu component
window.openFinrezDialog = () => {
    _openFinrezDialog();
};

// Invoked from windows pane
function _openInstrumentPickerCallback(secId, windowId) {
    if (instrumentPicker != null) {
        instrumentPicker.close();
        instrumentPicker = null;
        return;
    }
    instrumentPicker = new InstrumentPicker({ all: instruments, fav: favInstruments, recent: recentInstruments }, {
        secId: secId,
        windowId: windowId,
        profile: instrumentPickerProfile,
        selectInstrumentCallback: _selectedInstrumentCallback,
        favChangedCallback: _favInstrumentChangedCallback,
        closingCallback: _closingInstrumentPickerCallback,
        closedCallback: _closedInstrumentPickerCallback
    });
}

// Invoked by app bar action
function _openNewInstrumentPicker() {
    if (instrumentPicker != null) {
        instrumentPicker.close();
        instrumentPicker = null;
        return;
    }
    instrumentPicker = new InstrumentPicker({ all: instruments, fav: favInstruments, recent: recentInstruments }, {
        profile: instrumentPickerProfile,
        selectInstrumentCallback: _selectedInstrumentCallback,
        favChangedCallback: _favInstrumentChangedCallback,
        closingCallback: _closingInstrumentPickerCallback,
        closedCallback: _closedInstrumentPickerCallback
    });
}

// Invoked by app bar action
function _openActiveOrdersDialog() {
    if (activeOrdersDialog != null) {
        activeOrdersDialog.close();
        activeOrdersDialog = null;
        return;
    }

    let data = null;
    const activeTab = activeOrdersDialogProfile ? activeOrdersDialogProfile.activeTab : 'active';
    if (activeTab === 'active') {
        data = activeOrders;
    } else if (activeTab === 'canceled') {
        DOTNET_MENU.invokeMethodAsync('RequestCanceledOrders');
    } else if (activeTab === 'executed') {
        DOTNET_MENU.invokeMethodAsync('RequestExecutedOrders');
    }

    activeOrdersDialog = new ActiveOrdersDialog(data, {
        profile: activeOrdersDialogProfile,
        cancelOrderCallback: _cancelOrderCallback, // main.js
        changeTabCallback: _changeTabActiveOrderDialogCallback,
        closingCallback: _closingActiveOrdersDialogCallback,
        closedCallback: _closedActiveOrdersDialogCallback
    });
}

// Invoked by app bar action
function _openActivePositionsDialog() {
    if (activePositionsDialog != null) {
        activePositionsDialog.close();
        activePositionsDialog = null;
        return;
    }

    let data = null;
    const activeTab = activePositionsDialogProfile ? activePositionsDialogProfile.activeTab : 'opened';
    if (activeTab === 'opened') {
        data = openedPositions;
    } else if (activeTab === 'closed') {
        DOTNET_MENU.invokeMethodAsync('RequestClosedPositions');
    }

    activePositionsDialog = new ActivePositionsDialog(data, {
        profile: activePositionsDialogProfile,
        closePositionCallback: _closePositionCallback, // main.js
        changeTabCallback: _changeTabActivePositionsDialogCallback,
        closingCallback: _closingActivePositionsDialogCallback,
        closedCallback: _closedActivePositionsDialogCallback
    });
}

// Invoked by app bar action
function _openFinrezDialog() {

}

// Event handler
function _closingInstrumentPickerCallback() {
    if (!instrumentPicker) return;
    instrumentPickerProfile = instrumentPicker.getProfile();
    DOTNET_MENU.invokeMethodAsync('SaveDialogProfile', 'instrumentPicker', instrumentPickerProfile);
}
function _closedInstrumentPickerCallback() {
    instrumentPicker = null;
}

// Event handler
function _closingActiveOrdersDialogCallback() {
    if (!activeOrdersDialog) return;
    activeOrdersDialogProfile = activeOrdersDialog.getProfile();
    DOTNET_MENU.invokeMethodAsync('SaveDialogProfile', 'activeOrders', activeOrdersDialogProfile);
}
function _closedActiveOrdersDialogCallback() {
    activeOrdersDialog = null;
}

// Event handler
function _closingActivePositionsDialogCallback() {
    if (!activePositionsDialog) return;
    activePositionsDialogProfile = activePositionsDialog.getProfile();
    DOTNET_MENU.invokeMethodAsync('SaveDialogProfile', 'activePositions', activePositionsDialogProfile);
}
function _closedActivePositionsDialogCallback() {
    activePositionsDialog = null;
}

// Event handler
function _closingFinrezDialog() {
    if (!finrezDialog) return;
    // TODO
}
function _closedFinrezDialog() {
    finrezDialog = null;
}

// Event handler
function _selectedInstrumentCallback(category, oldSecId, oldWindowId, newInstrument) {
    instrumentPicker = null;
    if (newInstrument == null || oldSecId == newInstrument.id)
        return;
    if (oldSecId == null && oldWindowId == null) {
        // open new instrument
        console.log('Selected:', newInstrument);
        DOTNET_WINDOWS.invokeMethodAsync('OnOpenWindow', category, newInstrument.id);
    } else if (oldSecId != null && oldWindowId != null) {
        // replace instrument
        console.log('Selected:', oldSecId, oldWindowId, newInstrument);
        _iCloseWindows(oldWindowId);
        DOTNET_WINDOWS.invokeMethodAsync('OnReplaceWindow', category, oldSecId, oldWindowId, newInstrument.id);
    }
}

// Event handler
function _favInstrumentChangedCallback(secId, favOn) {
    console.log(secId);
    DOTNET_WINDOWS.invokeMethodAsync('OnFavoriteInstrumentChanged', secId, favOn);
}

// Event handler
function _changeTabActiveOrderDialogCallback(tab) {

    if (activeOrdersDialogProfile) activeOrdersDialogProfile.activeTab = tab;

    if (tab === 'active') {
        if (activeOrdersDialog)
            activeOrdersDialog.setData(activeOrders);
    } else if (tab === 'canceled') {
        DOTNET_MENU.invokeMethodAsync('RequestCanceledOrders');
    } else if (tab === 'executed') {
        DOTNET_MENU.invokeMethodAsync('RequestExecutedOrders');
    }
}

function _changeTabActivePositionsDialogCallback(tab) {

    if (activePositionsDialogProfile) activePositionsDialogProfile.activeTab = tab;

    if (tab === 'opened') {
        if (activePositionsDialog)
            activePositionsDialog.setData(openedPositions);
    } else if (tab === 'closed') {
        DOTNET_MENU.invokeMethodAsync('RequestClosedPositions');
    }
}