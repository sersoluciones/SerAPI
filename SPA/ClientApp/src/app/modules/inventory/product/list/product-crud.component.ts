import { Component } from '@angular/core';
import { PaginationParams, CrudPermissions, FilterField, GraphQuery, ExportSettings } from 'src/app/common/interfaces/base';
import { GraphQLCrudRead } from 'src/app/common/graphql/crud-read';
import { take } from 'rxjs/operators';
import { ProductFormComponent } from '../form/product-form.component';

@Component({
    templateUrl: './product-crud.component.html',
    styleUrls: ['./product-crud.component.scss']
})
export class ProductCrudComponent extends GraphQLCrudRead {

    restUrl = 'Product/';
    graphQuery: GraphQuery =  {
        list: {
            name: 'products_list',
            fields: ['id', 'name', 'price', 'current_quantity', 'short_description', 'attachment {key}']
        }
    };

    permissions: CrudPermissions = {
        create: 'products.add',
        update: 'products.update',
        delete: 'products.delete'
    };

    filters: FilterField[] = [
        {
            type: 'input', label: 'name', field: 'name'
        }
    ];

    exportSettings: ExportSettings = {
        modelName: 'products',
        columns: ['id', 'name'],
        formats: ['xlsx']
    };

    paginationParams: PaginationParams = {
        pageSize: 30,
        orderBy: 'name',
        sortReverse: false
    };

    addObject() {
        this._modalService.open(ProductFormComponent, {
            closeOnNavigation: true,
            disableClose: true,
            maxWidth: '100vw',
            width: '1000px',
            height: '97%',
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

        this._modalService.open(ProductFormComponent, {
            data: {
                instance: object
            },
            closeOnNavigation: true,
            disableClose: true,
            maxWidth: '100vw',
            width: '1000px',
            height: '97%',
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

