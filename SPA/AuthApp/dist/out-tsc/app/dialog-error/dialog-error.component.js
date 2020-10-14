import { __decorate, __param } from "tslib";
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
let DialogErrorComponent = class DialogErrorComponent {
    constructor(dialog, data) {
        this.dialog = dialog;
        this.data = data;
    }
    cancel() {
        this.dialog.close();
    }
    ngOnInit() {
        console.log(this.data);
    }
};
DialogErrorComponent = __decorate([
    Component({
        templateUrl: './dialog-error.component.html',
        styleUrls: ['./dialog-error.component.scss']
    }),
    __param(1, Inject(MAT_DIALOG_DATA))
], DialogErrorComponent);
export { DialogErrorComponent };
//# sourceMappingURL=dialog-error.component.js.map