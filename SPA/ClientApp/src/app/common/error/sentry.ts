import { Injectable, ErrorHandler } from '@angular/core';
import * as Sentry from '@sentry/browser';
import { environment } from 'src/environments/environment';
import { hasValue } from '@sersol/ngx';

if (hasValue((window as any).sentry_dsn)) {
    Sentry.init({
        dsn: (window as any).sentry_dsn
    });
}

@Injectable()
export class SentryErrorHandler implements ErrorHandler {
    constructor() { }
    handleError(error: any) {
        if (hasValue((window as any).sentry_dsn) && environment.production) {
            Sentry.captureException(error.originalError || error);
        } else {
            console.error(error);
        }
    }
}
