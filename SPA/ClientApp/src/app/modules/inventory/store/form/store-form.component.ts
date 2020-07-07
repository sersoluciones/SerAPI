import { Component, Inject } from '@angular/core';
import { BaseFormDialog } from 'src/app/common/base/form-dialog';
import { Validators, FormBuilder } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { BaseFormData } from 'src/app/common/interfaces/base';
import { ClaimsService, hasValue, AwsService } from '@sersol/ngx';
import { HttpClient } from '@angular/common/http';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { SnackbarService } from 'src/app/common/snackbar/snackbar.service';
import { SoundService } from 'src/app/common/sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/common/auth/auth.service';
import { take } from 'rxjs/operators';
import * as L from 'leaflet';

@Component({
  templateUrl: './store-form.component.html',
  styleUrls: ['./store-form.component.scss']
})
export class StoreFormComponent extends BaseFormDialog {

  modelForm = this._fb.group({
    id: [],
    company_id: [],
    name: ['', Validators.required],
    code: [],
    address: ['', Validators.required],
    city_id: ['', Validators.required],
    latitude: [],
    longitude: []
  });

  cities = [];

  private map: any;
  private marker: any;

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
      address: requestValues.address,
      city_id: requestValues.city_id
    };

    if (hasValue(requestValues.code)) {
      body.code = requestValues.code;
    }
    if (hasValue(requestValues.latitude) && hasValue(requestValues.longitude)) {
      body.info_gps = {
        lat: requestValues.latitude,
        lon: requestValues.longitude
      };
    }

    if (this.state.isEditing) {
      body.id = requestValues.id;
    }

    this._http[this.state.isEditing ? 'put' : 'post'](this.apiUrl + 'Store' + (this.state.isEditing ? `/${this.data.instance[this.PKField]}/` : '/'), body)
      .pipe(take(1))
      .subscribe((response: any) => {
        this.finishForm();
      }, error => {
        this.state.loading = false;
        this.state.isSaving = false;
      });
  }

  /**
   * Inicializa el mapa, agregando el marcador y habilitando el drag
   * Cuando termina de arrastrar el marcador, los input de latitud y longitud se setean
   * Es necesario inicializar en el afterInit para que el mapa se muestre
   */
  private _initMap(): void {

    this.map = L.map('map').setView([4.6097100, -74.0817500], 5);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(this.map);

    this.marker = L.marker([4.6097100, -74.0817500], { draggable: 'true' }).addTo(this.map);

    this.marker.on('dragend', (event) => {
      const position = event.target.getLatLng();
      this.modelForm.get('latitude').setValue(position.lat.toFixed(5));
      this.modelForm.get('longitude').setValue(position.lng.toFixed(5));
    });

  }

  /**
   * Cuando se cambia en el input de latitud y longitud, cambia la posicion del marcador y hace el zoom
   */
  changeCoordinates() {
    if (hasValue(this.modelForm.get('latitude').value) && hasValue(this.modelForm.get('longitude').value)) {
      this.marker.setLatLng([this.modelForm.get('latitude').value, this.modelForm.get('longitude').value]);
      this.map.flyTo([this.modelForm.get('latitude').value, this.modelForm.get('longitude').value], this.map.getZoom());
    }
  }

  afterInit() {

    this.windowControl.hide.subscribe(() => {
      this.close();
    });

    this._initMap();
  }

  init() {

    this.subscriptions.add(

      this._http.post(this.graphQLUrl,
        {
          query: `query {
                        cities_list {
                            id, name
                        }
                    }`
        })
        .pipe(take(1))
        .subscribe((responseCities: any) => {

          this.cities = responseCities.data.cities_list;

          if (hasValue(this.data?.instance)) {

            this.state.isEditing = true;
            this.PKValue = this.data.instance[this.PKField];

            this._http.get(this.apiUrl + 'Store/' + this.data.instance[this.PKField])
              .pipe(take(1))
              .subscribe((res: any) => {
                res.latitude = res.location_json.coordinates[1];
                res.longitude = res.location_json.coordinates[0];

                this.marker.setLatLng([res.latitude, res.longitude]);

                this.map.flyTo([res.latitude, res.longitude], 18);

                this.modelForm.patchValue(res);

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
