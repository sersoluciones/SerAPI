import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';
import { OpenIdClient } from '@sersol/ngx';

export function getAPIUrl(): string {
    return (window as any).api_url;
}

export function getGraphQLUrl(): string {
    return (window as any).graphql_url;
}

export function getClientId(): OpenIdClient {
    return {
        id: (window as any).openid_client_id,
        third_id: (window as any).openid_client_third_id,
        credential_id: (window as any).openid_client_credential_id,
        secret: (window as any).openid_client_secret,
        scopes: (window as any).openid_scopes
    };
}

const providers = [
    { provide: 'API_URL', useFactory: getAPIUrl, deps: [] },
    { provide: 'GRAPHQL_URL', useFactory: getGraphQLUrl, deps: [] },
    { provide: 'OPEN_ID_CLIENT', useFactory: getClientId, deps: [] }
];

if (environment.production) {
    enableProdMode();
}

platformBrowserDynamic(providers).bootstrapModule(AppModule)
    .catch(err => console.error(err));
