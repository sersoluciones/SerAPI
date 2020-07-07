import { AuthService } from 'src/app/common/auth/auth.service';
import { FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { ClaimsService, AwsService, WhenScrolledDirective } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { BaseEventService } from './base-event.service';
import { DownloadService } from '../utils/download.service';
import { BaseCrud } from './crud';
import { takeUntil } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SnackbarService } from '../snackbar/snackbar.service';

/**
 * Clase base para crear listados consultando via GraphQL con creación, edición y eliminación en masa sobre los resultados
 */
export class BaseCrudRead extends BaseCrud {

    objectList = [];
    objectListInitial = [];

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService,
                protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService, public claimService: ClaimsService,
                protected baseService: BaseEventService, public aws: AwsService, protected _downloadService: DownloadService, public apiUrl: string, public graphQLUrl: string) {
        super(_fb, _http, _modalService, _dialogService, _snackBar, _soundService, _translate, _auth, claimService, baseService, aws, _downloadService, apiUrl, graphQLUrl);
    }

    toggleSelected() {

        let selectedObjects = 0;

        this.objectList.forEach(element => {
            if (element._isSelected) {
                selectedObjects++;
            }
        });

        this.state.selectedObjects = selectedObjects;

    }

    getSelectedObjects() {
        return this.objectList.filter(item => item._isSelected);
    }

    protected reInitList(): void {
        this.objectList = [];
    }

    addObject() { }

    afterInit() {

        this.toolbarAdd.add.pipe(takeUntil(this.notifierDestroySubs)).subscribe(() => {
            this.addObject();
        });

    }
}
