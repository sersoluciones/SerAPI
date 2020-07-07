import { PaginationParams, CrudPermissions, FilterField, GraphQuery } from './base';
import { FormGroup } from '@angular/forms';

export declare interface IBaseCrud {
    paginationParams: PaginationParams;
    permissions: CrudPermissions;
    filters: FilterField[];
    graphQuery: GraphQuery;
    newObject(): FormGroup;
    listObject(object: any): FormGroup;
}
