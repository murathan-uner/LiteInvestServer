let workspacePicker = null;
const childTabs = new Map();

let lastWindowState;

// Interval to detect window's resizing and moving to store the state
let resizeMoveInterval;

// Master tab interval for heartbeat system
let masterHeartbeat;

// Child tab interval to self-close if master is dead
let checkMasterInterval;

window.cleanupBlazorTabInterop = () => {
    console.log("cleanup");
    _garbageCollect();
    delete window.blazorTabInterop;
};

// Invoked from master's Menu after logged in
//window.openWorkspaces = async (tabIds) => {
//    if (tabIds.length === 0) return;

//    for (const tabId of tabIds) {
//        _addWorkspace(tabId);
//    }
//}
window.openWorkspaces = async (tabProfiles) => {

    if (tabProfiles.length == 1) {
        // Open single tab immediately
        var tab = await _mOpenWorkspace(tabProfiles[0]);
        if (tab == null) { // maybe popup-blocked by refreshing browser
            _mShowWorkspacePicker(tabProfiles);
        }
    } else if (tabProfiles.length > 1) {

        if (workspacePicker != null) {
            workspacePicker.close();
            return;
        }
        workspacePicker = new WorkspacePicker({ tabProfiles: tabProfiles }, {
            closeCallback: _mCloseButtonCallback,
            selectWorkspaceCallback: async (tabProfile) => {
                await _mOpenWorkspace(tabProfile);
            }
        });
    }
};

window.logout = () => {
    if (childTabs) {
        childTabs.forEach((tab, id) => !tab.window.closed && _iCloseChildTab(id, tab.window));
        childTabs.clear();
    }
    _mCheckForOrphanedTabs();
    localStorage.removeItem('masterTabAlive');
};

window.tabCaptionUpdated = (caption) => {
    sessionStorage.tabCaption = caption;
};

const initializeTab = (sessionToken) => {
    window.blazorTabInterop = {
        SaveTabProfile: async (tabProfile) => {
            return DOTNET_MENU.invokeMethodAsync('SaveTabProfile', tabProfile);
        },
        RemoveTabProfile: async () => {
            return DOTNET_MENU.invokeMethodAsync('RemoveTabProfile');
        },
        RemoveTabProfiles: async (tabIds) => {
            return DOTNET_MENU.invokeMethodAsync('RemoveTabProfiles', tabIds);
        }
    };
    sessionStorage.token = sessionToken;

    lastWindowState = {
        x: window.screenX,
        y: window.screenY,
        width: window.outerWidth,
        height: window.outerHeight
    };

    // Start window's resize/move detection interval
    _startResizeMoveInterval();

    if (_isMasterTab()) {

        // Start heartbeat system
        _mStartHeartbeat();

        // Run check on startup
        _mCheckForOrphanedTabs();

    } else {

        // self-close if master is dead
        _cStartCheckMaster();
    }
};

// master's function
const _mStartHeartbeat = () => {

    if (masterHeartbeat) {
        clearInterval(masterHeartbeat);
        delete masterHeartbeat;
    }

    // Heartbeat system
    localStorage.setItem('masterTabAlive', Date.now());
    masterHeartbeat = setInterval(() => {
        localStorage.setItem('masterTabAlive', Date.now());
    }, 800); // Faster than 1s for safety
};

const _mShowWorkspacePicker = (tabProfiles) => {
    if (workspacePicker != null) {
        workspacePicker.close();
        return;
    }
    workspacePicker = new WorkspacePicker({ tabProfiles: tabProfiles }, {
        closeCallback: _mCloseButtonCallback,
        selectWorkspaceCallback: async (tabProfile) => {
            await _mOpenWorkspace(tabProfile);
        }
    });
};

// master's function
const _mOpenWorkspace = async (tabProfile) => {

    /////////////////////////////////////////////////////
    // remove childTabs that don't exist in local storage
    const storedTabs = localStorage.getItem('childTabs');
    if (storedTabs) {
        const tabs = JSON.parse(storedTabs);
        const storedIds = new Set(Object.keys(tabs));

        for (const [id, tab] of childTabs.entries()) {
            if (!storedIds.has(id)) {
                childTabs.delete(id);
            }
        }
    } else {
        childTabs.clear();
    }
    ////////////////////////////////////////////////////

    //let width = window.screen.availWidth;
    //let height = window.screen.availHeight;
    let width = window.outerWidth;
    let height = window.outerHeight;

    let left = 0;
    let top = 0;
    let id = "";
    let caption = "";

    if (tabProfile === undefined || (tabProfile.left === 0 && tabProfile.top === 0 && tabProfile.width === 0 && tabProfile.height === 0)) { // new tab

        if (tabProfile === undefined) {
            id = crypto.randomUUID();
            caption = __('workspace');
        }
        else {
            id = tabProfile.id;
            caption = tabProfile.caption;
        }

        let displayIndex = childTabs.size + 1;

        var bound = await _iGetWindowBoundByDisplayIndex(displayIndex);
        left = bound.left;
        top = bound.top;
        width = bound.width;
        height = bound.height;

    } else { // existing tab

        id = tabProfile.id;
        caption = tabProfile.caption;

        if (tabProfile.width > 0)
            width = tabProfile.width;
        if (tabProfile.height > 0)
            height = tabProfile.height;
        left = tabProfile.left;
        top = tabProfile.top;
    }

    //let features = `
    //    menubar=no,
    //    toolbar=no,
    //    location=no,
    //    status=no,
    //    resizable=yes,
    //    scrollbars=yes,
    //    top=${top},
    //    left=${left},
    //    screenX=${left},
    //    screenY=${top}`;
    let features = `
        menubar=no,
        toolbar=no,
        location=no,
        status=no,
        resizable=yes,
        scrollbars=yes`;

    if (tabProfile === undefined || tabProfile.width === 0 || tabProfile.height === 0) {
        features += `,fullscreen=yes`;
    } else {
        features += `,width=${width},height=${height}`;
    }
    console.log(features, left, top);

    // TODO - encrypt the URL
    //let url = `${window.location.origin}?index=${childTabs.size + 1}&tabId=${id}&caption=${caption}&left=${left}&top=${top}&width=${width}&height=${height}`;
    const queryParams = {
        index: childTabs.size + 1,
        token: sessionStorage.token,
        tabId: id,
        caption: caption, // English(Ascill text)/Russian(Unicode text)
        left: left,
        top: top,
        width: width,
        height: height
    };

    const json = JSON.stringify(queryParams);
    // Safely encode Unicode → Base64
    const base64 = btoa(
        encodeURIComponent(json).replace(/%([0-9A-F]{2})/g, (_, hex) =>
            String.fromCharCode(parseInt(hex, 16))));
    const url = `${window.location.origin}?q=${encodeURIComponent(base64)}`;
    const tab = window.open(url, '_blank', features);

    if (tab) {
        try {
            tab.focus();
            setTimeout(() => {
                try {
                    tab.resizeTo(width, height);
                    tab.moveTo(left, top);
                } catch (e) {
                    console.warn('Could not reposition window:', e);
                }
            }, 100);
        } catch (e) {
            console.warn('Could not focus window:', e);
        }

        childTabs.set(id, { window: tab, caption: caption });

        // save tabs to local storage to ensure opened tabs tracking
        const tabsToStore = {};
        childTabs.forEach((tab, id) => {
            if (!tab.window.closed) {
                tabsToStore[id] = tab.window.location.href;
            }
        });
        localStorage.setItem('childTabs', JSON.stringify(tabsToStore));
    }

    return tab;
};

// master's function
const _mCheckForOrphanedTabs = () => {
    const storedTabs = localStorage.getItem('childTabs');
    if (storedTabs) {
        const tabs = JSON.parse(storedTabs);
        Object.entries(tabs).forEach(([id, url]) => {
            // Try to close any tabs that might still be open
            const tab = window.open('', '_blank');
            if (tab) {
                try {
                    _iCloseChildTab(id, tab);
                    console.log(`Force-closed orphaned tab ${id}`);
                } catch (e) {
                    console.warn(`Couldn't close orphaned tab ${id}:`, e);
                }
            }
        });
        localStorage.removeItem('childTabs');
    }
};

// master's function
const _mCloseButtonCallback = async () => {
    if (!workspacePicker || !window.blazorTabInterop)
        return;
    var remainedTabIds = workspacePicker.getRemainedTabs();
    if (remainedTabIds.length == 0)
        return;

    await window.blazorTabInterop.RemoveTabProfiles(remainedTabIds);
    // or we can use sendBeacon
};

const _cStartCheckMaster = () => {

    if (checkMasterInterval) {
        clearInterval(checkMasterInterval);
        delete checkMasterInterval;
    }

    checkMasterInterval = setInterval(() => {
        const lastHeartbeat = localStorage.getItem('masterTabAlive');
        if (!lastHeartbeat || Date.now() - lastHeartbeat > 3000) {
            window.close();
        }
    }, 1000);
};

const _startResizeMoveInterval = () => {
    if (resizeMoveInterval) {
        clearInterval(resizeMoveInterval);
        delete resizeMoveInterval;
    }

    resizeMoveInterval = setInterval(async () => {
        const current = {
            x: window.screenX,
            y: window.screenY,
            width: window.outerWidth,
            height: window.outerHeight
        };

        if (!lastWindowState ||
            lastWindowState.x != current.x ||
            lastWindowState.y != current.y ||
            lastWindowState.width != current.width ||
            lastWindowState.height != current.height) {

            lastWindowState = current;
            console.log("size/position changed", current);

            if (window.blazorTabInterop) {
                const tabProfile = _iGetTabProfile();
                await window.blazorTabInterop.SaveTabProfile(tabProfile);
            }
        }
    }, 1000);
}

const _garbageCollect = () => {
    if (resizeMoveInterval) {
        clearInterval(resizeMoveInterval);
        delete resizeMoveInterval;
    }
    if (masterHeartbeat) {
        clearInterval(masterHeartbeat);
        delete masterHeartbeat;
    }
    if (checkMasterInterval) {
        clearInterval(checkMasterInterval);
        delete checkMasterInterval;
    }
};

// Internal function
const _iGetTabProfile = () => {
    return {
        id: sessionStorage.tabId,
        caption: sessionStorage.tabCaption,
        left: window.screenX,
        top: window.screenY,
        width: window.outerWidth,
        height: window.outerHeight
    };
};

const _iGetWindowBoundByDisplayIndex = async (displayIndex) => {
    let left = 0, top = 0, width = window.outerWidth, height = window.outerHeight;
    if (window.screen.isExtended) {
        // For multi-display setups
        if (typeof window.getScreenDetails === 'function') {
            // Modern browsers with multi-screen support
            try {
                const screenDetails = await window.getScreenDetails();
                if (displayIndex < screenDetails.screens.length) {
                    const screen = screenDetails.screens[displayIndex];
                    left = screen.availLeft;
                    width = screen.availWidth;
                    height = screen.availHeight;
                }
            } catch (e) {
                console.warn('Failed to get screen details:', e);
                // Fallback to simple calculation
                left = displayIndex * window.screen.availWidth;
            }
        } else {
            // Fallback for browsers without getScreenDetails
            left = displayIndex * window.screen.availWidth;
        }
    }
    return { left: left, top: top, width: width, height: height };
};

const _isMasterTab = () => {
    return sessionStorage.tabId === undefined || sessionStorage.tabId === 'master';
};

const _iCloseChildTab = (id, tab) => {
    localStorage.setItem(`closingByMaster-${id}`, "true");
    tab.close();
};

// Close children when master tab closes
const _iHandleTabClose = () => {

    const tabId = sessionStorage.tabId;
    if (tabId === undefined) { // already closed
        console.log('already closed');
        return;
    }

    delete sessionStorage.tabId;

    // do not use _isMasterTab since sessionStorage.tabId is deleted just before
    if (tabId === 'master') { // master tab

        ////////////////////
        // Beacon

        const tabProfile = _iGetTabProfile();
        const childTabProfiles = [];

        // child tabs profile
        for (const [id, tab] of childTabs) {
            if (!tab.window.closed) {
                const childProfile = {
                    id: id,
                    caption: tab.caption,
                    left: tab.window.screenX,
                    top: tab.window.screenY,
                    width: tab.window.outerWidth,
                    height: tab.window.outerHeight
                };
                childTabProfiles.push(childProfile);
            }
        }

        const payload = {
            token: sessionStorage.token,
            tabProfile: tabProfile,
            childTabProfiles: childTabProfiles
        };
        const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
        navigator.sendBeacon("/api/blazor-tab/close-master", blob);

        ////////////////////

        if (childTabs) {
            childTabs.forEach((tab, id) => !tab.window.closed && _iCloseChildTab(id, tab.window));
            childTabs.clear();
        }
        localStorage.removeItem('masterTabAlive');

        // clear tabs from local storage
        const storedTabs = localStorage.getItem('childTabs');
        if (storedTabs)
            localStorage.removeItem('childTabs');

    } else { // child tab

        const closedByMaster = localStorage.getItem(`closingByMaster-${tabId}`) === "true";

        // Clean up the flag regardless
        localStorage.removeItem(`closingByMaster-${tabId}`);

        if (!closedByMaster) {
            // Only send beacon or invoke Blazor when closed manually
            const payload = {
                token: sessionStorage.token,
                tabId: tabId
            };
            const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
            navigator.sendBeacon("/api/blazor-tab/close-child", blob);

        }

        // remove tab from local storage
        const storedTabs = localStorage.getItem('childTabs');
        if (storedTabs) {
            const tabs = JSON.parse(storedTabs);

            if (tabs.hasOwnProperty(tabId)) {

                // Remove the tab entry
                delete tabs[tabId];

                // If no tabs remain, remove the entire key
                if (Object.keys(tabs).length === 0) {
                    localStorage.removeItem('childTabs');
                } else {
                    localStorage.setItem('childTabs', JSON.stringify(tabs));
                }
            }
        }
    }
};



(() => {

    const getParamsFromBase64Query = () => {
        const params = new URLSearchParams(window.location.search);
        const encoded = params.get('q');

        if (encoded) {
            try {
                // 1. Decode Base64 → Binary String
                const binaryStr = atob(encoded);

                // 2. Convert Binary String → Percent-Encoded String
                const percentEncoded = Array.from(binaryStr).map(c =>
                    '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
                ).join('');

                // 3. Decode Percent-Encoded String → Original JSON
                const jsonStr = decodeURIComponent(percentEncoded);

                // 4. Parse JSON
                const parsed = JSON.parse(jsonStr);
                return parsed; // Returns { tabId, caption, index, ... }
            } catch (e) {
                console.error("Failed to decode or parse query:", e);
            }
        }
        return null;
    }

    const queryParams = getParamsFromBase64Query();
    if (queryParams) {
        const tabId = queryParams.tabId || 'master';
        const caption = queryParams.caption;

        sessionStorage.tabId = tabId;
        sessionStorage.tabCaption = caption;
    } else {
        sessionStorage.tabId = 'master';
    }

    //const params = new URLSearchParams(window.location.search);
    //const tabId = params.get('tabId') || 'master';
    //const tabCaption = params.get('caption');
    //sessionStorage.tabId = tabId;
    //sessionStorage.tabCaption = tabCaption;

    //window.addEventListener('pagehide', () => {
    //    console.log('pagehide');
    //    _iHandleTabClose();
    //});

    //let isRefreshing = false;
    //// Alternative modern approach using PerformanceNavigationTiming
    //if (window.performance && window.performance.getEntriesByType) {
    //    const navigationEntries = performance.getEntriesByType('navigation');
    //    if (navigationEntries.length > 0 && navigationEntries[0].type === 'reload') {
    //        isRefreshing = true;
    //    }
    //}
    //window.addEventListener('beforeunload', (e) => {
    //    console.log('beforeunload');
    //    // Check if this is potentially a refresh (not 100% reliable)
    //    if (e.currentTarget.performance && e.currentTarget.performance.navigation) {
    //        // Deprecated but works in older browsers
    //        if (e.currentTarget.performance.navigation.type === 1) {
    //            isRefreshing = true;
    //        }
    //    }
    //});
    //window.addEventListener('unload', () => {
    //    if (!isRefreshing) {
    //        console.log('Window is really closing');
    //        _iHandleTabClose();
    //    } else {
    //        console.log('This is just a refresh');
    //    }
    //});

    if (_isMasterTab()) { // master tab

        // Start heartbeat system
        _mStartHeartbeat();

        // Run check on startup
        _mCheckForOrphanedTabs();

    } else { // child tab

        // Self-close if master is dead
        _cStartCheckMaster();
    }

    new MutationObserver((_, observer) => {

        const btnSelectInstrument = document.getElementById("btn-select-instrument");
        const btnAddWorkspace = document.getElementById("btn-add-workspace");
        const btnCloseWorkspace = document.getElementById("btn-close-workspace");

        if (btnSelectInstrument) {
            btnSelectInstrument.onclick = async (e) => {
                e.preventDefault();
                _openNewInstrumentPicker();
            };
        }

        if (_isMasterTab() && btnAddWorkspace) {
            btnAddWorkspace.onclick = async (e) => {
                e.preventDefault();
                await _mOpenWorkspace();
            };
        }

        if (!_isMasterTab() && btnCloseWorkspace) {
            btnCloseWorkspace.onclick = (e) => {
                e.preventDefault();
                _iHandleTabClose();
                window.close();
            };
        }

        if ((_isMasterTab() && btnSelectInstrument && btnAddWorkspace) ||
            (!_isMasterTab() && btnSelectInstrument && btnCloseWorkspace)) {
            observer.disconnect();
        }

    }).observe(document.body, { childList: true, subtree: true });

})();
