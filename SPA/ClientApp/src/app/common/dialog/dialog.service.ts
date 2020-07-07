import { DialogErrorBulkComponent } from './dialog-error-bulk/error-bulk.component';
import { MatDialog } from '@angular/material/dialog';
import { Injectable } from '@angular/core';
import { DialogSimpleComponent } from './simple/simple.component';
import { SimpleMessage } from './message';
import { SoundService } from '../sound/sound.service';
import { HttpErrorResponse } from '@angular/common/http';
import { DialogErrorComponent } from './error/error.component';
import { DialogUnsavedFormComponent } from './unsaved-form/unsaved-form.component';

@Injectable({
    providedIn: 'root'
})
export class DialogService {

    constructor(private modalService: MatDialog, private soundService: SoundService) { }

    simple(data: SimpleMessage) {

        this.soundService.notify();

        return this.modalService.open(DialogSimpleComponent, {
            data,
            closeOnNavigation: true
        });
    }

    unsavedForm() {

        this.soundService.alert();

        return this.modalService.open(DialogUnsavedFormComponent);
    }

    error(error: HttpErrorResponse) {
        this.soundService.alert();

        return this.modalService.open(DialogErrorComponent, {
            data: {
                reject: error
            },
            closeOnNavigation: true
        });
    }

    errorBulk(data: any) {
        this.soundService.alert();

        return this.modalService.open(DialogErrorBulkComponent, {
            data,
            closeOnNavigation: true
        });
    }

}
