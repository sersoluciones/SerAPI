import { Component, HostListener, Inject } from '@angular/core';
import { BaseLoginService } from '../base/base-login.service';
import { HttpParams } from '@angular/common/http';
import { FacebookSDKService, FacebookUserProfile, OpenIdClient, AwsService } from '@sersol/ngx';

@Component({
    selector: 'app-facebook',
    templateUrl: './facebook.component.html',
    styleUrls: ['./facebook.component.scss']
})
export class FacebookComponent {

    // tslint:disable-next-line: max-line-length
    constructor(private loginService: BaseLoginService, private facebookSDKService: FacebookSDKService,
                @Inject('OPEN_ID_CLIENT') private openIdClient: OpenIdClient, public aws: AwsService) { }

    @HostListener('click')
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();

        this.facebookSDKService.login().subscribe((res: FacebookUserProfile) => {

            const loginFormEncoded = new HttpParams()
                .set('token', res.access_token)
                .set('third_type', 'facebook')
                .set('grant_type', 'delegation')
                .set('client_id', this.openIdClient.third_id);

            this.loginService.facebookProfile = res;
            this.loginService.sendLogin(loginFormEncoded);

        }, (reject: any) => {
            this.loginService.setLoader(false);
            console.warn(reject);
        });
    }

}
