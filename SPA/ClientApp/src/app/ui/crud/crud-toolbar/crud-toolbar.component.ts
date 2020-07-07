import { FilterField } from 'src/app/common/interfaces/base';
import { CrudState, PaginationState } from './../../../common/interfaces/base';
import { AwsService, hasValue } from '@sersol/ngx';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseEventService } from 'src/app/common/base/base-event.service';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'crud-toolbar',
  templateUrl: './crud-toolbar.component.html',
  styleUrls: ['./crud-toolbar.component.scss']
})
export class CrudToolbarComponent {

    @Input() state: CrudState;
    @Input() pagination: PaginationState;
    @Input() filters: FilterField[];
    @Input() filterFG: FormGroup;
    @Output() list: EventEmitter<void> = new EventEmitter();
    @Output() download: EventEmitter<string> = new EventEmitter();
    hasValue = hasValue;

    constructor(public aws: AwsService) { }

    toggleFilters() {
        this.pagination.showFilters = !this.pagination.showFilters;
    }

    setSort(field: string) {
        this.pagination.sortType = field;
        this.pagination.sortReverse = !this.pagination.sortReverse;

        if (this.pagination.sortReverse) {
            this.pagination.sortReverseSymbol = 'DESC';
        } else {
            this.pagination.sortReverseSymbol = 'ASC';
        }

        this.list.emit();
    }
}
