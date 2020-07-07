import { BaseForm } from './form';
import { ViewChild, Inject } from '@angular/core';
import { WindowControlsComponent } from 'src/app/ui/dialog/window-controls/window-controls.component';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { BaseFormData } from '../interfaces/base';
import { FormBuilder } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DialogService } from '../dialog/dialog.service';
import { ClaimsService, AwsService } from '@sersol/ngx';
import { SoundService } from '../sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { take } from 'rxjs/operators';
import { SnackbarService } from '../snackbar/snackbar.service';

export class BaseFormDialog extends BaseForm {

    @ViewChild(WindowControlsComponent) windowControl: WindowControlsComponent;

    constructor(public dialogRef: MatDialogRef<any>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
                protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
                public apiUrl: string, public graphQLUrl: string, protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService) {
        super(_fb, _http, _modalService, _dialogService, _snackBar, aws, _soundService, _translate, _auth);
    }

    canClose(): boolean {
        return !this.modelForm.dirty;
    }

    private _close() {
        this.dialogRef.close(this.state.reloadOnclose);
        this.notifierDestroySubs.next();
        this.notifierDestroySubs.complete();
        this.subscriptions.unsubscribe();
    }

    close() {
        if (this.canClose()) {
            this._close();
        } else {
            this._dialogService.unsavedForm()
            .afterClosed()
            .pipe(take(1))
            .subscribe((close: boolean) => {
                if (close) {
                    this._close();
                }
            });
        }
    }

    finishForm() {
        this.state.reloadOnclose = true;
        this.modelForm.markAsPristine();
        this._snackBar[this.state.isEditing ? 'updated' : 'created']();
        this.close();
    }

    afterInit() {
        this.windowControl.hide.subscribe(() => {
            this.close();
        });
    }

    protected _init(): void {
        this.dialogRef.addPanelClass('object-details');
        this.state.maximized = this.data?.maximized || this._auth.oidcUser.maximized_windows;

        if (this.state.maximized) {
            this.dialogRef.addPanelClass('maximized');
        }
    }

}
