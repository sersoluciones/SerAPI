import { Component, OnInit, Inject } from '@angular/core';
import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { Validators, FormBuilder } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { StoreFormComponent } from 'src/app/modules/inventory/store/form/store-form.component';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { ClaimsService, AwsService, hasValue, Patterns } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent extends BaseFormDialog {

  modelForm = this._fb.group({
    id: [],
    company_id: [],
    name: ['', Validators.required],
    last_name: ['', Validators.required],
    email: ['', { validators: [Validators.required, Validators.pattern(Patterns.EMAIL)] }],
    username: [],
    phone_number: [],
    role: ['', Validators.required],
    password: ['', Validators.required],
    confirm_password: ['', Validators.required]
  });

  roles = [];

  constructor(public dialogRef: MatDialogRef<StoreFormComponent>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
              protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
              @Inject('API_URL') public apiUrl: string, @Inject('GRAPHQL_URL') public graphQLUrl: string, protected _soundService: SoundService,
              protected _translate: TranslateService, protected _auth: AuthService) {
      super(dialogRef, data, _fb, claimService, _http, _modalService, _dialogService, _snackBar, aws, apiUrl, graphQLUrl, _soundService, _translate, _auth);
  }

  getMessage(): string {
    let msj = '';

    if (!this.claimService.hasPermission('stores.update') && this.state.isEditing) {
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

  protected processSubmit() {

    const requestValues = JSON.parse(JSON.stringify(this.modelForm.value));

    const body: any = {
      company_id: this.state.isEditing ? requestValues.company_id : this._auth.oidcUser.company_id,
      name: requestValues.name,
      last_name: requestValues.last_name,
      email: requestValues.email,
      username: requestValues.email,
      role: requestValues.role
    };

    if (hasValue(requestValues.phone_number)) {
      body.phone_number = requestValues.phone_number;
    }

    if (hasValue(requestValues.password) && hasValue(requestValues.confirm_password)) {
      body.password = requestValues.password;
      body.confirm_password = requestValues.confirm_password;
    }

    if (this.state.isEditing) {
      body.id = requestValues.id;
    }

    this._http[this.state.isEditing ? 'put' : 'post'](this.apiUrl + 'User' + (this.state.isEditing ? `/${this.data.instance[this.PKField]}/` : '/'), body)
      .pipe(take(1))
      .subscribe((response: any) => {
        this.finishForm();
      }, error => {
        this.state.loading = false;
        this.state.isSaving = false;
      });
  }

  init() {

    this.subscriptions.add(

      this._http.post(this.graphQLUrl,
        {
          query: `query {
                    application_roles_list {
                      id, name
                    }
                  }`
        })
        .pipe(take(1))
        .subscribe((responseCities: any) => {

          this.roles = responseCities.data.application_roles_list;

          if (hasValue(this.data?.instance)) {

            this.state.isEditing = true;
            this.PKValue = this.data.instance[this.PKField];

            this._http.get(this.apiUrl + 'User/' + this.data.instance[this.PKField])
              .pipe(take(1))
              .subscribe((res: any) => {

                this.modelForm.patchValue(res);

                this.modelForm.get('password').clearValidators();
                this.modelForm.get('password').updateValueAndValidity();

                this.modelForm.get('confirm_password').clearValidators();
                this.modelForm.get('confirm_password').updateValueAndValidity();

              }, () => { }, () => {
                this.state.loading = false;
              });

          } else {
            this.state.loading = false;
          }

        }, () => { }, () => { })

    );

  }

}
