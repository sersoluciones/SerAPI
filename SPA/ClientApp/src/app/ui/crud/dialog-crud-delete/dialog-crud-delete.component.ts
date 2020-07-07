import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  templateUrl: './dialog-crud-delete.component.html',
  styleUrls: ['./dialog-crud-delete.component.scss']
})
export class DialogCrudDeleteComponent {

    constructor(public dialogRef: MatDialogRef<DialogCrudDeleteComponent>, @Inject(MAT_DIALOG_DATA) public data: any) { }

    close(confirm?: boolean): void {
        this.dialogRef.close(confirm);
    }

}
