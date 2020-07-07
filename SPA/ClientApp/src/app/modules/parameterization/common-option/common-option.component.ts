import { Component } from '@angular/core';
import { GraphQLCrudEdit } from 'src/app/common/graphql/crud-edit';
import { PaginationParams, CrudPermissions, FilterField, GraphQuery, ExportSettings } from 'src/app/common/interfaces/base';
import { FormGroup, Validators } from '@angular/forms';

@Component({
  templateUrl: './common-option.component.html',
  styleUrls: ['./common-option.component.scss']
})
export class CommonOptionComponent extends GraphQLCrudEdit {

    restUrl = 'CommonOptions/';
    graphQuery: GraphQuery = {
        list: {
            name: 'common_options_list',
            fields: ['id', 'type', 'value', 'description']
        },
        delete: {
            pk: 'commonoptionId',
            name: 'delete_commonoption'
        }
    };

    permissions: CrudPermissions = {
        create: 'commonoptions.add',
        update: 'commonoptions.update',
        delete: 'commonoptions.delete'
    };

    filters: FilterField[] = [
        { type: 'input', label: 'name', field: 'type', exact: false },
        { type: 'input', label: 'value', field: 'value', exact: false },
        { type: 'input', label: 'description', field: 'description', exact: false }
    ];

    exportSettings: ExportSettings = {
        modelName: 'countries',
        columns: ['id', 'type', 'value', 'description'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'type',
        sortReverse: false
    };

    newObject(): FormGroup {
        return this._fb.group({
            type: ['', [Validators.required]],
            value: ['', [Validators.required]],
            description: ['', [Validators.required]]
        });
    }

    listObject(object: any): FormGroup {
        return this._fb.group({
            id: [object.id],
            type: [object.type, [Validators.required]],
            value: [object.value, [Validators.required]],
            description: [object.description, [Validators.required]]
        });
    }

    getItemDeleteText(object: any): string {
        return `${object.type}: ${object.description}`;
    }
}
