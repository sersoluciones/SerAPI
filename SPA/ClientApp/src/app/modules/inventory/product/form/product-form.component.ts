import { Component, Inject, ViewChild } from '@angular/core';
import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { Validators, FormBuilder, FormArray } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { CategoryFormComponent } from '../../category/form/category-form.component';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { ClaimsService, hasValue, AwsService } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { take } from 'rxjs/operators';
import { MatCheckboxChange } from '@angular/material/checkbox';
import * as ClassicEditor from '@sersol/ckeditor5-build-classic';
import { CKEditorComponent } from '@ckeditor/ckeditor5-angular';

@Component({
  templateUrl: './product-form.component.html',
  styleUrls: ['./product-form.component.scss']
})
export class ProductFormComponent extends BaseFormDialog {

    modelForm = this._fb.group({
        id: [],
        is_active: [true],
        company_id: [],
        name: ['', Validators.required],
        description: [],
        short_description: [],
        sku: [],
        ean: [],
        price: [null, [Validators.required, Validators.pattern('[0-9]*')]],
        current_quantity: [null, Validators.required],
        is_bargain: [null],
        discount_price: [null, Validators.required],
        technical_specifications: this._fb.array([]),
        is_new_product: [null],
        brand_id: [null],
        sub_category_id: [null, Validators.required],
        iva: [null]
    });

    modelFiles = [];

    descActiveTab = 0;

    @ViewChild('editor', {static: false})
    editorComponent: CKEditorComponent;
    ckEditor = ClassicEditor;
    ckEditorConfig = {
        height: '400px'
    };

    constructor(public dialogRef: MatDialogRef<CategoryFormComponent>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
                protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
                @Inject('API_URL') public apiUrl: string, @Inject('GRAPHQL_URL') public graphQLUrl: string, protected _soundService: SoundService,
                protected _translate: TranslateService, protected _auth: AuthService) {
        super(dialogRef, data, _fb, claimService, _http, _modalService, _dialogService, _snackBar, aws, apiUrl, graphQLUrl, _soundService, _translate, _auth);
    }

    getMessage(): string {
        let msj = '';

        if (!this.claimService.hasPermission('products.update') && this.state.isEditing) {
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

    setDescTab(tab: number) {
        this.descActiveTab = tab;
    }

    onDiscount(ev: MatCheckboxChange) {
        if (ev.checked) {
            this.modelForm.get('discount_price').enable();
        } else {
            this.modelForm.get('discount_price').disable();
        }
    }

    addTech() {

    }

    private getGraphParams(): string[] {
        const graph_params = [];
        let counter = 0;

        if (this.state.isEditing) {
            graph_params.push(`($id: ID!, $category:category_input!) { a${++counter}: update_category(id: $id, category: $category) { id }`);
        } else {
            graph_params.push(`($category:category_input!) { a${++counter}: create_category(category: $category) { id }`);
        }

        return graph_params;
    }

    protected processSubmit() {
        const requestValues = JSON.parse(JSON.stringify(this.modelForm.value));
        console.log(requestValues);

        const variables: any = {
            category: {
                company_id: this.state.isEditing ? requestValues.company_id : this._auth.oidcUser.company_id,
                name: requestValues.name,
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
                            product(id: ${this.PKValue}) {
                                id, company_id, name, short_description, description, technical_specifications,
                                it_includes, sku, ean, price, discount_price, type, iva, attachment {key},
                                unit_measured, current_quantity, brand_id, is_new_product, is_bargain, sub_category_id
                            }
                        }`
                    })
                    .pipe(take(1))
                    .subscribe((res: any) => {

                        console.log(res.data.product.technical_specifications);

                        this.modelForm.patchValue({
                            id: res.data.product.id,
                            company_id: res.data.product.company_id,
                            is_new_product: res.data.product.is_new_product,
                            name: res.data.product.name,
                            short_description: res.data.product.short_description,
                            description: res.data.product.description,
                            sku: res.data.product.sku,
                            ean: res.data.product.ean,
                            price: res.data.product.price,
                            is_bargain: res.data.product.is_bargain,
                            discount_price: res.data.product.discount_price,
                            type: res.data.product.type,
                            iva: res.data.product.iva,
                            unit_measured: res.data.product.unit_measured,
                            current_quantity: res.data.product.current_quantity,
                            brand_id: res.data.product.brand_id,
                            sub_category_id: res.data.product.sub_category_id
                        });

                        if (!res.data.product.is_garbain) {
                            this.modelForm.get('discount_price').disable();
                        }

                        /* const photos = JSON.parse(res.data.product.photos);
                        for (const key in photos) {
                            if (photos.hasOwnProperty(key)) {
                                this.modelFiles.push(photos[key].split('||')[0]);
                            }
                        } */

                    }, () => {}, () => {
                        this.state.loading = false;
                    })
            );
        } else {
            this.modelForm.get('discount_price').disable();
            this.state.loading = false;
        }
    }

}
