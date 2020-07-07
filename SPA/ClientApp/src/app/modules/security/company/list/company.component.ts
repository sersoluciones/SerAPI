import { Component } from '@angular/core';
import { GraphQLCrudRead } from 'src/app/common/graphql/crud-read';
import { GraphQuery, CrudPermissions, FilterField, ExportSettings, PaginationParams } from 'src/app/common/interfaces/base';
import { CompanyFormComponent } from '../form/company-form.component';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-company',
  templateUrl: './company.component.html',
  styleUrls: ['./company.component.scss']
})
export class CompanyComponent extends GraphQLCrudRead {

  restUrl = 'Company/';
  graphQuery: GraphQuery = {
      list: {
          name: 'companies_list',
          fields: ['id', 'base_identity_id', 'email', 'base_identity{name, last_name, document_type_id, document_number}']
      },
      delete: {
          pk: 'companyId',
          name: 'delete_company'
      }
  };

  permissions: CrudPermissions = {
      create: 'companies.add',
      update: 'companies.update',
      delete: 'companies.delete'
  };

  filters: FilterField[] = [
      { type: 'input', label: 'name', field: 'name', exact: false }
  ];

  exportSettings: ExportSettings = {
      modelName: 'companies',
      columns: ['id', 'name'],
      formats: ['xlsx']
  };

  paginationParams: PaginationParams = {
      pageSize: 30,
      orderBy: 'base_identity.name',
      sortReverse: false
  };

  getItemDeleteText(object: any): string {
      return object.base_identity.name;
  }

  addObject() {
      this._modalService.open(CompanyFormComponent, {
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

      this._modalService.open(CompanyFormComponent, {
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

