import { Injectable, EventEmitter, Output } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { MicrosoftResponse } from '../microsoft/microsoft-response';
import { GoogleUserProfile, FacebookUserProfile } from '@sersol/ngx';

@Injectable({
  providedIn: 'root'
})
export class BaseLoginService {
    public remember = false;

    private _googleProfile: GoogleUserProfile;
    public get googleProfile(): GoogleUserProfile {
        return this._googleProfile;
    }
    public set googleProfile(value: GoogleUserProfile) {
        this.remember = true;
        this._googleProfile = value;
    }

    private _microsoftProfile: MicrosoftResponse;
    public get microsoftProfile(): MicrosoftResponse {
        return this._microsoftProfile;
    }
    public set microsoftProfile(value: MicrosoftResponse) {
        this.remember = true;
        this._microsoftProfile = value;
    }

    private _facebookProfile: FacebookUserProfile;
    public get facebookProfile(): FacebookUserProfile {
        return this._facebookProfile;
    }
    public set facebookProfile(value: FacebookUserProfile) {
        this.remember = true;
        this._facebookProfile = value;
    }

    @Output() loader: EventEmitter<boolean> = new EventEmitter();
    @Output() login: EventEmitter<HttpParams> = new EventEmitter();

    clearProfiles() {
        this.googleProfile = null;
        this.microsoftProfile = null;
        this.facebookProfile = null;
        this.remember = false;
    }

    setLoader(value: boolean) {
      this.loader.emit(value);
    }

    sendLogin(loginFormEncoded: HttpParams) {
        this.login.emit(loginFormEncoded);
    }
}
