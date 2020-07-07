import { Component, HostBinding, HostListener, Output, EventEmitter } from '@angular/core';

@Component({
    selector: 'app-backdrop',
    template: '',
    styleUrls: ['./backdrop.component.scss']
})
export class BackdropComponent {

    constructor() { }

    // tslint:disable-next-line: no-output-on-prefix
    @Output()
    public onClick = new EventEmitter<MouseEvent>();

    @HostListener('click', ['$event'])
    click(event: MouseEvent) {
        this.onClick.emit(event);
    }

}
