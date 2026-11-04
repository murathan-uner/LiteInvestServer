let DOTNET_HOME;
let DOTNET_MENU;
let DOTNET_WINDOWS;
window.setDotNetHome = (dotnet) => {
    DOTNET_HOME = dotnet;
};
window.setDotNetMenu = (dotnet) => {
    DOTNET_MENU = dotnet;
};
window.setDotNetWindows = (dotnet) => {
    DOTNET_WINDOWS = dotnet;
};
function keyDownHandler(e, windowId, isMouseOverQuoteContainer)
{
    //const windowElement = document.getElementById(windowId);
    //console.log(e.code, windowElement);
    //console.log("code: " + e.target);
    if (e.target && (e.target.nodeName != "INPUT") && e.target.nodeName != "TEXTAREA") {
        if (e.code !== "F5") {
            e.preventDefault();
        }
        processOnKey(windowId, e.code, e.ctrlKey, e.shiftKey, isMouseOverQuoteContainer);
        //DOTNET_HOME.invokeMethodAsync("KeyDown", windowId, e.code, e.ctrlKey, e.shiftKey, isMouseOverQuoteContainer);
    }
    else if (e.target && e.target.nodeName == "INPUT") {
        if (e.code == "Enter" || e.code == "Escape" || e.code == "NumpadEnter") {
            //DOTNET_HOME.invokeMethodAsync("KeyDown", windowId, e.code, e.ctrlKey, e.shiftKey, isMouseOverQuoteContainer);
        }
    }
}
function keyUpHandler(e) {
    //if (e.target && (e.target.nodeName != "INPUT")) {
    //    DOTNET_HOME.invokeMethodAsync("KeyUp", e.code, e.ctrlKey, e.shiftKey);
    //}
}

// Event handler
// Invoked from hotkeyListener
function processOnKey(windowId, keycode, ctrlKey, shiftKey, isMouseOverQuoteContainer) {
    if (keycode == 'ShiftLeft' || keycode == 'ShiftRight') { // force all instruments to scroll to center

        if (windowInstances) {
            windowInstances.forEach(_ => {
                _evScrollBestBidIndex(_.windowId);
            });
        }

    } else if (keycode == 'KeyA') { // automic centering flag on/off

        const window = _iGetWindowInstanceByWindowId(windowId);
        if (window) {
            window.quote.toggleAutomicCentering();
            var automicCentering = window.quote.isAutomicCentering();
            DOTNET_HOME.invokeMethodAsync('OnToggleAutomicCentering', windowId, automicCentering);
        }

    } else {

        if (isMouseOverQuoteContainer) {
            if (keycode == 'KeyM') { // force to scroll to center

                _evScrollBestBidIndex(windowId);

            } else if (keycode == 'Equal') { // scale up
                _evChangeScale(windowId, true);
            } else if (keycode == 'Minus') { // scale down
                _evChangeScale(windowId, false);
            }
        }
    }
}
