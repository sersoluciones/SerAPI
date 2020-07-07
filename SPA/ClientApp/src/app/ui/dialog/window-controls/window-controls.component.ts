import { FormState } from './../../../common/interfaces/base';
import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'window-controls',
  templateUrl: './window-controls.component.html',
  styleUrls: ['./window-controls.component.scss']
})
export class WindowControlsComponent {

    @Input() dialogRef: MatDialogRef<any>;
    @Input() state: FormState;
    @Output() hide: EventEmitter<void> = new EventEmitter();

    @HostListener('dblclick')
    toggleWindow() {

        this.state.maximized = !this.state.maximized;

        if (this.state.maximized) {
            this.dialogRef.addPanelClass('maximized');
        } else {
            this.dialogRef.removePanelClass('maximized');
        }

    }
}
