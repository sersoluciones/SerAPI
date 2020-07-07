import { Component, HostListener, Inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { BaseLoginService } from '../base/base-login.service';
import { GoogleSDKService, GoogleUserProfile, OpenIdClient, AwsService } from '@sersol/ngx';

@Component({
    selector: 'app-google',
    templateUrl: './google.component.html',
    styleUrls: ['./google.component.scss']
})
export class GoogleComponent {
    googleAuth2: any;

    // tslint:disable-next-line: max-line-length
    constructor(private loginService: BaseLoginService, private googleSDKService: GoogleSDKService,
                @Inject('OPEN_ID_CLIENT') private openIdClient: OpenIdClient, public aws: AwsService) { }

    @HostListener('click')
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();

        this.googleSDKService.login().subscribe((res: GoogleUserProfile) => {

            const loginFormEncoded = new HttpParams()
                .set('token', res.id_token)
                .set('third_type', 'google')
                .set('grant_type', 'delegation')
                .set('client_id', this.openIdClient.third_id);

            this.loginService.googleProfile = res;
            this.loginService.sendLogin(loginFormEncoded);

        }, (reject: any) => {
            this.loginService.setLoader(false);

            if (reject.error === 'popup_closed_by_user') {
                console.warn(JSON.stringify(reject.error, undefined, 2));
            }
        });
    }

}
