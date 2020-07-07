import { BaseEventService } from './../../common/base/base-event.service';
import { HttpClient } from '@angular/common/http';
import { BackdropComponent } from './../backdrop/backdrop.component';
import { Component, OnInit, HostBinding, ComponentFactoryResolver, ApplicationRef, Injector, EmbeddedViewRef, ComponentRef } from '@angular/core';
import { SidebarService } from '../sidebar/sidebar.service';
import { PrefersColorSchemeService } from '@sersol/ngx';
import { OidcUser } from 'src/app/common/auth/oidc';
import { AuthService } from 'src/app/common/auth/auth.service';
import { take } from 'rxjs/operators';

@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {

    @HostBinding('class.show') show = false;
    _backdrop: ComponentRef<BackdropComponent>;
    backdropDOMElem: HTMLElement;
    selectedIndex = 0;
    oidcUser: OidcUser;

    constructor(private _sidebarService: SidebarService, private _colorscheme: PrefersColorSchemeService, public auth: AuthService, private _componentFactoryResolver: ComponentFactoryResolver,
                private _appRef: ApplicationRef, private _injector: Injector, private _http: HttpClient) {
        this.oidcUser = this.auth.oidcUser;
    }

    close() {
        this.show = false;
        this._sidebarService.profile.show.status = false;
        this._appRef.detachView(this._backdrop.hostView);
    }

    setTab(tab: number) {
        this.selectedIndex = tab;
    }

    toggleDarkMode() {
        this.oidcUser = this.auth.oidcUser;
        this.oidcUser.dark_mode = !this.oidcUser.dark_mode;

        if (this.oidcUser.dark_mode) {
            this._colorscheme.enableDark();
        } else {
            this._colorscheme.enableLight();
        }

        this._http.put('/api/User/SetDarkMode', {
            new_value: this.oidcUser.dark_mode
        }).pipe(take(1)).subscribe(() => {
            localStorage.setItem('oidc_user', JSON.stringify(this.oidcUser));
        });
    }

    // TODO: eliminar subscribes
    ngOnInit() {

        this._backdrop = this._componentFactoryResolver
            .resolveComponentFactory(BackdropComponent)
            .create(this._injector);

        this._backdrop.instance.onClick.subscribe((ev: MouseEvent) => {
            this.close();
        });

        this._sidebarService.profile.show.event.subscribe((data: boolean) => {
            this.show = data;

            if (data) {
                this._appRef.attachView(this._backdrop.hostView);
                this.backdropDOMElem = (this._backdrop.hostView as EmbeddedViewRef<any>).rootNodes[0] as HTMLElement;
                document.body.appendChild(this.backdropDOMElem);
            }
        });
    }

}
