import { Injectable, Output, EventEmitter } from '@angular/core';
import { hasValue } from '@sersol/ngx';

@Injectable({
    providedIn: 'root'
})
export class SidebarService {
    main: SidebarStatus = {
        name: 'main',
        show: {
            status: false,
            event: new EventEmitter(),
            toggle(status?: boolean) {
                if (hasValue(status)) {
                    this.status = status;
                    this.event.emit(status);
                } else {
                    this.status = !this.status;
                    this.event.emit(this.status);
                }
            }
        }
    };

    profile: SidebarStatus = {
        name: 'profile',
        show: {
            status: false,
            event: new EventEmitter(),
            toggle(status?: boolean) {
                if (hasValue(status)) {
                    this.status = status;
                    this.event.emit(status);
                } else {
                    this.status = !this.status;
                    this.event.emit(this.status);
                }
            }
        }
    };

    notifications: SidebarStatus = {
        name: 'notifications',
        show: {
            status: false,
            event: new EventEmitter(),
            toggle(status?: boolean) {
                if (hasValue(status)) {
                    this.status = status;
                    this.event.emit(status);
                } else {
                    this.status = !this.status;
                    this.event.emit(this.status);
                }
            }
        }
    };

    favorites: SidebarStatus = {
        name: 'favorites',
        show: {
            status: false,
            event: new EventEmitter(),
            toggle(status?: boolean) {
                if (hasValue(status)) {
                    this.status = status;
                    this.event.emit(status);
                } else {
                    this.status = !this.status;
                    this.event.emit(this.status);
                }
            }
        }
    };

    constructor() { }
}

/**
 * @description
 * Interface que representa al objeto Sidebar para controlar el estado de todas las sidebars
 */
export interface SidebarStatus {
    name?: string;
    show?: {
        status: boolean;
        event: EventEmitter<boolean>;
        toggle(showState?: boolean): any;
    };
}
