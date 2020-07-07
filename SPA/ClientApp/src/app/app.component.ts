import { SidebarService } from './ui/sidebar/sidebar.service';
import { AuthService } from 'src/app/common/auth/auth.service';
import { TranslateService } from '@ngx-translate/core';
import { OidcUser } from 'src/app/common/auth/oidc';
import { Component, OnInit, Renderer2 } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { DomSanitizer } from '@angular/platform-browser';
import { AwsService, PrefersColorSchemeService, setBowserClasses } from '@sersol/ngx';
import { Router, NavigationEnd } from '@angular/router';
import * as Bowser from 'bowser';
import * as Sentry from '@sentry/browser';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {

    oidcUser: OidcUser;
    offlineBackdrop: HTMLDivElement;

    constructor(private _iconRegistry: MatIconRegistry, private _sanitizer: DomSanitizer, public aws: AwsService, private _colorscheme: PrefersColorSchemeService,
                private _translate: TranslateService, private _rendered: Renderer2, auth: AuthService, private router: Router, private sidebarService: SidebarService) {

        if (auth.isLoggedIn()) {
            this.oidcUser = auth.oidcUser;

            if ((window as any).debug) {
                console.log('%c📟 DEBUG MODE ENABLED', 'color: limegreen;border: 1px solid limegreen;padding: 8px; border-radius: 4px;margin: 8px 0;');

                console.group('%c[debug]', 'color: limegreen;');

                console.groupCollapsed('JWT token');
                console.log(auth.getJwtToken());
                console.groupEnd();

                console.groupCollapsed('Refresh token');
                console.log(auth.getRefreshToken());
                console.groupEnd();

                console.groupCollapsed('Session data');
                console.log(this.oidcUser);
                console.groupEnd();

                console.groupCollapsed('Claims');
                console.log(this.oidcUser.claims);
                console.groupEnd();

                console.groupEnd();

            } else {

                Sentry.configureScope(scope => {
                    scope.setUser({
                        email: this.oidcUser.email,
                        username: this.oidcUser.username
                    });

                    scope.setExtra('Rol', this.oidcUser.role);
                    scope.setExtra('Company - id', this.oidcUser.company_id);
                    scope.setExtra('Company - name', this.oidcUser.company_name);
                });

                console.log(`
                 ____   ____   ___
                / __/  / __/  / _ \\
               _\\ \\   / _/   / , _/
              /___/  /___/  /_/|_|

                `);
            }

        } else {
            auth.logout();
        }
    }

    ngOnInit(): void {

        const bowser = Bowser.getParser(window.navigator.userAgent);
        setBowserClasses(bowser, this._rendered);

        if (this.oidcUser?.dark_mode) {
            this._colorscheme.enableDark();
        } else {
            this._colorscheme.enableLight();
        }

        this._colorscheme.watch();

        this._iconRegistry.addSvgIconSetInNamespace(
            'ser',
            this._sanitizer.bypassSecurityTrustResourceUrl(this.aws.getS3Url('assets/icons/icons.svg'))
        );

        this.offlineBackdrop = this._rendered.createElement('div');
        this.offlineBackdrop.className = 'offline-backdrop';

        this._translate.get('offline_msg').subscribe((text) => {
            this.offlineBackdrop.innerHTML = `<img src="${this.aws.getS3Url('assets/images/offline-spinner.svg')}"><div class="text">${text}</div>`;
        });

        window.addEventListener('online', () => {
            if ((window as any).debug) {
                console.group('%c[debug]', 'color: limegreen;');
                console.log('%cNetwork connected...', 'color: limegreen;font-weight: bold;');
                console.groupEnd();
            } else {
                this._rendered.removeChild(document.body, this.offlineBackdrop);
            }
        });

        window.addEventListener('offline', () => {
            if ((window as any).debug) {
                console.group('%c[debug]', 'color: limegreen;');
                console.log('%cNetwork disconnected...', 'color: red;font-weight: bold;');
                console.groupEnd();
            } else {
                this._rendered.appendChild(document.body, this.offlineBackdrop);
            }
        });

        this.router.events.subscribe((ev) => {
            if (ev instanceof NavigationEnd) {
                this.sidebarService.main.show.event.emit(false);
            }
        });

    }
}
