import { hasValue } from '@sersol/ngx';
import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { SimpleMessage } from '../message';

@Component({
    templateUrl: './simple.component.html',
    styleUrls: ['./simple.component.scss']
})
export class DialogSimpleComponent {

    closeButton = true;

    constructor(public dialogRef: MatDialogRef<DialogSimpleComponent>, @Inject(MAT_DIALOG_DATA) public data: SimpleMessage) {
        if (hasValue(data.closeButton)) {
            this.closeButton = data.closeButton;
        }
    }

    close(): void {
        this.dialogRef.close();
    }

}
