import { Component, OnInit } from '@angular/core';
import { GraphQLCrudRead } from 'src/app/common/graphql/crud-read';
import { GraphQuery, CrudPermissions, FilterField, PaginationParams } from 'src/app/common/interfaces/base';
import { take } from 'rxjs/operators';
import { StoreFormComponent } from '../form/store-form.component';

@Component({
  selector: 'app-store',
  templateUrl: './store.component.html',
  styleUrls: ['./store.component.scss']
})
export class StoreListComponent extends GraphQLCrudRead {

  restUrl = 'Store/';
  graphQuery: GraphQuery = {
      list: {
          name: 'stores_list',
          fields: ['id', 'name']
      },
      delete: {
          pk: 'storeId',
          name: 'delete_store'
      }
  };

  permissions: CrudPermissions = {
      create: 'stores.add',
      update: 'stores.update',
      delete: 'stores.delete'
  };

  filters: FilterField[] = [
      { type: 'input', label: 'name', field: 'name', exact: false }
  ];

  paginationParams: PaginationParams = {
      pageSize: 30,
      orderBy: 'name',
      sortReverse: false
  };

  getItemDeleteText(object: any): string {
      return object.name;
  }

  addObject() {
      this._modalService.open(StoreFormComponent, {
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

      this._modalService.open(StoreFormComponent, {
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
