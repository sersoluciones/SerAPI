import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { CookiesService } from '@sersol/ngx';

/* @Injectable()
export class LangInterceptor implements HttpInterceptor {
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        if (req.url.indexOf('s3.amazonaws.com') === -1) {
            const cookies = new CookiesService();

            const modified = req.clone({
                setHeaders: {
                    Authorization: 'Bearer ' + cookies.get('token')
                }
            });

            return next.handle(modified);

        } else {

            return next.handle(req);

        }

    }
} */
