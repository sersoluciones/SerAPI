import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
    templateUrl: './error-bulk.component.html',
    styleUrls: ['./error-bulk.component.scss']
})
export class DialogErrorBulkComponent {

    constructor(public dialogRef: MatDialogRef<DialogErrorBulkComponent>, @Inject(MAT_DIALOG_DATA) public data: any) { }

    close() {
        this.dialogRef.close();
    }

}
