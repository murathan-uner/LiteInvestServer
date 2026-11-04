
class WorkspacePicker extends AlgoDialog {
    constructor(data, config) {
        const contentHtml = WorkspacePicker.buildContentHtml();
        super({
            title: __('open_workspace'),
            contentHtml,
            customClass: 'workspace-dialog',
            isModal: true,
            //hasCloseButton: false
            closeCallback: config.closeCallback
        });

        this.tabProfiles = data.tabProfiles;
        this.selectWorkspaceCallback = config.selectWorkspaceCallback;
        this.pickedCount = 0;
        this.init();
    }

    static buildContentHtml() {
        return `
            <div class="workspace-picker-message">
                ${__('open_workspace_message')}
            </div>
            <div class="workspace-picker-profiles-container">
            </div>
        `;
    }

    init() {
        const base = this.dialog;
        this.profileContainer = base.querySelector('.workspace-picker-profiles-container');
        this.profileContainer.innerHTML = '';

        this.tabProfiles.forEach((tabProfile) => {
            const btn = document.createElement('div');
            btn.classList = 'algo-dialog-btn algo-dialog-btn-filled workspace-picker-btn';
            btn.innerHTML = `<i class="bi bi-window" style="padding-right:4px;"></i> ${tabProfile.caption}`;

            btn.onclick = (e) => {
                e.preventDefault();
                if (this.selectWorkspaceCallback) {
                    this.selectWorkspaceCallback(tabProfile);
                    btn.classList.add('disabled');
                    this.pickedCount++;
                    if (this.pickedCount == this.tabProfiles.length) {
                        this.close();
                    }
                }
            };

            this.profileContainer.appendChild(btn);
        });
    }

    close() {
        super.close();
    }

    getRemainedTabs() {
        const buttons = this.profileContainer.querySelectorAll('.workspace-picker-btn');
        const remained = [];

        buttons.forEach((btn, index) => {
            if (!btn.classList.contains('disabled')) {
                remained.push(this.tabProfiles[index].id);
            }
        });

        return remained;
    }
}
