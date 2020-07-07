import { Component, OnInit, Inject, NgZone, OnDestroy } from '@angular/core';
import { Validators, FormBuilder } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { CookiesService, OpenIdClient, AwsService } from '@sersol/ngx';
import { TokenResponse } from './token-response';
import { BaseLoginService } from './base/base-login.service';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-sign-in',
    templateUrl: './sign-in.component.html',
    styleUrls: ['./sign-in.component.scss']
})
export class SignInComponent implements OnInit, OnDestroy {
    loader = false;
    selectedIndex = 0;
    loginFormEncoded: HttpParams;
    subscriptions: Subscription[] = [];

    forgot_ok = false;
    forgot_errors = false;
    login_errors = '';
    year = (new Date()).getFullYear();

    loginForm = this.formBuilder.group({
        username: ['', [Validators.required]],
        password: ['', [Validators.required]],
        remember: [false]
    });

    forgotForm = this.formBuilder.group({
        email_username: ['', [Validators.required]]
    });

    constructor(private formBuilder: FormBuilder, private http: HttpClient, private translate: TranslateService,
                private loginService: BaseLoginService, private cookiesService: CookiesService, public aws: AwsService,
                @Inject('OPEN_ID_CLIENT') private openIdClient: OpenIdClient, private router: Router, private ngZone: NgZone) { }

    setTab(tab: number) {
        this.selectedIndex = tab;

        if (tab === 1) {
            this.forgotForm.setValue({
                email_username: ''
            });

            this.forgot_errors = false;
            this.forgot_ok = false;
        }
    }

    togglePassword() {
        const x = document.getElementById('password') as HTMLInputElement;
        const icon = document.getElementById('icon-password') as HTMLElement;

        if (x.type === 'password') {
            x.type = 'text';
            icon.innerHTML = 'visibility';
        } else {
            x.type = 'password';
            icon.innerHTML = 'visibility_off';
        }
    }

    forgot() {

        if (this.forgotForm.valid) {

            this.loader = true;
            this.forgot_errors = false;
            this.forgot_ok = false;

            this.http.post(`/api/User/ForgotPassword/${this.forgotForm.value.email_username}`, {})
                .subscribe(() => {
                    this.forgotForm.setValue({
                        email_username: ''
                    });

                    this.forgot_ok = true;
                    this.loader = false;

                }, () => {
                    this.loader = false;
                    this.forgot_errors = true;
                });
        }

    }

    normalSignIn() {
        if (this.loginForm.valid) {
            this.loader = true;
            this.loginService.clearProfiles();

            this.loginFormEncoded = new HttpParams()
                .set('username', this.loginForm.value.username)
                .set('password', this.loginForm.value.password)
                .set('grant_type', 'password')
                .set('client_id', this.openIdClient.id);

            this.login();
        }
    }

    login() {
        this.login_errors = '';

        const authorization = this.loginFormEncoded.get('client_id');
        const loginFormEncoded = this.loginFormEncoded.delete('client_id');

        this.http.post('/connect/token', loginFormEncoded, {
            headers: {
                Authorization: `Basic ${authorization}`
            }
        })
        .subscribe((response: TokenResponse) => {

            const decoded_access_token = JSON.parse(atob(response.access_token.split('.')[1]));

            this.cookiesService.set('token', response.access_token, decoded_access_token.exp, '/', undefined, true, 'Strict');

            if (this.loginForm.value.remember || this.loginService.remember) {
                this.cookiesService.set('refresh_token', response.refresh_token, decoded_access_token.exp, '/', undefined, true, 'Strict');
            }

            this.http.get('/api/userinfo', {
                headers: {
                    Authorization: `Bearer ${response.access_token}`
                }
            })
            .subscribe((profile: any) => {

                localStorage.setItem('oidc_user', JSON.stringify(profile));
                document.location.href = `/v1/${window.location.search}`;

            }, (reject) => {
                this.loader = false;

                if (reject?.error?.error_description) {
                    this.translate.get(reject.error.error_description).subscribe((text: string) => {
                        this.login_errors = text;
                    });
                }
            });

        }, (reject) => {
            this.loader = false;

            if (reject?.error?.error_description) {

                if (reject.error.error_description === 'User Not found') {
                    this.router.navigate(['sign-up']);
                }

                this.translate.get(reject.error.error_description).subscribe((text: string) => {
                    this.login_errors = text;
                });
            }
        });
    }

    ngOnInit() {
        this.subscriptions.push(
            this.loginService.loader.subscribe((data: boolean) => {
                this.loader = data;
            })
        );

        this.subscriptions.push(
            this.loginService.login.subscribe((data: HttpParams) => {
                this.loginFormEncoded = data;
                this.ngZone.run(() => this.login());
            })
        );
    }

    ngOnDestroy() {
        this.subscriptions.forEach(element => {
            element.unsubscribe();
        });
    }
}
