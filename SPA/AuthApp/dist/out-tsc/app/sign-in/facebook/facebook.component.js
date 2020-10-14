import { __decorate, __param } from "tslib";
import { Component, HostListener, Inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
let FacebookComponent = class FacebookComponent {
    // tslint:disable-next-line: max-line-length
    constructor(loginService, facebookSDKService, openIdClient, aws) {
        this.loginService = loginService;
        this.facebookSDKService = facebookSDKService;
        this.openIdClient = openIdClient;
        this.aws = aws;
    }
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();
        this.facebookSDKService.login().subscribe((res) => {
            const loginFormEncoded = new HttpParams()
                .set('token', res.access_token)
                .set('third_type', 'facebook')
                .set('grant_type', 'delegation')
                .set('client_id', this.openIdClient.third_id);
            this.loginService.facebookProfile = res;
            this.loginService.sendLogin(loginFormEncoded);
        }, (reject) => {
            this.loginService.setLoader(false);
            console.warn(reject);
        });
    }
};
__decorate([
    HostListener('click')
], FacebookComponent.prototype, "click", null);
FacebookComponent = __decorate([
    Component({
        selector: 'app-facebook',
        templateUrl: './facebook.component.html',
        styleUrls: ['./facebook.component.scss']
    }),
    __param(2, Inject('OPEN_ID_CLIENT'))
], FacebookComponent);
export { FacebookComponent };
//# sourceMappingURL=facebook.component.js.map