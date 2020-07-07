import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CrudState, CrudPermissions } from 'src/app/common/interfaces/base';
import { ClaimsService } from '@sersol/ngx';
import { FormArray } from '@angular/forms';

@Component({
    selector: 'crud-edit-tools',
    templateUrl: './crud-edit-tools.component.html',
    styleUrls: ['./crud-edit-tools.component.scss']
})
export class CrudEditToolsComponent {

    @Input() state: CrudState;
    @Input() permissions: CrudPermissions;
    @Input() objectList: FormArray;
    @Output() save: EventEmitter<void> = new EventEmitter();

    constructor(public claimService: ClaimsService) { }

    toggleEditMode() {
        this.state.formMode = !this.state.formMode;

        if (!this.state.formMode) {

            const newObjects = [];

            this.objectList.value.forEach((value: any, index: number) => {
                if (value._new) {
                    newObjects.push(index);
                }
            });

            for (let i2 = newObjects.length - 1; i2 >= 0; i2--) {
                this.objectList.removeAt(newObjects[i2]);
            }

        }
    }

}
