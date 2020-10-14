import { __decorate, __param } from "tslib";
import { Component, HostListener, Inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
let MicrosoftComponent = class MicrosoftComponent {
    constructor(loginService, authService, openIdClient, aws) {
        this.loginService = loginService;
        this.authService = authService;
        this.openIdClient = openIdClient;
        this.aws = aws;
    }
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();
        this.authService.loginPopup().then((res) => {
            const microsoftProfile = {
                id_token: res,
                first_name: this.authService.getUser().name,
                username: this.authService.getUser().displayableId,
                email: this.authService.getUser().displayableId
            };
            const loginFormEncoded = new HttpParams()
                .set('token', microsoftProfile.id_token)
                .set('third_type', 'microsoft')
                .set('grant_type', 'delegation')
                .set('client_id', this.openIdClient.third_id);
            this.loginService.microsoftProfile = microsoftProfile;
            this.loginService.sendLogin(loginFormEncoded);
        }).catch((err) => this.loginService.setLoader(false));
    }
};
__decorate([
    HostListener('click')
], MicrosoftComponent.prototype, "click", null);
MicrosoftComponent = __decorate([
    Component({
        selector: 'app-microsoft',
        templateUrl: './microsoft.component.html',
        styleUrls: ['./microsoft.component.scss']
    }),
    __param(2, Inject('OPEN_ID_CLIENT'))
], MicrosoftComponent);
export { MicrosoftComponent };
//# sourceMappingURL=microsoft.component.js.map