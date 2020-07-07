import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  templateUrl: './unsaved-form.component.html',
  styleUrls: ['./unsaved-form.component.scss']
})
export class DialogUnsavedFormComponent {

    constructor(public dialogRef: MatDialogRef<DialogUnsavedFormComponent>) { }

    cancel(): void {
        this.dialogRef.close(false);
    }

    close(): void {
        this.dialogRef.close(true);
    }

}
