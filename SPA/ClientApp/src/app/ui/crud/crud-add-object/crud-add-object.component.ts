import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CrudState, CrudPermissions } from 'src/app/common/interfaces/base';
import { hasValue, ClaimsService } from '@sersol/ngx';

@Component({
  selector: 'crud-add-object',
  templateUrl: './crud-add-object.component.html',
  styleUrls: ['./crud-add-object.component.scss']
})
export class CrudAddObjectComponent {

    @Input() state: CrudState;
    @Input() label: string;
    @Input() permissions: CrudPermissions;
    @Output() add: EventEmitter<void> = new EventEmitter();
    hasValue = hasValue;

    constructor(public claimService: ClaimsService) {}

}
