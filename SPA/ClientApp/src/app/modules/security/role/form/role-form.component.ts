import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { hasValue, ClaimsService, AwsService } from '@sersol/ngx';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { take, catchError } from 'rxjs/operators';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';

@Component({
    templateUrl: './role-form.component.html',
    styleUrls: ['./role-form.component.scss']
})
export class RoleFormComponent extends BaseFormDialog {

    modelForm = this._fb.group({
        id: [],
        name: ['', [Validators.required]],
        permissions: []
    });

    modelAux = {
        permissionWrapper: []
    };

    modelFilter: any;

    constructor(public dialogRef: MatDialogRef<RoleFormComponent>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
                protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
                @Inject('API_URL') public apiUrl: string, @Inject('GRAPHQL_URL') public graphQLUrl: string, protected _soundService: SoundService,
                protected _translate: TranslateService, protected _auth: AuthService) {
        super(dialogRef, data, _fb, claimService, _http, _modalService, _dialogService, _snackBar, aws, apiUrl, graphQLUrl, _soundService, _translate, _auth);
    }

    getMessage(): string {
        let msj = '';

        if (!this.claimService.hasPermission('roles.update') && this.state.isEditing) {
            msj = 'permission_update_error';
        }

        return msj;
    }

    undo() {
        if (this.state.isEditing) {
            this.modelForm.patchValue(this.data.instance);
            this.modelForm.markAsPristine();
        } else {
            this.modelForm.reset();
        }
    }

    getPermissionsJson() {

        const objectOfObservables = [
            this._http.get('/PermissionGUI').pipe(catchError(e => of(e)))
        ];

        if (hasValue(this.data?.instance)) {
            objectOfObservables.push(
                this._http.get(this.apiUrl + 'Roles/' + this.data?.instance.id).pipe(catchError(e => of(e)))
            );
        }

        return forkJoin(objectOfObservables);

    }

    processSubmit() {

        const permissions = [];

        this.modelAux.permissionWrapper.forEach(item => {

            for (const permissionsModule of item.Permissions) {
                if (permissionsModule.Selected) {
                    permissions.push(permissionsModule.Value);
                }
            }

            for (const groups of item.Groups) {

                for (const permissionsGroup of groups.Permissions) {
                    if (permissionsGroup.Selected) {
                        permissions.push(permissionsGroup.Value);
                    }
                }

            }

        });

        this._http[this.state.isEditing ? 'put' : 'post'](this.apiUrl + 'Roles' + (this.state.isEditing ? `/${this.instance.id}/` : '/'), this.modelForm.value)
            .pipe(take(1))
            .subscribe((response: any) => {
                this._http.post(this.apiUrl + 'Claims/AddToRole/', {
                    role_id: response.id,
                    permission_names: permissions
                })
                .pipe(take(1))
                .subscribe(() => {
                    this.finishForm();
                }, error => {
                    this.state.loading = false;
                    this.state.isSaving = false;
                });
            }, error => {
                this.state.loading = false;
                this.state.isSaving = false;
            });

    }

    initForm(permissions: any[]) {

        if (hasValue(this.instance)) {
            this.modelForm.patchValue(this.instance);
            this.state.isEditing = true;
        }

        if (this.state.isEditing && hasValue(this.modelForm.controls.permissions.value)) {

            this.modelForm.controls.permissions.value.forEach(item => {

                permissions.forEach(element => {

                    for (const permissionsModule of element.Permissions) {
                        if (permissionsModule.Value === item.claim_value) {
                            permissionsModule.Selected = true;
                        }
                    }

                    for (const groups of element.Groups) {
                        for (const permissionsGroup of groups.Permissions) {
                            if (permissionsGroup.Value === item.claim_value) {
                                permissionsGroup.Selected = true;
                            }
                        }
                    }
                });

            });

        }

        this.modelAux.permissionWrapper = permissions;

    }

    init() {

        this.subscriptions.add(
            this.getPermissionsJson()
            .pipe(take(1))
            .subscribe((response: any[]) => {

                if (hasValue(response[1])) {
                    this.data.instance.permissions = response[1].permissions;
                    this.instance = this.data.instance;
                }

                this.initForm(response[0]);
                this.state.loading = false;

            })
        );

    }

}
