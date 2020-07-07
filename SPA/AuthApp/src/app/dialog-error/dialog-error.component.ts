import { Component, OnInit, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  templateUrl: './dialog-error.component.html',
  styleUrls: ['./dialog-error.component.scss']
})
export class DialogErrorComponent implements OnInit {

  constructor(private dialog: MatDialogRef<DialogErrorComponent>, @Inject(MAT_DIALOG_DATA) public data: any) { }

  cancel(): void {
    this.dialog.close();
  }

  ngOnInit(): void {
    console.log(this.data);
  }

}
