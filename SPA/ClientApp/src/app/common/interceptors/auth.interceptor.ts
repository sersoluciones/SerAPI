import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { TokenResponse } from '../auth/token-response';
import { hasValue } from '@sersol/ngx';
import { DialogService } from '../dialog/dialog.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private isRefreshing = false;
    private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

    constructor(private _auth: AuthService, private _dialog: DialogService) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        if (request.url.indexOf('s3.amazonaws.com') === -1) {
            request = this.addToken(request);

            return next.handle(request).pipe(catchError((error: HttpErrorResponse) => {
                switch (error.status) {
                    case 401:
                        return this.handle401Error(request, next);

                    case 403:
                        return this.handle403Error(request, next, error);

                    case 500:
                        // return this.handle500Error(request, next, error);
                        return next.handle(request);

                    default:
                        return throwError(error);
                }
            }));
        }

        return next.handle(request);

    }

    private addToken(request: HttpRequest<any>) {
        return request.clone({
            setHeaders: {
                Authorization: `Bearer ${this._auth.getJwtToken()}`
            }
        });
    }

    private handle401Error(request: HttpRequest<any>, next: HttpHandler) {
        if (hasValue(this._auth.getRefreshToken())) {
            if (!this.isRefreshing) {
                this.isRefreshing = true;
                this.refreshTokenSubject.next(null);

                return this._auth.refreshToken().pipe(
                    switchMap((token: TokenResponse) => {
                        this.isRefreshing = false;
                        this.refreshTokenSubject.next(token.access_token);
                        return next.handle(this.addToken(request));
                    }));

            } else {
                return this.refreshTokenSubject.pipe(
                    filter(token => token != null),
                    take(1),
                    switchMap(() => {
                        return next.handle(this.addToken(request));
                    }));
            }
        } else {
            this._auth.logout();
            return next.handle(request);
        }
    }

    private handle403Error(request: HttpRequest<any>, next: HttpHandler, error: HttpErrorResponse) {
        console.log(error);

        this._dialog.error(error);

        return next.handle(request);
    }

    private handle500Error(request: HttpRequest<any>, next: HttpHandler, error: HttpErrorResponse) {
        this._dialog.error(error);

        return next.handle(request);
    }
}
