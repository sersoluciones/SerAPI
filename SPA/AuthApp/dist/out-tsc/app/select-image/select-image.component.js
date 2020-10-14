import { __decorate, __param } from "tslib";
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
let SelectImageComponent = class SelectImageComponent {
    constructor(dialog, data) {
        this.dialog = dialog;
        this.data = data;
        this.files = [];
        // Variables para recortar la imagen
        this.imageChangedEvent = '';
        this.croppedImage = '';
    }
    cancel() {
        this.dialog.close();
    }
    ngOnInit() {
        if (this.data.round == null) {
            this.data.round = false;
        }
        if (this.data.file != null) {
            this.imageChangedEvent = { target: { files: [this.data.file] } };
        }
    }
    // Funciones para cuando se recorta la imagen
    imageCropped(event) {
        this.croppedImage = event.base64;
    }
    imageLoaded() {
        console.log('Image loaded');
    }
    cropperReady(sourceImageDimensions) {
        console.log('Cropper ready', sourceImageDimensions);
    }
    loadImageFailed() {
        console.log('Load failed');
    }
};
SelectImageComponent = __decorate([
    Component({
        templateUrl: './select-image.component.html',
        styleUrls: ['./select-image.component.scss']
    }),
    __param(1, Inject(MAT_DIALOG_DATA))
], SelectImageComponent);
export { SelectImageComponent };
//# sourceMappingURL=select-image.component.js.map