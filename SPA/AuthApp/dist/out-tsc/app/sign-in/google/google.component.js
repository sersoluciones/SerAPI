import { __decorate, __param } from "tslib";
import { Component, HostListener, Inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
let GoogleComponent = class GoogleComponent {
    // tslint:disable-next-line: max-line-length
    constructor(loginService, googleSDKService, openIdClient, aws) {
        this.loginService = loginService;
        this.googleSDKService = googleSDKService;
        this.openIdClient = openIdClient;
        this.aws = aws;
    }
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();
        this.googleSDKService.login().subscribe((res) => {
            const loginFormEncoded = new HttpParams()
                .set('token', res.id_token)
                .set('third_type', 'google')
                .set('grant_type', 'delegation')
                .set('client_id', this.openIdClient.third_id);
            this.loginService.googleProfile = res;
            this.loginService.sendLogin(loginFormEncoded);
        }, (reject) => {
            this.loginService.setLoader(false);
            if (reject.error === 'popup_closed_by_user') {
                console.warn(JSON.stringify(reject.error, undefined, 2));
            }
        });
    }
};
__decorate([
    HostListener('click')
], GoogleComponent.prototype, "click", null);
GoogleComponent = __decorate([
    Component({
        selector: 'app-google',
        templateUrl: './google.component.html',
        styleUrls: ['./google.component.scss']
    }),
    __param(2, Inject('OPEN_ID_CLIENT'))
], GoogleComponent);
export { GoogleComponent };
//# sourceMappingURL=google.component.js.map