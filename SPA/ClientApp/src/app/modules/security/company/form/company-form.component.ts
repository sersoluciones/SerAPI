import { Component, Inject, ViewChild, ElementRef, Renderer2, HostListener } from '@angular/core';
import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { Validators, FormBuilder } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { CategoryFormComponent } from 'src/app/modules/inventory/category/form/category-form.component';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { ClaimsService, AwsService, hasValue } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { take } from 'rxjs/operators';
import { SelectImageComponent } from 'src/app/common/select-image/select-image.component';
import { NgxFileDropEntry, FileSystemFileEntry, FileSystemDirectoryEntry } from 'ngx-file-drop';

@Component({
  selector: 'app-company-form',
  templateUrl: './company-form.component.html',
  styleUrls: ['./company-form.component.scss']
})
export class CompanyFormComponent extends BaseFormDialog {

  modelForm = this._fb.group({
    id: [],
    name: ['', Validators.required],
    last_name: [],
    document_type_id: ['', Validators.required],
    document_number: ['', Validators.required],
    companies: this._fb.group({
      id: [],
      email: [],
      type_company_id: [],
      logo: [],
      photos: [],
      phone_number: [],
      contact_name: [],
      city_id: []
    })
  });

  optionsDocumentType = [];
  optionsTypeCompany = [];
  optionsCity = [];

  files: NgxFileDropEntry[] = [];
  keyUploadFile = 'file';

  @ViewChild('photoBackground') photoBackground: ElementRef;
  @ViewChild('dropzone') dropZone: ElementRef;

  constructor(public dialogRef: MatDialogRef<CategoryFormComponent>, @Inject(MAT_DIALOG_DATA) public data: BaseFormData, protected _fb: FormBuilder, public claimService: ClaimsService,
              protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService, public aws: AwsService,
              @Inject('API_URL') public apiUrl: string, @Inject('GRAPHQL_URL') public graphQLUrl: string, protected _soundService: SoundService,
              protected _translate: TranslateService, protected _auth: AuthService, private renderer: Renderer2) {
    super(dialogRef, data, _fb, claimService, _http, _modalService, _dialogService, _snackBar, aws, apiUrl, graphQLUrl, _soundService, _translate, _auth);
  }

  getMessage(): string {
    let msj = '';

    if (!this.claimService.hasPermission('companies.update') && this.state.isEditing) {
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

  /**
   * @description Detecta cuando se arrastra un documento en la ventana
   */
  @HostListener('dragenter') onDragenter() {
    if (!this.state.loading || !this.state.isSaving) {
      this.showDropZone();
    }
  }

  /**
   * @description Preview de la foto de perfil
   */
  viewFile(event: any) {
    this.openDialog(event.target.files[0]);
  }

  /**
   * @description Abre el dialog para seleccionar la imagen
   * @param file - Imagen para recortar
   */
  openDialog(file: File): void {

    const dialogRef = this._modalService.open(SelectImageComponent, {
      width: '550px',
      data: { round: true, file }
    });

    dialogRef.afterClosed().subscribe(image => {
      if (image != null) {
        // this.modelFiles[this.keyUploadFile] = image;
        this.renderer.setStyle(this.photoBackground.nativeElement, 'background-image', 'url(' + image + ')');

        // console.log(this.modelFiles);
      }
    });
  }

  // Funciones para seleccionar la imagen

  showDropZone() {
    this.renderer.setStyle(this.dropZone.nativeElement, 'display', 'block');
  }

  /**
   * @description Función para cuando se arrastra o se selecciona imagenes
   * @param file - Archivo que se arrastro
   */
  dropped(files: NgxFileDropEntry[]) {

    this.files = files;

    for (const droppedFile of files) {

      if (droppedFile.fileEntry.isFile) {

        const fileEntry = droppedFile.fileEntry as FileSystemFileEntry;

        fileEntry.file((file: File) => {
          this.renderer.setStyle(this.dropZone.nativeElement, 'display', 'none');
          this.openDialog(file);
        });

      } else {
        const fileEntry = droppedFile.fileEntry as FileSystemDirectoryEntry;
        console.log(droppedFile.relativePath, fileEntry);
      }
    }
  }

  fileOver(event) {
    console.log(event);
  }

  fileLeave(event) {
    console.log(event);
  }

  private getGraphParams(): string[] {
    const graph_params = [];
    let counter = 0;

    if (this.state.isEditing) {
      graph_params.push(`($id: ID!, $baseIdentity:baseidentity_input!) { a${++counter}: update_baseidentity(id: $id, baseidentity: $baseIdentity) { id }`);
    } else {
      graph_params.push(`($baseIdentity:baseidentity_input!) { a${++counter}: create_baseidentity(baseidentity: $baseIdentity) { id }`);
    }

    return graph_params;
  }

  protected processSubmit() {
    const requestValues = JSON.parse(JSON.stringify(this.modelForm.value));

    const variables: any = {
      baseIdentity: {
        name: requestValues.name,
        last_name: requestValues.last_name,
        document_type_id: requestValues.document_type_id,
        document_number: requestValues.document_number,
        companies: [
          {
            email: requestValues.companies.email,
            type_company_id: requestValues.companies.type_company_id,
            phone_number: requestValues.companies.phone_number,
            contact_name: requestValues.companies.contact_name,
            city_id: requestValues.companies.city_id
          }
        ]
      }
    };

    if (this.state.isEditing) {
      variables.id = requestValues.id;
      variables.baseIdentity.companies[0].id = requestValues.companies.id;
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
    this.subscriptions.add(
      this._http.post(this.graphQLUrl,
        {
          query: `query {
                    cities_list {
                      id, name
                    }
                    document_type: common_options_list(type: "DT") {
                      id, value
                    }
                    type_company: common_options_list(type: "TC") {
                      id, value
                    }
                  }`
        })
        .pipe(take(1))
        .subscribe((responseCities: any) => {

          this.optionsCity = responseCities.data.cities_list;
          this.optionsDocumentType = responseCities.data.document_type;
          this.optionsTypeCompany = responseCities.data.type_company;

          if (hasValue(this.data?.instance)) {
            this.state.isEditing = true;
            this.PKValue = this.data.instance[this.PKField];

            this._http.post(this.graphQLUrl,
              {
                query: `query {
                          company(id: ${this.PKValue}) {
                            id, base_identity_id, email, type_company_id, logo, photos, phone_number, contact_name, city_id
                          }
                          base_identity(id: ${this.data?.instance.base_identity_id}) {
                            id, name, last_name, document_type_id, document_number
                          }
                        }`
              })
              .pipe(take(1))
              .subscribe((res: any) => {

                this.modelForm.patchValue(res.data.base_identity);
                this.modelForm.controls.companies.patchValue(res.data.company);

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
