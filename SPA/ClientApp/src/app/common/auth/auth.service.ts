import { OidcUser } from 'src/app/common/auth/oidc';
import { CookiesService, hasValue, OpenIdClient } from '@sersol/ngx';
import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { tap, catchError } from 'rxjs/operators';
import { TokenResponse } from './token-response';

@Injectable({
    providedIn: 'root'
})
export class AuthService {

    private _oidcUser: OidcUser;
    public get oidcUser(): OidcUser {
        const oidc = JSON.parse(localStorage.getItem('oidc_user')) as OidcUser;

        if (hasValue(oidc)) {
            oidc.name_initial = hasValue(oidc.name) ? oidc.name[0] : oidc.username[0];
            oidc.name_to_show = hasValue(oidc.name) ? oidc.name : oidc.username;
            this._oidcUser = oidc;

            return this._oidcUser;
        } else {
            return null;
        }
    }
    public set oidcUser(v: OidcUser) {
        this._oidcUser = v;
    }

    constructor(private _cookieService: CookiesService, private http: HttpClient, @Inject('OPEN_ID_CLIENT') private openIdClient: OpenIdClient) { }

    isLoggedIn(): boolean {
        return hasValue(this._cookieService.get('token')) && hasValue(this.oidcUser);
    }

    getJwtToken() {
        return this._cookieService.get('token');
    }

    setJwtToken(tokens: TokenResponse) {
        this._cookieService.set('token', tokens.access_token, tokens.decoded_access_token.exp, '/', undefined, true, 'Strict');
    }

    getRefreshToken() {
        return this._cookieService.get('refresh_token');
    }

    setRefreshToken(tokens: TokenResponse) {
        this._cookieService.set('refresh_token', tokens.refresh_token, tokens.decoded_access_token.exp, '/', undefined, true, 'Strict');
    }

    refreshToken() {
        const client = atob(this.openIdClient.id).split(':');
        const refreshForm = new HttpParams()
            .set('client_id', client[0])
            .set('client_secret', client[1])
            .set('refresh_token', this.getRefreshToken())
            .set('grant_type', 'refresh_token');

        return this.http.post<any>('/connect/token', refreshForm)
            .pipe(
                tap((tokens: TokenResponse) => {
                    tokens.decoded_access_token = JSON.parse(atob(tokens.access_token.split('.')[1]));
                    this.setJwtToken(tokens);
                    this.setRefreshToken(tokens);
                }),
                catchError(err => {
                    this.logout();
                    return err;
                })
            );
    }

    logout() {
        this._cookieService.delete('token', '/');
        this._cookieService.delete('refresh_token', '/');
        localStorage.clear();

        window.location.href = `/auth/${window.location.search}`;
    }

}
