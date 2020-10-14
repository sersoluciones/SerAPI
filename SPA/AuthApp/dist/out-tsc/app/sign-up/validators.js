import { __decorate } from "tslib";
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
/**
 * @description Validadores asincronos
 */
let UniqueEmail = class UniqueEmail {
    constructor(http) {
        this.http = http;
    }
    validate(control) {
        const validationError = { emailTaken: true };
        const url = '/User/VerifyEmail/' + control.value;
        return this.http.get(url).pipe(map(response => {
            console.log(response, 'async validator');
            if (response == null) {
                return validationError;
            }
        }), catchError(() => of(null)));
    }
};
UniqueEmail = __decorate([
    Injectable({ providedIn: 'root' })
], UniqueEmail);
export { UniqueEmail };
//# sourceMappingURL=validators.js.map