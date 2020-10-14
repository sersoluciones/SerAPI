import { __decorate } from "tslib";
import { Injectable, EventEmitter, Output } from '@angular/core';
let BaseLoginService = class BaseLoginService {
    constructor() {
        this.remember = false;
        this.loader = new EventEmitter();
        this.login = new EventEmitter();
    }
    get googleProfile() {
        return this._googleProfile;
    }
    set googleProfile(value) {
        this.remember = true;
        this._googleProfile = value;
    }
    get microsoftProfile() {
        return this._microsoftProfile;
    }
    set microsoftProfile(value) {
        this.remember = true;
        this._microsoftProfile = value;
    }
    get facebookProfile() {
        return this._facebookProfile;
    }
    set facebookProfile(value) {
        this.remember = true;
        this._facebookProfile = value;
    }
    clearProfiles() {
        this.googleProfile = null;
        this.microsoftProfile = null;
        this.facebookProfile = null;
        this.remember = false;
    }
    setLoader(value) {
        this.loader.emit(value);
    }
    sendLogin(loginFormEncoded) {
        this.login.emit(loginFormEncoded);
    }
};
__decorate([
    Output()
], BaseLoginService.prototype, "loader", void 0);
__decorate([
    Output()
], BaseLoginService.prototype, "login", void 0);
BaseLoginService = __decorate([
    Injectable({
        providedIn: 'root'
    })
], BaseLoginService);
export { BaseLoginService };
//# sourceMappingURL=base-login.service.js.map