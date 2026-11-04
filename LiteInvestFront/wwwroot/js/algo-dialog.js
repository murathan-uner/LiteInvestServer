class AlgoDialog {
    constructor({
        title = '',
        contentHtml = '',
        customClass = '',
        isModal = false,
        hasCloseButton = true,
        hasTitleBar = true,
        resizable = false,
        profile = undefined,
        closingCallback = undefined,
        closedCallback = undefined
    }) {
        this.title = title;
        this.contentHtml = contentHtml;
        this.customClass = customClass;
        this.isModal = isModal;
        this.hasCloseButton = hasCloseButton;
        this.hasTitleBar = hasTitleBar;
        this.closingCallback = closingCallback;
        this.closedCallback = closedCallback;
        this.dialogId = `algo-dialog-${Date.now()}`;
        this.zIndex = AlgoDialog.getNextZIndex(); // Track z-index for this dialog

        // Context menu properties
        this.contextMenu = null;
        this.contextMenuItems = [];

        // Bind methods for context menu
        this.boundHideContextMenu = this.hideContextMenu.bind(this);

        // Bind methods for event listeners
        this.boundStopDrag = this.stopDrag.bind(this);
        this.boundDrag = this.drag.bind(this);

        this.resizable = resizable;
        if (this.resizable && !this.isModal) {
            this.isResizing = false;
            this.resizeDirection = null;
            this.startWidth = 0;
            this.startHeight = 0;
            this.startX = 0;
            this.startY = 0;
            this.minWidth = 200;
            this.minHeight = 150;
            const initWidth = (profile && profile.bound) ? profile.bound.width : 600;
            const initHeight = (profile && profile.bound) ? profile.bound.height : 360;
            this.initWidth = initWidth < this.minWidth ? this.minWidth : initWidth;
            this.initHeight = initHeight < this.minHeight ? this.minHeight : initHeight;

            // Bind resize methods
            this.boundStartResize = this.startResize.bind(this);
            this.boundResize = this.resize.bind(this);
            this.boundStopResize = this.stopResize.bind(this);
        }

        this.createDialog();
    }

    // Static property to track all open dialogs
    static openDialogs = [];
    static baseZIndex = 10000; // Starting z-index from the CSS

    // Static method to get next available z-index
    static getNextZIndex() {
        return AlgoDialog.baseZIndex + AlgoDialog.openDialogs.length;
    }

    // Static method to bring a dialog to front
    static bringToFront(dialogInstance) {
        // Move this dialog to end of array (top in z-order)
        const index = AlgoDialog.openDialogs.indexOf(dialogInstance);
        if (index > -1) {
            AlgoDialog.openDialogs.splice(index, 1);
            AlgoDialog.openDialogs.push(dialogInstance);
        }

        // Update z-indexes for all dialogs
        AlgoDialog.openDialogs.forEach((dialog, i) => {
            dialog.zIndex = AlgoDialog.baseZIndex + i;
            if (dialog.wrapper) {
                dialog.wrapper.style.zIndex = dialog.zIndex;
            }
        });
    }

    createDialog() {
        this.wrapper = document.createElement('div');
        this.wrapper.className = 'algo-dialog-wrapper';

        if (this.isModal) {
            this.wrapper.classList.add('algo-dialog-modal');
        }

        // Add to open dialogs list
        AlgoDialog.openDialogs.push(this);

        // Click handler to bring to front
        this.wrapper.addEventListener('mousedown', () => {
            AlgoDialog.bringToFront(this);
        });

        this.dialog = document.createElement('div');
        if (this.customClass === '') {
            this.dialog.className = 'algo-dialog';
        } else {
            this.dialog.classList = `algo-dialog ${this.customClass}`;
        }
        this.dialog.id = this.dialogId;

        // Title Bar (if enabled)
        if (this.hasTitleBar) {
            const titleBar = document.createElement('div');
            titleBar.className = 'algo-dialog-titlebar';

            let titleBarContent = `<span class="algo-dialog-title">${this.title}</span>`;

            if (this.hasCloseButton) {
                titleBarContent += `<span class="algo-dialog-close">${this.getCloseSvg()}</span>`;
            }
            titleBar.innerHTML = titleBarContent;

            if (this.hasCloseButton) {
                titleBar.querySelector('.algo-dialog-close').onclick = (e) => {
                    e.preventDefault();
                    this.close();
                }
            }

            if (!this.isModal) {
                titleBar.onmousedown = (e) => {
                    this.startDrag(e);
                };
                //document.onmouseup = () => this.stopDrag();
                //document.onmousemove = (e) => this.drag(e);
            }

            this.dialog.appendChild(titleBar);
        }

        // Content Area
        const contentArea = document.createElement('div');
        contentArea.className = 'algo-dialog-content';
        contentArea.innerHTML = this.contentHtml;
        this.dialog.appendChild(contentArea);

        // Resizer handlers
        if (this.resizable && !this.isModal) {
            this.createResizeHandles();
        }

        this.wrapper.appendChild(this.dialog);
        document.body.appendChild(this.wrapper);

        // Center dialog
        this.dialog.style.position = 'fixed';
        this.dialog.style.top = '50%';
        this.dialog.style.left = '50%';
        this.dialog.style.transform = 'translate(-50%, -50%)';
        if (this.resizable && !this.isModal) {
            this.dialog.style.width = `${this.initWidth}px`;
            this.dialog.style.height = `${this.initHeight}px`;
            this.dialog.style.minWidth = `${this.minWidth}px`;
            this.dialog.style.minHeight = `${this.minHeight}px`;
        }

        // For subpixel rendering / anti-aliasing
        requestAnimationFrame(() => {
            const rect = this.dialog.getBoundingClientRect();
            this.dialog.style.transform = '';
            this.dialog.style.left = `${rect.left}px`;
            this.dialog.style.top = `${rect.top-1}px`;
        });
    }

    close() {
        if (this.closingCallback)
            this.closingCallback();
        this.cleanupContextMenu();
        this.cleanupResizeHandler();
        if (this.wrapper) {
            // Remove from open dialogs list
            const index = AlgoDialog.openDialogs.indexOf(this);
            if (index > -1) {
                AlgoDialog.openDialogs.splice(index, 1);
            }

            document.body.removeChild(this.wrapper);
        }
        if (this.closedCallback)
            this.closedCallback();
    }

    startDrag(e) {
        if (e.button !== 0 || this.isModal) return;

        const rect = this.dialog.getBoundingClientRect();
        this.dialog.style.transform = '';
        this.dialog.style.top = `${rect.top}px`;
        this.dialog.style.left = `${rect.left}px`;
        this.dialog.style.position = 'fixed';

        this.dragOffsetX = e.clientX - rect.left;
        this.dragOffsetY = e.clientY - rect.top;
        this.isDragging = true;

        // Add event listeners
        document.addEventListener('mouseup', this.boundStopDrag);
        document.addEventListener('mousemove', this.boundDrag);
    }

    stopDrag() {
        this.isDragging = false;
        // Remove event listeners
        document.removeEventListener('mouseup', this.boundStopDrag);
        document.removeEventListener('mousemove', this.boundDrag);
    }

    drag(e) {
        if (!this.isDragging || this.isModal) return;
        e.preventDefault();
        this.dialog.style.position = 'fixed';
        this.dialog.style.left = `${e.clientX - this.dragOffsetX}px`;
        this.dialog.style.top = `${e.clientY - this.dragOffsetY}px`;
    }

    createResizeHandles() {
        const directions = ['n', 'e', 's', 'w', 'ne', 'nw', 'se', 'sw'];

        directions.forEach(dir => {
            const resizer = document.createElement('div');
            resizer.className = `algo-dialog-resizer algo-dialog-resizer-${dir}`;
            resizer.dataset.direction = dir;
            resizer.addEventListener('mousedown', this.boundStartResize);
            this.dialog.appendChild(resizer);
        });
    }

    startResize(e) {
        if (this.isModal) return;

        e.preventDefault();
        e.stopPropagation();

        this.isResizing = true;
        this.resizeDirection = e.target.dataset.direction;

        // Store initial dimensions and position
        const rect = this.dialog.getBoundingClientRect();
        this.startWidth = rect.width;
        this.startHeight = rect.height;
        this.startLeft = rect.left;
        this.startTop = rect.top;
        this.startX = e.clientX;
        this.startY = e.clientY;

        document.body.classList.add('algo-dialog-resizing');
        document.addEventListener('mousemove', this.boundResize);
        document.addEventListener('mouseup', this.boundStopResize);
    }

    resize(e) {
        if (!this.isResizing) return;

        e.preventDefault();

        // Calculate mouse movement since start
        const deltaX = e.clientX - this.startX;
        const deltaY = e.clientY - this.startY;

        let newWidth = this.startWidth;
        let newHeight = this.startHeight;
        let newLeft = this.startLeft;
        let newTop = this.startTop;

        // Handle width changes (left and right edges)
        if (this.resizeDirection.includes('e')) {
            // Right edge - increase width by deltaX
            newWidth = Math.max(this.minWidth, this.startWidth + deltaX);
        }
        if (this.resizeDirection.includes('w')) {
            // Left edge - decrease width by deltaX and move right
            newWidth = Math.max(this.minWidth, this.startWidth - deltaX);
            newLeft = this.startLeft + deltaX;
        }

        // Handle height changes (top and bottom edges)
        if (this.resizeDirection.includes('s')) {
            // Bottom edge - increase height by deltaY
            newHeight = Math.max(this.minHeight, this.startHeight + deltaY);
        }
        if (this.resizeDirection.includes('n')) {
            // Top edge - decrease height by deltaY and move down
            newHeight = Math.max(this.minHeight, this.startHeight - deltaY);
            newTop = this.startTop + deltaY;
        }

        // Apply the new dimensions and position
        this.dialog.style.width = `${newWidth}px`;
        this.dialog.style.height = `${newHeight}px`;
        this.dialog.style.left = `${newLeft}px`;
        this.dialog.style.top = `${newTop}px`;

        // Update content area height
        this.updateContentHeight();

        return { dx: deltaX, dy: deltaY };
    }

    stopResize() {
        this.isResizing = false;
        this.resizeDirection = null;

        document.body.classList.remove('algo-dialog-resizing');
        document.removeEventListener('mousemove', this.boundResize);
        document.removeEventListener('mouseup', this.boundStopResize);
    }

    updateContentHeight() {
        const contentArea = this.dialog.querySelector('.algo-dialog-content');
        if (contentArea) {
            const rect = this.dialog.getBoundingClientRect();
            const titleBarHeight = this.hasTitleBar ?
                this.dialog.querySelector('.algo-dialog-titlebar').offsetHeight : 0;
            contentArea.style.height = `${rect.height - titleBarHeight}px`;
        }
    }

    /* Base Context Menu Methods */
    createContextMenu() {
        this.contextMenu = document.createElement('div');
        this.contextMenu.className = 'algo-dialog-context-menu';
        document.body.appendChild(this.contextMenu);

        // Close menu when clicking outside
        document.addEventListener('click', this.boundHideContextMenu);
        window.addEventListener('scroll', this.boundHideContextMenu, true);
    }

    addContextMenuItem(item) {
        if (!this.contextMenu) {
            this.createContextMenu();
        }

        const menuItem = document.createElement('div');
        menuItem.className = `algo-dialog-context-menu-item ${item.submenu ? 'has-submenu' : ''}`;

        // Create the item content
        let itemContent = '';
        if (item.icon) {
            itemContent = `
                <span class="algo-dialog-context-menu-icon">${item.icon}</span>
                <span class="algo-dialog-context-menu-item-text">${item.text}</span>
            `;
        } else {
            itemContent = `
                <span class="algo-dialog-context-menu-item-text">${item.text}</span>
            `;
        }

        // Add caret if there's a submenu
        if (item.submenu) {
            itemContent += `
                <span class="algo-dialog-context-menu-caret">${this.getRightCaretSvg()}</span>
            `;
        }

        menuItem.innerHTML = itemContent;

        if (item.action) {
            menuItem.addEventListener('click', item.action);
        }

        if (item.submenu) {
            const submenu = this.createSubmenu(item.submenu);
            menuItem.appendChild(submenu);

            menuItem.addEventListener('mouseenter', () => {
                submenu.style.display = 'block';
                this.positionSubmenu(submenu, menuItem);
            });

            menuItem.addEventListener('mouseleave', () => {
                submenu.style.display = 'none';
            });
        }

        this.contextMenu.appendChild(menuItem);
        this.contextMenuItems.push(item);
        return menuItem;
    }

    addContextMenuSeparator() {
        if (!this.contextMenu) {
            this.createContextMenu();
        }

        const separator = document.createElement('div');
        separator.className = 'algo-dialog-context-menu-separator';
        this.contextMenu.appendChild(separator);
        return separator;
    }

    createSubmenu(items) {
        const submenu = document.createElement('div');
        submenu.className = 'algo-dialog-context-submenu';

        items.forEach(item => {
            const submenuItem = document.createElement('div');
            submenuItem.className = `algo-dialog-context-submenu-item ${item.checked ? 'checked' : ''}`;

            let itemContent = '';
            if (item.icon) {
                itemContent = `
                    <span class="algo-dialog-context-menu-icon">${item.icon}</span>
                    <span class="algo-dialog-context-submenu-item-text">${item.text}</span>
                `;
            } else {
                itemContent = `
                    <span class="algo-dialog-context-submenu-item-text">${item.text}</span>
                `;
            }

            // Add checkmark if this is a checkable item
            if (item.checkable) {
                itemContent += `
                    <span class="algo-dialog-context-submenu-item-check">${this.getCheckSvg()}</span>
                `;
            }

            submenuItem.innerHTML = itemContent;

            submenuItem.addEventListener('click', () => {
                if (item.checkable) {
                    // Uncheck all items in this group first
                    if (item.group) {
                        submenu.querySelectorAll('.algo-dialog-context-submenu-item').forEach(el => {
                            if (el.dataset.group === item.group) {
                                el.classList.remove('checked');
                            }
                        });
                    }
                    submenuItem.classList.add('checked');
                }
                item.action?.();
                this.hideContextMenu();
            });

            if (item.group) {
                submenuItem.dataset.group = item.group;
            }

            submenu.appendChild(submenuItem);
        });

        return submenu;
    }

    positionSubmenu(submenu, parentItem) {
        const submenuRect = submenu.getBoundingClientRect();
        const parentRect = parentItem.getBoundingClientRect();

        // Check if submenu would go off screen
        if (parentRect.right + submenuRect.width > window.innerWidth) {
            submenu.style.left = 'auto';
            submenu.style.right = '100%';
        } else {
            submenu.style.left = '100%';
            submenu.style.right = 'auto';
        }
    }

    showContextMenu(x, y) {
        if (!this.contextMenu) return;

        this.contextMenu.style.display = 'block';
        this.contextMenu.style.left = `${x}px`;
        this.contextMenu.style.top = `${y}px`;

        // Ensure menu stays on screen
        const rect = this.contextMenu.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            this.contextMenu.style.left = `${window.innerWidth - rect.width - 5}px`;
        }
        if (rect.bottom > window.innerHeight) {
            this.contextMenu.style.top = `${window.innerHeight - rect.height - 5}px`;
        }
    }

    hideContextMenu() {
        if (this.contextMenu) {
            this.contextMenu.style.display = 'none';
            // Hide all submenus
            const submenus = this.contextMenu.querySelectorAll('.algo-dialog-context-submenu');
            submenus.forEach(submenu => {
                submenu.style.display = 'none';
            });
        }
    }

    cleanupContextMenu() {
        if (this.contextMenu && document.body.contains(this.contextMenu)) {
            document.removeEventListener('click', this.boundHideContextMenu);
            window.removeEventListener('scroll', this.boundHideContextMenu, true);
            document.body.removeChild(this.contextMenu);
            this.contextMenu = null;
        }
    }

    cleanupResizeHandler() {
        if (this.boundResize)
            document.removeEventListener('mousemove', this.boundResize);
        if (this.boundStopResize)
            document.removeEventListener('mouseup', this.boundStopResize);
    }

    getCloseSvg() {
        return `
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" width="10" height="10">
                <line x1="0" y1="0" x2="10" y2="10" stroke="#222" stroke-width="1"/>
                <line x1="0" y1="10" x2="10" y2="0" stroke="#222" stroke-width="1"/>
            </svg>
        `;
    }

    getCloseSvgRed() {
        return `
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" width="10" height="10">
                <line x1="0" y1="0" x2="10" y2="10" stroke="red" stroke-width="1"/>
                <line x1="0" y1="10" x2="10" y2="0" stroke="red" stroke-width="1"/>
            </svg>
        `;
    }

    getSortAscSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="14" height="14">
            <path d="M256 192v224h-32V192h-96L240 64l112 128z" fill="#222"></path>
        </svg>`;
    }

    getSortDescSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="14" height="14">
            <path d="M352 288 240 416 128 288h96V64h32v224z" fill="#222"></path>
        </svg>`;
    }

    getRightCaretSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="10" height="10">
            <path d="m158.059 129.941 126.06 126.06-126.06 126.061L192 416l160-159.999L192 96z"></path>
        </svg>`;
    }

    getCheckSvg() {
        return `<svg xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="10" height="10">
            <path d="M434.7 82.7 480 128 192 416 32 256l45.3-45.3L192 325.5z"></path>
        </svg>`;
    }

    getProfile() {
        const rect = this.dialog.getBoundingClientRect();
        const profile = {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
        return profile;
    }
}