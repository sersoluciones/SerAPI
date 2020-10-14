import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';
export function getAPIUrl() {
    return window.api_url;
}
export function getClientId() {
    return {
        id: window.openid_client_id,
        third_id: window.openid_client_third_id,
        credential_id: window.openid_client_credential_id,
        secret: window.openid_client_secret,
        scopes: window.openid_scopes
    };
}
const providers = [
    { provide: 'API_URL', useFactory: getAPIUrl, deps: [] },
    { provide: 'OPEN_ID_CLIENT', useFactory: getClientId, deps: [] }
];
if (environment.production) {
    enableProdMode();
}
platformBrowserDynamic(providers).bootstrapModule(AppModule)
    .catch(err => console.error(err));
//# sourceMappingURL=main.js.map