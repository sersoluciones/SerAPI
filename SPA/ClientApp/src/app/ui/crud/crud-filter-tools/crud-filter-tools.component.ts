import { Component, OnInit, Input } from '@angular/core';
import { PaginationState, FilterField } from 'src/app/common/interfaces/base';
import { FormGroup } from '@angular/forms';
import { BaseEventService } from 'src/app/common/base/base-event.service';

@Component({
    selector: 'crud-filter-tools',
    templateUrl: './crud-filter-tools.component.html',
    styleUrls: ['./crud-filter-tools.component.scss']
})
export class CrudFilterToolsComponent {

    @Input() pagination: PaginationState;
    @Input() filters: FilterField[];
    @Input() filterFG: FormGroup;

    constructor(public baseEvents: BaseEventService) { }

}
