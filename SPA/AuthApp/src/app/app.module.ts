import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule } from '@angular/common/http';

// SER ngx
import { NG_GAPI_CONFIG, GoogleSDKModule, NG_FSDK_CONFIG, FacebookSDKModule, AwsModule, AWS_CONFIG, SerFormModule } from '@sersol/ngx';

// Material
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';

// Translate
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { Routes, RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { SignInComponent } from './sign-in/sign-in.component';
import { SignUpComponent } from './sign-up/sign-up.component';
import { SelectImageComponent } from './select-image/select-image.component';
import { DialogErrorComponent } from './dialog-error/dialog-error.component';

// Social Logins
import { MsalModule } from '@azure/msal-angular';
import { GoogleComponent } from './sign-in/google/google.component';
import { MicrosoftComponent } from './sign-in/microsoft/microsoft.component';
import { FacebookComponent } from './sign-in/facebook/facebook.component';

// Cropper image
import { ImageCropperModule } from 'ngx-image-cropper';
import { NgxFileDropModule } from 'ngx-file-drop';
import { from } from 'rxjs';

// Lottie
import {  LottieModule} from 'ngx-lottie';

const isIE = window.navigator.userAgent.indexOf('MSIE ') > -1 || window.navigator.userAgent.indexOf('Trident/') > -1;

const appRoutes: Routes = [
    { path: 'sign-in', component: SignInComponent },
    { path: 'sign-up', component: SignUpComponent },
    { path: '', redirectTo: '/sign-in', pathMatch: 'full' },
    { path: '**', redirectTo: '/sign-in' }
];

@NgModule({
    imports: [
        BrowserModule,
        BrowserAnimationsModule,
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        HttpClientModule,
        SerFormModule,
        MatIconModule,
        MatTabsModule,
        MatCheckboxModule,
        MatDialogModule,
        TranslateModule.forRoot({
            defaultLanguage: 'es',
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient]
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
        RouterModule.forRoot(
            appRoutes,
            { enableTracing: false } // <-- debugging purposes only
        ),
        ImageCropperModule,
        NgxFileDropModule,
        LottieModule.forRoot({
            player: playerFactory,
            useCache: true
        })
    ],
    declarations: [
        AppComponent,
        SignInComponent,
        SignUpComponent,
        GoogleComponent,
        MicrosoftComponent,
        FacebookComponent,
        SelectImageComponent,
        DialogErrorComponent
    ],
    schemas: [],
    providers: [],
    bootstrap: [AppComponent]
})
export class AppModule { }

// required for AOT compilation
export function HttpLoaderFactory(http: HttpClient) {
    return new TranslateHttpLoader(http, 'assets/i18n/');
}

export function playerFactory() {
    return import('lottie-web');
}
