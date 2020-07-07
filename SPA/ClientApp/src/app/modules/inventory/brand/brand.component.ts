import { Component, OnInit } from '@angular/core';
import { GraphQLCrudEdit } from 'src/app/common/graphql/crud-edit';
import { GraphQuery, CrudPermissions, FilterField, ExportSettings, PaginationParams } from 'src/app/common/interfaces/base';
import { FormGroup, Validators } from '@angular/forms';

@Component({
  templateUrl: './brand.component.html',
  styleUrls: ['./brand.component.scss']
})
export class BrandComponent extends GraphQLCrudEdit {
    restUrl = 'Brand/';

    graphQuery: GraphQuery = {
        list: {
            name: 'brands_list',
            fields: ['id', 'name', 'abbreviation']
        },
        delete: {
            name: 'delete_brand',
            pk: 'brandId'
        }
    };

    permissions: CrudPermissions = {
        create: 'brands.add',
        update: 'brands.update',
        delete: 'brands.delete'
    };

    filters: FilterField[] = [
        { type: 'input', label: 'name', field: 'name', exact: false },
        { type: 'input', label: 'abbreviation', field: 'abbreviation', exact: false }
    ];

    exportSettings: ExportSettings = {
        modelName: 'brands',
        columns: ['id', 'name', 'abbreviation'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'name',
        sortReverse: false
    };

    newObject(): FormGroup {
        return this._fb.group({
            name: ['', Validators.required],
            abbreviation: ['']
        });
    }

    listObject(object: any): FormGroup {
        return this._fb.group({
            id: [object.id],
            name: [object.name, Validators.required],
            abbreviation: [object.abbreviation]
        });
    }

    getItemDeleteText(object: any): string {
        return `${object.abbreviation} ${object.name}`;
    }
}
