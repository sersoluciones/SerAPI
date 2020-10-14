import { __awaiter, __decorate, __param } from "tslib";
import { Component, Inject, ViewChild, HostListener } from '@angular/core';
import { Validators } from '@angular/forms';
import { hasValue, Patterns, CustomValidators } from '@sersol/ngx';
import { HttpParams, HttpHeaders } from '@angular/common/http';
import { SelectImageComponent } from '../select-image/select-image.component';
import { DialogErrorComponent } from '../dialog-error/dialog-error.component';
let SignUpComponent = class SignUpComponent {
    constructor(loginService, formBuilder, aws, _http, _ngZone, dialog, renderer, openIdClient, cdRef, translate) {
        this.loginService = loginService;
        this.formBuilder = formBuilder;
        this.aws = aws;
        this._http = _http;
        this._ngZone = _ngZone;
        this.dialog = dialog;
        this.renderer = renderer;
        this.openIdClient = openIdClient;
        this.cdRef = cdRef;
        this.translate = translate;
        this._step = 1;
        this.subscriptions = [];
        this.complete = false;
        this.loader = false;
        this.auth_type = 'email';
        this.error = {};
        this.photoSocialRed = '';
        this.files = [];
        this.logoOptions = {
            path: this.aws.getS3Url('assets/lottie/confirmation.json'),
            renderer: 'canvas',
            loop: 0
        };
        this.RegisterForm = this.formBuilder.group({
            password: [null, [Validators.required, Validators.pattern(Patterns.PASSWORD)]],
            confirm_password: [null, [Validators.required]],
            email: ['', {
                    validators: [Validators.required, Validators.pattern(Patterns.EMAIL)],
                    // asyncValidators: [this.uniqueEmail.validate.bind(this.uniqueEmail)],
                    updateOn: 'blur'
                }
            ],
            name: ['', [Validators.required, Validators.maxLength(100)]],
            last_name: ['', [Validators.required, Validators.maxLength(100)]],
            role: ['admin'],
            company_name: ['', [Validators.required, Validators.maxLength(100)]],
            type_company_id: ['', [Validators.required]],
            phone: [''],
            token: [''],
            third_type: ['']
        }, {
            validators: CustomValidators.match('password', 'confirm_password')
        });
    }
    get step() {
        return this._step;
    }
    set step(value) {
        if (this._step !== 4 && !this.loader) {
            this._step = value;
            this.checkStep();
            this.cdRef.detectChanges();
        }
    }
    /**
     * @description Detecta cuando se arrastra un documento en la ventana
     */
    onDragenter() {
        if (!this.loader) {
            this.showDropZone();
        }
    }
    /**
     * @description Verifica paso
     */
    checkStep() {
        switch (this.step) {
            case 1:
                this.RegisterForm.get('token').reset();
                this.RegisterForm.get('email').reset();
                this.RegisterForm.get('name').reset();
                this.RegisterForm.get('last_name').reset();
                this.RegisterForm.get('third_type').reset();
                break;
            case 2:
                this.photoProfile = null;
                this.RegisterForm.get('name').reset();
                this.RegisterForm.get('last_name').reset();
                this.RegisterForm.get('phone').reset();
                break;
            case 4:
                break;
            default:
                break;
        }
    }
    /**
     * @description Adelanta un paso en el wizard
     */
    nextStep() {
        if (this.step < 3) {
            this.step++;
        }
    }
    /**
     * @description Devuelve un paso en el wizard
     */
    prevStep() {
        if (this.step > 1) {
            this.step--;
        }
    }
    /**
     * @description Agrega o elimina el tipo de negocio
     * @param id - Id del tipo de negocio
     */
    setTypeCompany(id) {
        this.RegisterForm.get('type_company_id').setValue(id);
    }
    /**
     * @description Oculta/muestra contraseña
     */
    toggleVisibility(ev) {
        if (ev.target.innerText === 'visibility_off') {
            ev.target.parentElement.parentElement.children[0].type = 'text';
            ev.target.innerText = 'visibility';
        }
        else {
            ev.target.parentElement.parentElement.children[0].type = 'password';
            ev.target.innerText = 'visibility_off';
        }
    }
    setSocialProfile() {
        if (hasValue(this.loginService.googleProfile)) {
            this.auth_type = 'google';
            this.RegisterForm.get('token').setValue(this.loginService.googleProfile.id_token);
        }
        else if (hasValue(this.loginService.facebookProfile)) {
            this.auth_type = 'facebook';
            this.RegisterForm.get('token').setValue(this.loginService.facebookProfile.access_token);
        }
        else if (hasValue(this.loginService.microsoftProfile)) {
            this.auth_type = 'microsoft';
            this.RegisterForm.get('token').setValue(this.loginService.microsoftProfile.id_token);
        }
        this.RegisterForm.get('email').setValue(this.loginService[this.auth_type + 'Profile'].email);
        this.RegisterForm.get('name').setValue(this.loginService[this.auth_type + 'Profile'].first_name);
        this.RegisterForm.get('last_name').setValue(this.loginService[this.auth_type + 'Profile'].last_name);
        this.RegisterForm.get('third_type').setValue(this.auth_type);
        if (hasValue(this.loginService[this.auth_type + 'Profile'].picture)) {
            this.photoSocialRed = this.loginService[this.auth_type + 'Profile'].picture;
            this.getBase64ImageFromUrl(this.photoSocialRed)
                .then(result => {
                this.photoProfile = result;
                this.renderer.setStyle(this.photoBackground.nativeElement, 'background-image', 'url(' + result + ')');
            })
                .catch(err => console.log(err));
        }
        this.step = 3;
    }
    /**
     * @description De una url se convierte en un Base64
     * @param imageUrl - Url para de la imagen
     */
    getBase64ImageFromUrl(imageUrl) {
        return __awaiter(this, void 0, void 0, function* () {
            const res = yield fetch(imageUrl);
            const blob = yield res.blob();
            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.addEventListener('load', () => {
                    resolve(reader.result);
                }, false);
                reader.onerror = () => {
                    return reject(this);
                };
                reader.readAsDataURL(blob);
            });
        });
    }
    /**
     * @description Abre el dialog para seleccionar la imagen
     * @param file - Imagen para recortar
     */
    openDialog(file) {
        const dialogRef = this.dialog.open(SelectImageComponent, {
            width: '550px',
            data: { round: true, file }
        });
        dialogRef.afterClosed().subscribe(image => {
            if (image != null) {
                this.photoProfile = image;
                this.renderer.setStyle(this.photoBackground.nativeElement, 'background-image', 'url(' + image + ')');
            }
        });
    }
    /**
     * @description Preview de la foto de perfil
     */
    viewFile(event) {
        this.openDialog(event.target.files[0]);
    }
    // Funciones para seleccionar la imagen
    showDropZone() {
        this.renderer.setStyle(this.dropZone.nativeElement, 'display', 'block');
    }
    /**
     * @description Función para cuando se arrastra o se selecciona imagenes
     * @param file - Archivo que se arrastro
     */
    dropped(files) {
        this.files = files;
        for (const droppedFile of files) {
            if (droppedFile.fileEntry.isFile) {
                const fileEntry = droppedFile.fileEntry;
                fileEntry.file((file) => {
                    this.renderer.setStyle(this.dropZone.nativeElement, 'display', 'none');
                    this.openDialog(file);
                });
            }
            else {
                const fileEntry = droppedFile.fileEntry;
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
    /**
     * @description Crea la animacion de Lottie
     */
    animationCreated(animationItem) {
        console.log('Crea la animacion');
        console.log(animationItem);
    }
    /**
     * convierte un base64 string a un Blob decuerdo a su contenido.
     * @param b64Data - base64 string con contentType
     * @param contentType - El content type del archivo i.e (image/jpeg - image/png - text/plain)
     * @param sliceSize - Tamaño del archivo
     * @return Blob
     */
    b64toBlob(b64Data, contentType, sliceSize) {
        contentType = contentType || '';
        sliceSize = sliceSize || 512;
        const byteCharacters = atob(b64Data);
        const byteArrays = [];
        for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
            const slice = byteCharacters.slice(offset, offset + sliceSize);
            const byteNumbers = new Array(slice.length);
            for (let i = 0; i < slice.length; i++) {
                byteNumbers[i] = slice.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            byteArrays.push(byteArray);
        }
        const blob = new Blob(byteArrays, { type: contentType });
        return blob;
    }
    submit() {
        this.loader = true;
        const token_params = new HttpParams().set('grant_type', 'client_credentials')
            .set('grant_type', 'client_credentials');
        this._http.post('/connect/token', token_params, {
            headers: {
                Authorization: 'Basic ' + this.openIdClient.credential_id
            }
        }).subscribe((token_response) => {
            const signUpData = this.RegisterForm.getRawValue();
            signUpData.type_company_id = 1;
            const headers = new HttpHeaders().set('Authorization', 'Bearer ' + token_response.access_token)
                .set('Content-type', 'application/json');
            this._http.post('/api/User/Register', signUpData, { headers }).subscribe((response) => {
                if (hasValue(this.photoProfile)) {
                    const ImageURL = this.photoProfile;
                    const block = ImageURL.split(';');
                    const contentType = block[0].split(':')[1];
                    const realData = block[1].split(',')[1];
                    const blob = this.b64toBlob(realData, contentType, null);
                    const formData = new FormData();
                    formData.append('file', blob);
                    const headerPhotoRequest = new HttpHeaders().set('Authorization', 'Bearer ' + token_response.access_token);
                    this._http.put('/api/User/UpdatePhoto/' + response.id + '/', formData, { headers: headerPhotoRequest, responseType: 'blob' })
                        .subscribe(data => {
                        this.loader = false;
                        this.step = 4;
                    });
                }
                else {
                    this.loader = false;
                    this.step = 4;
                }
            }, (reject) => {
                this.loader = false;
                this.status = 'error';
                this.error_register = reject.statusText;
                if (hasValue(reject.error.Error)) {
                    if (reject.error.Error[0] === '4') {
                        this.translate.get('error_4').subscribe((text) => {
                            this.dialog.open(DialogErrorComponent, {
                                width: '550px',
                                data: { error: text }
                            });
                        });
                    }
                }
            });
        }, (error) => {
            this.loader = false;
            this.status = 'error';
            console.warn(error);
        });
    }
    ngOnInit() {
        this.subscriptions.push(this.loginService.loader.subscribe((data) => {
            this.loader = data;
        }));
        this.subscriptions.push(this.loginService.login.subscribe((data) => {
            this._ngZone.run(() => {
                this.loader = false;
                this.setSocialProfile();
            });
        }));
    }
    ngAfterViewInit() {
        setTimeout(() => {
            this.companyNameElem.nativeElement.focus();
        }, 2000);
    }
    ngOnDestroy() {
        this.subscriptions.forEach(element => {
            element.unsubscribe();
        });
    }
};
__decorate([
    ViewChild('photoBackground')
], SignUpComponent.prototype, "photoBackground", void 0);
__decorate([
    ViewChild('dropzone')
], SignUpComponent.prototype, "dropZone", void 0);
__decorate([
    ViewChild('company_name')
], SignUpComponent.prototype, "companyNameElem", void 0);
__decorate([
    HostListener('dragenter')
], SignUpComponent.prototype, "onDragenter", null);
SignUpComponent = __decorate([
    Component({
        selector: 'app-sign-up',
        templateUrl: './sign-up.component.html',
        styleUrls: ['./sign-up.component.scss']
    }),
    __param(7, Inject('OPEN_ID_CLIENT'))
], SignUpComponent);
export { SignUpComponent };
//# sourceMappingURL=sign-up.component.js.map