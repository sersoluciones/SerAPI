import { __decorate, __param } from "tslib";
import { Component, Inject } from '@angular/core';
import { Validators } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
let SignInComponent = class SignInComponent {
    constructor(formBuilder, http, translate, loginService, cookiesService, aws, openIdClient, router, ngZone) {
        this.formBuilder = formBuilder;
        this.http = http;
        this.translate = translate;
        this.loginService = loginService;
        this.cookiesService = cookiesService;
        this.aws = aws;
        this.openIdClient = openIdClient;
        this.router = router;
        this.ngZone = ngZone;
        this.loader = false;
        this.selectedIndex = 0;
        this.subscriptions = [];
        this.forgot_ok = false;
        this.forgot_errors = false;
        this.login_errors = '';
        this.year = (new Date()).getFullYear();
        this.loginForm = this.formBuilder.group({
            username: ['', [Validators.required]],
            password: ['', [Validators.required]],
            remember: [false]
        });
        this.forgotForm = this.formBuilder.group({
            email_username: ['', [Validators.required]]
        });
    }
    setTab(tab) {
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
        const x = document.getElementById('password');
        const icon = document.getElementById('icon-password');
        if (x.type === 'password') {
            x.type = 'text';
            icon.innerHTML = 'visibility';
        }
        else {
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
            .subscribe((response) => {
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
                .subscribe((profile) => {
                localStorage.setItem('oidc_user', JSON.stringify(profile));
                document.location.href = `/v1/${window.location.search}`;
            }, (reject) => {
                var _a;
                this.loader = false;
                if ((_a = reject === null || reject === void 0 ? void 0 : reject.error) === null || _a === void 0 ? void 0 : _a.error_description) {
                    this.translate.get(reject.error.error_description).subscribe((text) => {
                        this.login_errors = text;
                    });
                }
            });
        }, (reject) => {
            var _a;
            this.loader = false;
            if ((_a = reject === null || reject === void 0 ? void 0 : reject.error) === null || _a === void 0 ? void 0 : _a.error_description) {
                if (reject.error.error_description === 'User Not found') {
                    this.router.navigate(['sign-up']);
                }
                this.translate.get(reject.error.error_description).subscribe((text) => {
                    this.login_errors = text;
                });
            }
        });
    }
    ngOnInit() {
        this.subscriptions.push(this.loginService.loader.subscribe((data) => {
            this.loader = data;
        }));
        this.subscriptions.push(this.loginService.login.subscribe((data) => {
            this.loginFormEncoded = data;
            this.ngZone.run(() => this.login());
        }));
    }
    ngOnDestroy() {
        this.subscriptions.forEach(element => {
            element.unsubscribe();
        });
    }
};
SignInComponent = __decorate([
    Component({
        selector: 'app-sign-in',
        templateUrl: './sign-in.component.html',
        styleUrls: ['./sign-in.component.scss']
    }),
    __param(6, Inject('OPEN_ID_CLIENT'))
], SignInComponent);
export { SignInComponent };
//# sourceMappingURL=sign-in.component.js.map