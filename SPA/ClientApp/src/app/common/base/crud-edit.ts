import { AuthService } from 'src/app/common/auth/auth.service';
import { FormBuilder, FormGroup, FormArray, FormControl } from '@angular/forms';
import { ClaimsService, AwsService, hasValue } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { BaseEventService } from './base-event.service';
import { DownloadService } from '../utils/download.service';
import { BaseCrud } from './crud';
import { take, takeUntil, catchError, map } from 'rxjs/operators';
import { forkJoin, Observable, of } from 'rxjs';
import { CrudEditToolsComponent } from 'src/app/ui/crud/crud-edit-tools/crud-edit-tools.component';
import { SnackbarService } from '../snackbar/snackbar.service';

/**
 * Clase base para crear listados consultando via GraphQL con creación, edición y eliminación en masa sobre los resultados
 */
export class BaseCrudEdit extends BaseCrud {

    toolbarEdit: CrudEditToolsComponent;
    objectList = new FormArray([]);
    objectListInitial: FormArray;

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService,
                protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService, public claimService: ClaimsService,
                protected baseService: BaseEventService, public aws: AwsService, protected _downloadService: DownloadService, public apiUrl: string, public graphUrl: string) {
        super(_fb, _http, _modalService, _dialogService, _snackBar, _soundService, _translate, _auth, claimService, baseService, aws, _downloadService, apiUrl, graphUrl);
    }

    /**
     * Sobreescribir este metodo para insertar un nuevo FormGroup a la lista
     */
    newObject(): FormGroup {
        throw new Error('Method not implemented: BaseCrudEdit.newObject()');
    }

    addObject() {
        this.state.formMode = true;
        const control = this.newObject();
        control.addControl('_new', new FormControl(true));
        control.addControl('_isSelected', new FormControl(false));
        this.objectList.insert(0, control);
    }

    /**
     * Sobreescribir este metodo para insertar el FormGroup requerido representando a un elemento ya existente en la lista
     * @param object item de la lista a mostrar
     */
    listObject(element: any): FormGroup {
        throw new Error('Method not implemented: BaseCrudEdit.listObject()');
    }

    protected reInitList(): void {
        this.objectList = new FormArray([]);
    }

    toggleSelected() {

        let selectedObjects = 0;

        this.objectList.value.forEach(element => {
            if (element._isSelected) {
                selectedObjects++;
            }
        });

        this.state.selectedObjects = selectedObjects;

    }

    getSelectedObjects() {
        return this.objectList.value.filter(item => item._isSelected);
    }

    saveAll() {
        if (!this.state.isSaving) {
            this.state.isSaving = true;
            const objects: Observable<any>[] = [];
            const errors = [];

            this.objectList.controls.forEach((control: FormGroup) => {
                if (control.dirty) {

                    if (control.value._new) {
                        objects.push(
                            this._http.post(
                                    this.apiUrl + this.restUrl, control.value,
                                    {observe: 'response'}
                                )
                                .pipe(catchError(e => of(e)))
                                .pipe(map(res => {

                                    if (res.status === 201) {
                                        control.removeControl('_new');
                                        control.addControl(this.PKField, new FormControl(res.body[this.PKField]));
                                        control.markAsPristine();
                                    } else {
                                        errors.push(res.error?.Error);
                                    }

                                    return res;
                                }))
                        );
                    } else {
                        objects.push(
                            this._http.put(
                                    this.apiUrl + this.restUrl + control.value[this.PKField], control.value,
                                    {observe: 'response'}
                                )
                                .pipe(catchError(e => of(e)))
                                .pipe(map(res => {

                                    if (res.status === 200) {
                                        control.markAsPristine();
                                    } else {
                                        errors.push(res.error?.Error);
                                    }

                                    return res;
                                }))
                        );
                    }
                }
            });

            forkJoin(objects).pipe(take(1)).subscribe(() => {

                if (hasValue(errors)) {
                    this._dialogService.errorBulk({
                        title: (errors.length === objects.length) ? 'update_fail' : 'update_with_errors',
                        errors
                    });
                } else {
                    this.state.formMode = false;
                    this._snackBar.updated();
                }

            }, () => {}, () => {
                this.state.isSaving = false;
            });
        }
    }

    afterInit() {

        this.toolbarAdd.add.pipe(takeUntil(this.notifierDestroySubs)).subscribe(() => {
            this.addObject();
        });

        this.toolbarEdit.save.pipe(takeUntil(this.notifierDestroySubs)).subscribe(() => {
            this.saveAll();
        });

    }
}
