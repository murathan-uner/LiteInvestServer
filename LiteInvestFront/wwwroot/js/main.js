window.focusElementById = (windowId) => {
    const element = document.getElementById(windowId);
    if (element) {
        console.log(element);
        element.focus({ preventScroll: true });
    }
};

function getScrollEventForAllTables(gridTableId) {
    let parent = document.getElementById(gridTableId);
    if (parent) {
        console.log(parent);
        parent.addEventListener("scroll", (e) => {
            if (e.target && e.target.classList.contains("k-grid-content")) {
                let targetElements = parent.querySelectorAll(".k-grid-content");
                let scrollTop = e.target.scrollTop;

                targetElements.forEach((otherElement) => {
                    if (otherElement !== e.target) {
                        //console.log(otherElement);
                        otherElement.scrollTop = scrollTop;
                    }
                });
            }
        }, true);
    }
}

function getScrollEvent2(gridTableId)
{
    const parent = document.getElementById(gridTableId);
    if (!parent) {
        console.log("Родительский элемент с ID " + gridTableId + " не найден");
        return;
    }

    const targetElement = parent.querySelector(".k-grid-content");
    if (!targetElement) {
        console.log("Элемент .k-grid-content не найден в родительском элементе");
        return;
    }

    const table = targetElement.querySelector(".k-grid-table");
    if (table) {
        table.classList.add("scroll-table");
    }

    let headerHeight = 0;
    const header = document.querySelector("#header");
    if (header)
    {
        headerHeight = header.clientHeight;
    }

    handleScroll(targetElement, gridTableId, headerHeight);

    function handleScroll(targetElement, gridTableId, headerHeight) {
        const visibleHeight = targetElement.clientHeight;
        const rows = targetElement.querySelectorAll("tr");

        let firstVisibleRowPrice = null;
        let lastVisibleRowPrice = null;
        let visibleRowCount = 0;

        rows.forEach((row) => {
            const rect = row.getBoundingClientRect();

            if (rect.top < visibleHeight && rect.bottom > 0) {
                const priceCell = row.querySelector(".price");

                if (priceCell) {
                    const price = priceCell.textContent.trim();

                    if (firstVisibleRowPrice === null) {
                        firstVisibleRowPrice = price;
                    }
                    lastVisibleRowPrice = price;

                    visibleRowCount++;
                }
            }
        });

        if (firstVisibleRowPrice !== null && lastVisibleRowPrice !== null) {
            DOTNET_JSINTEROPSERVICE_REFERENCE.invokeMethodAsync(
                "OnScroll", gridTableId,
                firstVisibleRowPrice,
                lastVisibleRowPrice,
                visibleRowCount
            ).catch((error) => {
                console.error("Ошибка вызова метода OnScroll:", error);
            });
        }
    }
}

function getScrollEvent2(gridTableId)
{
    const parent = document.getElementById(gridTableId);
    if (!parent) {
        console.log("Родительский элемент с ID " + gridTableId + " не найден");
        return;
    }

    const targetElement = parent.querySelector(".k-grid-content");
    if (!targetElement) {
        console.log("Элемент .k-grid-content не найден в родительском элементе");
        return;
    }

    const table = targetElement.querySelector(".k-grid-table");
    if (table) {
        table.classList.add("scroll-table");
    }

    let headerHeight = 0;
    const header = document.querySelector("#header");
    if (header) {
        headerHeight = header.clientHeight;
    }

   
    handleScroll(targetElement, gridTableId, headerHeight);

    function handleScroll(targetElement, gridTableId, headerHeight) {
        const visibleHeight = targetElement.clientHeight;
        const rows = targetElement.querySelectorAll("tr");

        let firstVisibleRowPrice = null;
        let lastVisibleRowPrice = null;
        let visibleRowCount = 0;

        rows.forEach((row) => {
            const rect = row.getBoundingClientRect();

            if (rect.top < visibleHeight && rect.bottom > 0) {
                const priceCell = row.querySelector(".price");

                if (priceCell) {
                    const price = priceCell.textContent.trim();

                    if (firstVisibleRowPrice === null) {
                        firstVisibleRowPrice = price;
                    }
                    lastVisibleRowPrice = price;

                    visibleRowCount++;
                }
            }
        });

        if (firstVisibleRowPrice !== null && lastVisibleRowPrice !== null) {
            DOTNET_JSINTEROPSERVICE_REFERENCE.invokeMethodAsync(
                "OnScroll", gridTableId,
                firstVisibleRowPrice,
                lastVisibleRowPrice,
                visibleRowCount
            ).catch((error) => {
                console.error("Ошибка вызова метода OnScroll:", error);
            });
        }
    }
}

function getScrollEvent(gridTableId)
{
    const parent = document.getElementById(gridTableId);
    if (!parent) {
        console.log("Родительский элемент с ID " + gridTableId + " не найден");
        return;
    }

    const targetElement = parent.querySelector(".k-grid-content");
    if (!targetElement) {
        console.log("Элемент .k-grid-content не найден в родительском элементе");
        return;
    }

    const table = targetElement.querySelector(".k-grid-table");
    if (table) {
        table.classList.add("scroll-table");
    }

    let headerHeight = 0;
    const header = document.querySelector("#header");
    if (header) {
        headerHeight = header.clientHeight;
    }

    let debounceTimer;
    const onScroll = (event) =>
    {
        //clearTimeout(debounceTimer);
        //debounceTimer = setTimeout(() => {
        //    handleScroll(targetElement, gridTableId, headerHeight);
        //}, 100);

        handleScroll(targetElement, gridTableId, headerHeight);
    };

    targetElement.addEventListener('scroll', onScroll);

    const observer = new MutationObserver(() => {
        if (!parent.contains(targetElement)) {
            observer.disconnect();
            targetElement.removeEventListener('scroll', onScroll);
        }
    });

    const config = {
        childList: true,
        subtree: true
    };

    observer.observe(document.body, config);

    function handleScroll(targetElement, gridTableId, headerHeight) {
        const visibleHeight = targetElement.clientHeight;
        const rows = targetElement.querySelectorAll("tr");

        let firstVisibleRowPrice = null;
        let lastVisibleRowPrice = null;
        let visibleRowCount = 0;

        rows.forEach((row) => {
            const rect = row.getBoundingClientRect();

            if (rect.top < visibleHeight && rect.bottom > 0) {
                const priceCell = row.querySelector(".price");

                if (priceCell) {
                    const price = priceCell.textContent.trim();

                    if (firstVisibleRowPrice === null) {
                        firstVisibleRowPrice = price;
                    }
                    lastVisibleRowPrice = price;

                    visibleRowCount++;
                }
            }
        });

        if (firstVisibleRowPrice !== null && lastVisibleRowPrice !== null) {
            DOTNET_JSINTEROPSERVICE_REFERENCE.invokeMethodAsync(
                "OnScroll", gridTableId,
                firstVisibleRowPrice,
                lastVisibleRowPrice,
                visibleRowCount
            ).catch((error) => {
                console.error("Ошибка вызова метода OnScroll:", error);
            });
        }
    }
}

function getWheelEvent(historyTableId) {
    let parent = document.getElementById(historyTableId);
    if (parent) {
        let targetElement = parent.querySelector(".k-grid-content");
        if (targetElement) {
            targetElement.addEventListener('wheel', (event) => {

                if (parent) {
                    DOTNET_JSINTEROPSERVICE_REFERENCE.invokeMethodAsync("OnWheel", historyTableId);

                    const isZoomIn = event.deltaY < 0; // Скролл вверх (+) — увеличение
                    const isZoomOut = event.deltaY > 0; // Скролл вниз (-) — уменьшение

                    // Задаем шаг изменения ширины
                    const zoomStep = 30; // Шаг изменения ширины в пикселях

                    const columns = targetElement.querySelectorAll('th, td');

                    columns.forEach((column, index) => {
                        const currentWidth = parseInt(window.getComputedStyle(column).width, 10);

                        // Если скроллим вверх (увеличение), увеличиваем ширину
                        if (isZoomIn && currentWidth < 300) {
                            column.style.width = `${currentWidth + zoomStep}px`;
                        }

                        // Если скроллим вниз (уменьшение), уменьшаем ширину
                        if (isZoomOut && currentWidth > 50) {
                            column.style.width = `${currentWidth - zoomStep}px`;
                        }

                    });
                }
                else {
                    console.log("Элемент с ID " + gridTableId + " не найден");
                }
            });
        }
    } else {
        console.log("Родительский элемент с ID " + gridTableId + " не найден");
    };
}

//function scrollToRow(gridItemId, rowIndex) {
//    let parent = document.getElementById(gridItemId);
//    if (parent) {
//        let scrollableElement = parent.querySelector(".k-grid-content");
//        if (scrollableElement) {
//            let row = scrollableElement.querySelectorAll(".k-table-tbody tr")[rowIndex];
//            if (row) {
//                row.classList.add("row");
//                row.scrollIntoView({ behavior: 'smooth', block: 'center' });
//            }
//        }
//    }
//}

let dropdownVisible = false;
function toggleDropdown() {
    console.log("1");
    var dropdownItems = document.getElementById('dropdown-items');
    if (dropdownItems) {
        dropdownItems.classList.toggle('show');
    }
    dropdownVisible = !dropdownVisible;
}
// Закрыть дропдаун при клике вне его
document.addEventListener('click', function (event) {
    var dropdownToggle = document.getElementById('dropdown-toggle');
    var dropdownItems = document.getElementById('dropdown-items');
    // Проверяем, был ли клик вне дропдауна
    if (dropdownVisible && !dropdownToggle.contains(event.target) && !dropdownItems.contains(event.target)) {
        dropdownItems.classList.remove('show');
        dropdownVisible = false;
    }
});


window.registerViewportChangeCallbackForChart = (dotnetHelper) => {
    let _dotnetHelper = dotnetHelper;
    window.addEventListener('resize', () => {
        _dotnetHelper.invokeMethodAsync('OnResize', window.innerWidth, window.innerHeight);
    });
}

window.myLayout; // Store layout globally

window.initGoldenLayout = () => {
    var config = {
        settings: {
            showPopoutIcon: false,
            showMaximiseIcon: false,
            showCloseIcon: false,
            selectionEnabled: true
        },
        content: [{
            type: 'row',
            //content: [
            //    {
            //        type: 'component',
            //        componentName: 'windows-component'
            //    }
            //]
        }]
    };
    var myLayout = new GoldenLayout(config);

    myLayout.init();

    window.myLayout = myLayout;
}

window.initSplitter = (windowId, windowTitle, idx) => {

    let _windowId = windowId;
    let _windowTitle = windowTitle;

    window.myLayout.registerComponent(windowTitle, function (container, state) {
    });

    let observer = new MutationObserver(() => {

        let __windowId = _windowId;

        let mainSplitter = document.getElementById("main-splitter-container-" + _windowId);
        let clustersSplitter = document.getElementById("clusters-container-" + _windowId);
        if (mainSplitter) {
            new MainSplitter("main-splitter-container-" + _windowId);
        }
        if (clustersSplitter) {
            new Splitter("clusters-container-" + _windowId);
        }
        if (mainSplitter && clustersSplitter) {
            observer.disconnect();

            var newItemConfig = {
                type: 'component',
                componentName: _windowTitle
            };
            window.myLayout.root.contentItems[0].addChild(newItemConfig);

            let observerInternal = new MutationObserver(() => {
                //let els = document.getElementsByClassName('lm_content');
                let els = Array.from(document.getElementsByClassName('lm_content')) // Convert to array
                    .filter(el => !Array.from(el.children).some(child => child.id.startsWith("windows-")));
                let sourceElement = document.getElementById('windows-' + __windowId);
                if (sourceElement && els && els.length > 0) {
                    observerInternal.disconnect();
                    let targetElement = els[0];
                    $(sourceElement).appendTo($(targetElement));
                }
            });

            observerInternal.observe(document.body, { childList: true, subtree: true });
        }
    });

    observer.observe(document.body, { childList: true, subtree: true });
}

window.getElementSizeById = (id) => {
    var el = document.getElementById(id);
    if (!el) return null;

    return {
        width: el.clientWidth,
        height: el.clientHeight
    };
}