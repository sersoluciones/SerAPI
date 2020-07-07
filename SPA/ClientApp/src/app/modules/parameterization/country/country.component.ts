import { Component } from '@angular/core';
import { GraphQLCrudEdit } from 'src/app/common/graphql/crud-edit';
import { PaginationParams, CrudPermissions, FilterField, GraphQuery, ExportSettings } from 'src/app/common/interfaces/base';
import { FormGroup, Validators } from '@angular/forms';

@Component({
  templateUrl: './country.component.html',
  styleUrls: ['./country.component.scss']
})
export class CountryComponent extends GraphQLCrudEdit {

    restUrl = 'Country/';
    graphQuery: GraphQuery = {
        list: {
            name: 'countries_list',
            fields: ['id', 'name_es', 'code_1', 'code_2', 'code_3']
        },
        delete: {
            pk: 'countryId',
            name: 'delete_country'
        }
    };

    permissions: CrudPermissions = {
        create: 'countries.add',
        update: 'countries.update',
        delete: 'countries.delete'
    };

    filters: FilterField[] = [
        { type: 'input', label: 'name', field: 'name_es', exact: false },
        { type: 'input', label: 'ISO 3166-1 alfa-2', field: 'code_1', exact: false },
        { type: 'input', label: 'ISO 3166-1 alfa-3', field: 'code_2', exact: false },
        { type: 'input', label: 'ISO 3166-1 númerico', field: 'code_3', exact: false }
    ];

    exportSettings: ExportSettings = {
        modelName: 'countries',
        columns: ['id', 'name_es', 'code_1', 'code_2', 'code_3'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'name_es',
        sortReverse: false
    };

    newObject(): FormGroup {
        return this._fb.group({
            name_es: ['', [Validators.required]],
            code_1: ['', [Validators.required]],
            code_2: ['', [Validators.required]],
            code_3: ['', [Validators.required]]
        });
    }

    listObject(object: any): FormGroup {
        return this._fb.group({
            id: [object.id],
            name_es: [object.name_es, [Validators.required]],
            code_1: [object.code_1, [Validators.required]],
            code_2: [object.code_2, [Validators.required]],
            code_3: [object.code_3, [Validators.required]]
        });
    }

    getItemDeleteText(object: any): string {
        return object.name_es;
    }

}
