import { Component, ElementRef, AfterViewInit, Renderer2, HostBinding } from '@angular/core';
import { AwsService, ClaimsService, hasValue } from '@sersol/ngx';
import { SidebarService } from './sidebar.service';
import { fromEvent, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { distinctUntilChanged } from 'rxjs-compat/operator/distinctUntilChanged';

@Component({
    selector: 'app-sidebar',
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements AfterViewInit {

    @HostBinding('class.show') show = false;
    subscriptions = new Subscription();
    lastMenu: any = null;
    collapse = false;
    self = this;

    constructor(private element: ElementRef, private renderer: Renderer2, public aws: AwsService, public claimService: ClaimsService, private sidebarService: SidebarService) { }

    setPositionSubmenu(target?: any) {

        const submenu = this.element.nativeElement.querySelector('.submenu-wrapper') as HTMLDivElement;

        if (hasValue(this.lastMenu)) {
            this.renderer.appendChild(this.lastMenu, submenu.querySelector('.submenu'));
        }

        this.renderer.appendChild(submenu, target.submenu);

        if (hasValue(target)) {
            this.lastMenu = target;
        }

        const remainingHeight = document.documentElement.offsetHeight - this.lastMenu.getBoundingClientRect().top;

        if (remainingHeight - submenu.offsetHeight < 50) {
            submenu.style.removeProperty('top');
            submenu.style.bottom = '58px';
        } else {
            submenu.style.top = (this.lastMenu.getBoundingClientRect().top - 7) + 'px';
            submenu.style.removeProperty('bottom');
        }
    }

    closeSubmenu() {
        const submenu = this.element.nativeElement.querySelector('.submenu-wrapper') as HTMLDivElement;
    }

    initSubmenus() {
        const cbox = this.element.nativeElement.querySelectorAll('.menu') as NodeList;

        cbox.forEach((el: any) => {
            el.submenu = el.querySelector('.submenu');
            fromEvent<any>(el, 'mouseenter')
            .pipe()
            .subscribe((e: MouseEvent) => {
                this.setPositionSubmenu(e.target);
                // debugger;
            });
            /* this.renderer.listen(el, 'mouseenter', (e: MouseEvent) => {
                // setTimeout(() => {
                    this.setPositionSubmenu(e.target);
                // }, 1000);
            });

            this.renderer.listen(el, 'mouseleave', (e: MouseEvent) => {
                console.log(e);
            }); */
        });
    }

    ngAfterViewInit() {
        this.initSubmenus();

        this.sidebarService.main.show.event.subscribe((data: boolean) => {
            this.show = data;
            if (!data) {
                this.closeSubmenu();
            }
        });
    }

}
