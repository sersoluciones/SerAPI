import { Validators, FormGroup } from '@angular/forms';
import { Component } from '@angular/core';
import { GraphQLCrudEdit } from 'src/app/common/graphql/crud-edit';
import { PaginationParams, GraphQuery, FilterField, CrudPermissions, ExportSettings } from 'src/app/common/interfaces/base';

@Component({
    templateUrl: './permission.component.html',
    styleUrls: ['./permission.component.scss']
})
export class PermissionComponent extends GraphQLCrudEdit {

    restUrl = 'Permissions/';
    graphQuery: GraphQuery = {
        list: {
            name: 'permissions_list',
            fields: ['id', 'name']
        },
        delete: {
            pk: 'permissionId',
            name: 'delete_permission'
        }
    };

    permissions: CrudPermissions = {
        create: 'permissions.add',
        update: 'permissions.update',
        delete: 'permissions.delete'
    };

    filters: FilterField[] = [
        {
            type: 'input', label: 'name', field: 'name', exact: false
        }
    ];

    exportSettings: ExportSettings = {
        modelName: 'permissions',
        columns: ['id', 'name'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'name',
        sortReverse: false
    };

    newObject(): FormGroup {
        return this._fb.group({
            name: ['', [Validators.required, Validators.pattern(/^([a-z0-9]+)\.([a-z0-9]+)$/)]]
        });
    }

    listObject(object: any): FormGroup {
        return this._fb.group({
            id: [object.id],
            name: [object.name, [Validators.required, Validators.pattern(/^([a-z0-9]+)\.([a-z0-9]+)$/)]]
        });
    }

    getItemDeleteText(object: any): string {
        return object.name;
    }
}
