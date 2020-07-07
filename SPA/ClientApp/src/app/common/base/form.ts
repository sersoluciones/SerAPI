import { AuthService } from 'src/app/common/auth/auth.service';
import { FormBuilder, FormGroup } from '@angular/forms';
import { hasValue, AwsService } from '@sersol/ngx';
import { OnInit, AfterViewInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { FormState } from '../interfaces/base';
import { ReplaySubject, Subscription } from 'rxjs';
import { SnackbarService } from '../snackbar/snackbar.service';

/**
 * @description
 * Clase base para crear formularios con subida de archivos, validación y compresión de imagenes
 */
export class BaseForm implements OnInit, AfterViewInit {

    notifierDestroySubs: ReplaySubject<any> = new ReplaySubject();
    subscriptions = new Subscription();
    modelForm: FormGroup;
    modelFilesForm: any = {};
    PKField = 'id';
    PKValue = null;
    instance: any;

    state: FormState = {
        loading: true,
        isEditing: false,
        isSaving: false,
        isDownloading: false,
        reloadOnclose: false,
        activeTab: 0
    };

    hasValue = hasValue;

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService,
                public aws: AwsService, protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService) { }

    checkFile(type: number, fieldName: string) {

        switch (type) {
            case 1:
                return hasValue(this.instance?.[fieldName]) || (hasValue(this.modelForm.get(fieldName).value) && this.modelForm.get(fieldName).valid);

            default:
                return false;
        }
    }

    setFile(ev: any, field: string) {
        const files = ev.target.files as FileList;

        if (hasValue(files)) {
            this.modelForm.get(field).setValue(files[0], { onlySelf: true });
        }

        ev.target.value = null;
    }

    clearFile(fieldName: string) {
        this.modelForm.get(fieldName).setValue(null, { onlySelf: true });

        if (hasValue(this.instance)) {
            this.instance[fieldName] = null;
        }
    }

    setTab(tab: number) {
        this.state.activeTab = tab;
    }

    protected saveModel() { }
    protected processSubmit() { }
    submit(): void {
        if (!this.state.isSaving) {
            this.state.isSaving = true;
            this.processSubmit();
        }
    }

    beforeSubmit() { }
    afterSubmit() { }

    init(): void { }
    protected _init(): void { }

    ngOnInit() {
        this._init();
        this.init();
    }

    afterInit() { }
    private _afterInit() {

    }

    ngAfterViewInit() {
        this._afterInit();
        this.afterInit();
    }
}
