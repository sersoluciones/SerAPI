import { hasValue, ClaimsService, AwsService, CustomValidators } from '@sersol/ngx';
import { Component, Inject } from '@angular/core';
import { Validators, FormArray, FormBuilder } from '@angular/forms';
import { take } from 'rxjs/operators';
import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { HttpClient } from '@angular/common/http';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';
import { AttachmentService } from 'src/app/common/attachment/attachment.service';

@Component({
    templateUrl: './category-form.component.html',
    styleUrls: ['./category-form.component.scss']
})
export class CategoryFormComponent extends BaseFormDialog {

    modelForm = this._fb.group({
        id: [],
        company_id: [],
        attachment: [null, CustomValidators.maxFileSize('100MB'), CustomValidators.requiredFileType(['png', 'jpg', 'jpeg', 'gif'])],
        name: ['', Validators.required],
        description: [],
        subcategories: this._fb.array([])
    });

    private deletedSubcategories = [];

    constructor(public dialogRef: MatDialogRef<CategoryFormComponent>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
                protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
                @Inject('API_URL') public apiUrl: string, @Inject('GRAPHQL_URL') public graphQLUrl: string, protected _soundService: SoundService,
                protected _translate: TranslateService, protected _auth: AuthService, private attachmentService: AttachmentService) {
        super(dialogRef, data, _fb, claimService, _http, _modalService, _dialogService, _snackBar, aws, apiUrl, graphQLUrl, _soundService, _translate, _auth);
    }

    getMessage(): string {
        let msj = '';

        if (!this.claimService.hasPermission('categories.update') && this.state.isEditing) {
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

    getSubcategories() {
        return (this.modelForm.get('subcategories') as FormArray).controls;
    }

    addSubcategory() {
        (this.modelForm.get('subcategories') as FormArray).push(this._fb.group({
            _new: [true],
            is_active: [true],
            name: ['', Validators.required]
        }));
    }

    private getGraphParams(): string[] {
        const graph_params = [];
        let counter = 0;

        if (this.state.isEditing) {
            graph_params.push(`($id: ID!, $category:category_input!) { a${++counter}: update_category(id: $id, category: $category) { id }`);
        } else {
            graph_params.push(`($category:category_input!) { a${++counter}: create_category(category: $category) { id }`);
        }

        this.deletedSubcategories.forEach(id => {
            graph_params.push(`a${++counter}: delete_subcategory(subcategoryId: ${id}) { id }`);
        });

        return graph_params;
    }

    deleteSubcategory(i: number) {
        const subcategories = (this.modelForm.get('subcategories') as FormArray).at(i);

        if (hasValue(subcategories.value.id)) {
            this.deletedSubcategories.push(subcategories.value.id);
        }

        (this.modelForm.get('subcategories') as FormArray).removeAt(i);
    }

    protected processSubmit() {

        if (hasValue(this.modelForm.get('attachment').value)) {
            this.attachmentService.upload(this.modelForm.get('attachment').value, 'category')
            .pipe(take(1))
            .subscribe(
                (res) => {
                    console.log(res.id);
                    this.modelFilesForm.main = res.id;
                    this.saveModel();
                },
                () => {
                    this.state.isSaving = false;
                }
            );
        } else {
            this.saveModel();
        }
    }

    protected saveModel() {
        const requestValues = JSON.parse(JSON.stringify(this.modelForm.value));
        const variables: any = {
            category: {
                company_id: this.state.isEditing ? requestValues.company_id : this._auth.oidcUser.company_id,
                name: requestValues.name,
                attachment_id: hasValue(this.modelFilesForm.main) ? this.modelFilesForm.main : null,
                sub_categories: (this.modelForm.get('subcategories') as FormArray).value.map(item => {
                    delete item._new;
                    return item;
                })
            }
        };

        if (this.state.isEditing) {
            variables.id = requestValues.id;
        }

        this._http.post(this.graphQLUrl,
            {
                query: `mutation${this.getGraphParams().join(' ')}}`,
                variables
            })
            .pipe(take(1))
            .subscribe((res: any) => {

                if ((window as any).debug) {
                    console.group('%c[debug]', 'color: limegreen;');
                    console.groupCollapsed(this.state.isEditing ? 'update_success' : 'create_success');
                    console.log(res);
                    console.groupEnd();
                    console.groupEnd();
                }

                this.finishForm();

            }, () => {
                this.state.isSaving = false;
            });
    }

    init() {

        if (hasValue(this.data?.instance)) {
            this.state.isEditing = true;
            this.PKValue = this.data.instance[this.PKField];

            this.subscriptions.add(
                this._http.post(this.graphQLUrl,
                    {
                        query: `query {
                            category(id: ${this.PKValue}) {
                                id, name, company_id, attachment_id, attachment { key, key4 }
                            }
                            sub_categories_list(category_id: ${this.PKValue}) {
                                id, is_active, name, category_id
                            }
                        }`
                    })
                    .pipe(take(1))
                    .subscribe((res: any) => {

                        res.data.sub_categories_list.forEach(element => {
                            (this.modelForm.get('subcategories') as FormArray).push(this._fb.group({
                                id: element.id,
                                is_active: [element.is_active],
                                name: [element.name, Validators.required]
                            }));
                        });

                        this.instance = res.data.category;

                        this.modelForm.patchValue({
                            id: res.data.category.id,
                            name: res.data.category.name,
                            company_id: res.data.category.company_id,
                            attachment_id: res.data.category.attachment_id
                        });

                    }, () => {}, () => {
                        this.state.loading = false;
                    })
            );
        } else {
            this.state.loading = false;
        }
    }

}
