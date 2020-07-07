import { AuthService } from 'src/app/common/auth/auth.service';
import { OidcUser } from '../../common/auth/oidc';
import { Component } from '@angular/core';
import {  } from 'src/app/common/auth/oidc';
import { SidebarService } from '../sidebar/sidebar.service';

@Component({
    selector: 'app-topbar',
    templateUrl: './topbar.component.html',
    styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent {
    oidcUser: OidcUser;

    constructor(private sidebarService: SidebarService, auth: AuthService) {
        this.oidcUser = auth.oidcUser;
    }

    openSidebar() {
        this.sidebarService.main.show.toggle();
    }

    openProfile() {
        this.sidebarService.profile.show.toggle();
    }

    openNotifications() {
        this.sidebarService.notifications.show.toggle();
    }

    openFavorites() {
        this.sidebarService.favorites.show.toggle();
    }

}
