import { Component } from '@angular/core';
import { GraphQLCrudRead } from 'src/app/common/graphql/crud-read';
import { GraphQuery, CrudPermissions, FilterField, PaginationParams } from 'src/app/common/interfaces/base';
import { take, catchError, map } from 'rxjs/operators';
import { UserFormComponent } from '../form/user-form.component';
import { DialogCrudDeleteComponent } from 'src/app/ui/crud/dialog-crud-delete/dialog-crud-delete.component';
import { forkJoin, of } from 'rxjs';
import { hasValue } from '@sersol/ngx';

@Component({
    selector: 'app-user',
    templateUrl: './user.component.html',
    styleUrls: ['./user.component.scss']
})
export class UserComponent extends GraphQLCrudRead {

    restUrl = 'User/';
    graphQuery: GraphQuery = {
        list: {
            name: 'application_users_list',
            fields: ['id', 'name', 'lastName']
        },
        delete: {
            pk: 'applicationuserId',
            name: 'delete_applicationuser'
        }
    };

    permissions: CrudPermissions = {
        create: 'users.add',
        update: 'users.update',
        delete: 'users.delete'
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
        this._modalService.open(UserFormComponent, {
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

        this._modalService.open(UserFormComponent, {
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
            }, () => { }, () => {
                object._loading = false;
            });
    }

    /**
     * sobrescribo el metodo para eliminar por rest
     * @param ev evento
     */
    delete(ev: any) {
        this._soundService.alert();

        const selectedObjects = this.getSelectedObjects();

        this._modalService.open(DialogCrudDeleteComponent, {
            data: selectedObjects.map(item => {
                return this.getItemDeleteText(item);
            }),
            closeOnNavigation: true
        })
            .afterClosed()
            .pipe(take(1))
            .subscribe((confirm: boolean) => {
                if (confirm) {

                    this.state.isSaving = true;

                    const arrayObservables = [];

                    const errors = [];

                    selectedObjects.forEach(object => {
                        arrayObservables.push(this._http.delete(this.apiUrl + 'User/' + object.id)
                            .pipe(catchError(e => of(e)))
                            .pipe(map(res => {
                                if (hasValue(res.status)) {
                                    if (res.status !== 200) {
                                        errors.push(res.error);
                                    }
                                }
                                return res;
                            })));
                    });

                    forkJoin(arrayObservables)
                        .pipe(take(1))
                        .subscribe((res: any) => {

                            if (hasValue(errors)) {
                                this.state.isSaving = false;

                                this._snackBar.deleteFail();

                                this.getList();

                            } else {

                                this.state.isSaving = false;

                                this._snackBar.deleted();

                                this.getList();

                                if ((window as any).debug) {
                                    console.group('%c[debug]', 'color: limegreen;');
                                    console.groupCollapsed('delete_ok');
                                    console.log(res);
                                    console.groupEnd();
                                    console.groupEnd();
                                }

                            }

                        }, () => { }, () => { });

                }
            });
    }

}
