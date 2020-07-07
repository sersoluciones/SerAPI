import { ExportSettings } from '../../../../common/interfaces/base';
import { Component } from '@angular/core';
import { PaginationParams, CrudPermissions, FilterField, GraphQuery } from 'src/app/common/interfaces/base';
import { GraphQLCrudRead } from 'src/app/common/graphql/crud-read';
import { take } from 'rxjs/operators';
import { CategoryFormComponent } from '../form/category-form.component';

@Component({
  templateUrl: './category.component.html',
  styleUrls: ['./category.component.scss']
})
export class CategoryListComponent extends GraphQLCrudRead {

    restUrl = 'Category/';
    graphQuery: GraphQuery = {
        list: {
            name: 'categories_list',
            fields: ['id', 'name', 'attachment { key5 }']
        },
        delete: {
            pk: 'categoryId',
            name: 'delete_category'
        }
    };

    permissions: CrudPermissions = {
        create: 'categories.add',
        update: 'categories.update',
        delete: 'categories.delete'
    };

    filters: FilterField[] = [
        { type: 'input', label: 'name', field: 'name', exact: false }
    ];

    exportSettings: ExportSettings = {
        modelName: 'categories',
        columns: ['id', 'name'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'name',
        sortReverse: false
    };

    getItemDeleteText(object: any): string {
        return object.name;
    }

    addObject() {
        this._modalService.open(CategoryFormComponent, {
            closeOnNavigation: true,
            disableClose: true,
            maxWidth: '100vw'
        })
        .afterClosed()
        .pipe(take(1)).subscribe((reload: boolean) => {
            if (reload) {
                this.getList();
            }
        });
    }

    editObject(object: any) {
        object._loading = true;

        this._modalService.open(CategoryFormComponent, {
            data: {
                instance: object
            },
            closeOnNavigation: true,
            disableClose: true,
            maxWidth: '100vw'
        })
        .afterClosed()
        .pipe(take(1)).subscribe((reload: boolean) => {
            if (reload) {
                this.getList();
            }
        }, () => {}, () => {
            object._loading = false;
        });
    }

}
