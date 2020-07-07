import { MicrosoftResponse } from './microsoft-response';
import { Component, HostListener, Inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { BaseLoginService } from '../base/base-login.service';
import { HttpParams } from '@angular/common/http';
import { OpenIdClient, AwsService } from '@sersol/ngx';

@Component({
    selector: 'app-microsoft',
    templateUrl: './microsoft.component.html',
    styleUrls: ['./microsoft.component.scss']
})
export class MicrosoftComponent {

    constructor(private loginService: BaseLoginService, private authService: MsalService,
                @Inject('OPEN_ID_CLIENT') private openIdClient: OpenIdClient, public aws: AwsService) { }

    @HostListener('click')
    click() {
        this.loginService.setLoader(true);
        this.loginService.clearProfiles();
        this.authService.loginPopup().then((res) => {

            const microsoftProfile: MicrosoftResponse = {
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

}

