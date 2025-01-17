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
                        console.log(otherElement);
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
    if (header) {
        headerHeight = header.clientHeight;
    }

    handleScroll(targetElement, gridTableId, headerHeight);

    const config = {
        childList: true,
        subtree: true
    };

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