import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule, ErrorHandler } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

// SER ngx
import { NG_GAPI_CONFIG, GoogleSDKModule, NG_FSDK_CONFIG, FacebookSDKModule, AwsModule, AWS_CONFIG,
    OPEN_ID_CONFIG, ClaimsModule, SerFormModule, SerUiModule } from '@sersol/ngx';

// Material
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

// Translate
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { AppComponent } from './app.component';

// Third Parties
import { NgxCurrencyModule } from 'ngx-currency';
import { CKEditorModule } from '@ckeditor/ckeditor5-angular';
import { ImageCropperModule } from 'ngx-image-cropper';
import { NgxFileDropModule } from 'ngx-file-drop';

// Social Logins
import { MsalModule } from '@azure/msal-angular';

//#region Project imports
import { AppRoutingModule } from './app-routing.module';
import { OidcUser } from './common/auth/oidc';
import { SidebarComponent } from './ui/sidebar/sidebar.component';
import { TopbarComponent } from './ui/topbar/topbar.component';
import { ProfileComponent } from './ui/profile/profile.component';
import { ProfileFormComponent } from './ui/profile/profile-form/profile-form.component';
import { DialogSimpleComponent } from './common/dialog/simple/simple.component';
import { ChangePasswordComponent } from './ui/profile/change-password/change-password.component';
import { DialogErrorComponent } from './common/dialog/error/error.component';
import { BackdropComponent } from './ui/backdrop/backdrop.component';
import { AuthInterceptor } from './common/interceptors/auth.interceptor';
import { PermissionComponent } from './modules/security/permission/permission.component';
import { CrudToolbarComponent } from './ui/crud/crud-toolbar/crud-toolbar.component';
import { CrudAddObjectComponent } from './ui/crud/crud-add-object/crud-add-object.component';
import { CrudEditToolsComponent } from './ui/crud/crud-edit-tools/crud-edit-tools.component';
import { CrudFilterToolsComponent } from './ui/crud/crud-filter-tools/crud-filter-tools.component';
import { DialogCrudDeleteComponent } from './ui/crud/dialog-crud-delete/dialog-crud-delete.component';
import { ProductFormComponent } from './modules/inventory/product/form/product-form.component';
import { ProductCrudComponent } from './modules/inventory/product/list/product-crud.component';
import { DialogErrorBulkComponent } from './common/dialog/dialog-error-bulk/error-bulk.component';
import { CountryComponent } from './modules/parameterization/country/country.component';
import { CommonOptionComponent } from './modules/parameterization/common-option/common-option.component';
import { CategoryListComponent } from './modules/inventory/category/list/category.component';
import { CategoryFormComponent } from './modules/inventory/category/form/category-form.component';
import { ErrorPermissionComponent } from './ui/403/error-permission/error-permission.component';
import { WindowControlsComponent } from './ui/dialog/window-controls/window-controls.component';
import { DialogUnsavedFormComponent } from './common/dialog/unsaved-form/unsaved-form.component';
import { RoleComponent } from './modules/security/role/list/role.component';
import { RoleFormComponent } from './modules/security/role/form/role-form.component';
import { BrandComponent } from './modules/inventory/brand/brand.component';
import { FilterArray } from './modules/security/role/form/filter-array.pipe';
import { SentryErrorHandler } from './common/error/sentry';
import { customCurrencyMaskConfig } from './common/config/currency';
import { StoreListComponent } from './modules/inventory/store/list/store.component';
import { StoreFormComponent } from './modules/inventory/store/form/store-form.component';
import { UserComponent } from './modules/security/user/list/user.component';
import { UserFormComponent } from './modules/security/user/form/user-form.component';
import { CompanyComponent } from './modules/security/company/list/company.component';
import { CompanyFormComponent } from './modules/security/company/form/company-form.component';
import { SelectImageComponent } from './common/select-image/select-image.component';
//#endregion

const isIE = window.navigator.userAgent.indexOf('MSIE ') > -1 || window.navigator.userAgent.indexOf('Trident/') > -1;
const oidc_user = JSON.parse(localStorage.getItem('oidc_user')) as OidcUser;
const providers: any = [
    {
        provide: HTTP_INTERCEPTORS,
        useClass: AuthInterceptor,
        multi: true
    }
];

if (!(window as any).debug) {
    console.log('entra');
    providers.push({
        provide: ErrorHandler,
        useClass: SentryErrorHandler
    });
}


@NgModule({
    imports: [
        BrowserModule,
        AppRoutingModule,
        BrowserAnimationsModule,
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        HttpClientModule,
        SerFormModule,
        SerUiModule,
        MatIconModule,
        MatTabsModule,
        MatCheckboxModule,
        MatSlideToggleModule,
        MatDialogModule,
        MatSnackBarModule,
        MatMenuModule,
        CKEditorModule,
        NgxCurrencyModule.forRoot(customCurrencyMaskConfig),
        TranslateModule.forRoot({
            defaultLanguage: 'es',
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient]
            }
        }),
        ClaimsModule.forRoot({
            provide: OPEN_ID_CONFIG,
            useValue: {
                claims: oidc_user?.claims,
                isSuperUser: oidc_user?.is_super_user
            }
        }),
        AwsModule.forRoot({
            provide: AWS_CONFIG,
            useValue: {
                s3: {
                  bucket: (window as any).aws_s3_bucket,
                }
            }
        }),
        GoogleSDKModule.forRoot({
            provide: NG_GAPI_CONFIG,
            useValue: {
                client_id: (window as any).google_client_id,
                scope: 'profile email'
            }
        }),
        FacebookSDKModule.forRoot({
            provide: NG_FSDK_CONFIG,
            useValue: {
                appId: (window as any).facebook_client_id,
                cookie: true,
                xfbml: true,
                version: 'v6.0'
            }
        }),
        MsalModule.forRoot({
            clientID: (window as any).microsoft_client_id,
            redirectUri: window.location.origin,
            storeAuthStateInCookie: isIE,
            popUp: !isIE,
            consentScopes: [
                'user.read',
                'openid',
                'profile',
            ]
        }),
        ImageCropperModule,
        NgxFileDropModule
    ],
    declarations: [
        AppComponent,
        SidebarComponent,
        TopbarComponent,
        DialogSimpleComponent,
        DialogErrorComponent,
        ProfileComponent,
        ProfileFormComponent,
        BackdropComponent,
        ChangePasswordComponent,
        PermissionComponent,
        CrudToolbarComponent,
        CrudAddObjectComponent,
        CrudEditToolsComponent,
        CrudFilterToolsComponent,
        DialogCrudDeleteComponent,
        ProductCrudComponent,
        DialogErrorBulkComponent,
        CountryComponent,
        CommonOptionComponent,
        RoleComponent,
        CategoryListComponent,
        CategoryFormComponent,
        ErrorPermissionComponent,
        WindowControlsComponent,
        DialogUnsavedFormComponent,
        ErrorPermissionComponent,
        RoleFormComponent,
        BrandComponent,
        FilterArray,
        ProductFormComponent,
        StoreListComponent,
        StoreFormComponent,
        UserComponent,
        UserFormComponent,
        CompanyComponent,
        CompanyFormComponent,
        SelectImageComponent
    ],
    schemas: [],
    providers,
    bootstrap: [AppComponent]
})
export class AppModule { }

// required for AOT compilation
export function HttpLoaderFactory(http: HttpClient) {
    return new TranslateHttpLoader(http, 'assets/i18n/');
}
