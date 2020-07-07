import { CrudState, FilterField, CrudPermissions, GraphQuery, ExportSettings } from '../interfaces/base';
import { AuthService } from 'src/app/common/auth/auth.service';
import { FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { hasValue, objHasValue, ClaimsService, AwsService, WhenScrolledDirective } from '@sersol/ngx';
import { OnInit, AfterViewInit, HostBinding, OnDestroy } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { PaginationParams, PaginationState } from '../interfaces/base';
import { BaseEventService } from './base-event.service';
import { Subject, ReplaySubject, Observable } from 'rxjs';
import { DownloadService } from '../utils/download.service';
import { debounceTime, distinctUntilChanged, takeUntil, take, switchMap, catchError, finalize } from 'rxjs/operators';
import { DialogCrudDeleteComponent } from 'src/app/ui/crud/dialog-crud-delete/dialog-crud-delete.component';
import { CrudToolbarComponent } from 'src/app/ui/crud/crud-toolbar/crud-toolbar.component';
import { CrudFilterToolsComponent } from 'src/app/ui/crud/crud-filter-tools/crud-filter-tools.component';
import { CrudAddObjectComponent } from 'src/app/ui/crud/crud-add-object/crud-add-object.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SnackbarService } from '../snackbar/snackbar.service';

/**
 * Clase base para crear listados consultando via GraphQL con creación, edición y eliminación en masa sobre los resultados
 */
export class BaseCrud implements OnInit, AfterViewInit, OnDestroy {

    @HostBinding('class') class = 'crud';
    scrollEvent: WhenScrolledDirective;
    toolbar: CrudToolbarComponent;
    toolbarFilter: CrudFilterToolsComponent;
    toolbarAdd: CrudAddObjectComponent;

    protected listDataSub: Subject<any> = new Subject();
    notifierDestroySubs: ReplaySubject<any> = new ReplaySubject();
    state: CrudState;
    permissions: CrudPermissions;
    graphQuery: GraphQuery;
    restUrl: string;
    PKField = 'id';

    /**
     * Download
     */
    exportSettings: ExportSettings;

    /**
     * Pagination
     */
    filters: FilterField[];
    filterFG: FormGroup;
    pagination: PaginationState;
    paginationParams: PaginationParams = {
        pageSize: 20,
        orderBy: '',
        sortReverse: false
    };

    hasValue = hasValue;
    objHasValue = objHasValue;

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService,
                protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService, public claimService: ClaimsService,
                protected baseService: BaseEventService, public aws: AwsService, protected _downloadService: DownloadService, public apiUrl: string, public graphQLUrl: string) { }

    getListData(): Observable<any> {
        throw new Error('Method not implemented: BaseCrud.getListData()');
    }

    processListData(response: any): void {
        throw new Error('Method not implemented: BaseCrud.processListaData()');
    }

    protected _setListDataSub() {

        this.listDataSub.pipe(
                switchMap(() => this.getListData()
                    .pipe(
                        catchError((e: any) => {
                            this._dialogService.error(e);
                            this.pagination.gettingMore = false;
                            return e;
                        })
                    )
                ),
                takeUntil(this.notifierDestroySubs),
                finalize(() => this.pagination.gettingMore = false)
            )
            .subscribe((response: any) => {
                this.processListData(response);
                this.pagination.gettingMore = false;
            });

    }

    protected reInitList(): void {
        throw new Error('Method not implemented: BaseCrud.reInitList()');
    }

    getList() {
        if (!this.state.isSaving) {
            this.pagination.gettingMore = true;
            this.state.selectedObjects = 0;
            this.pagination.currentPage = 1;
            this.reInitList();
            this.listDataSub.next();
        }
    }

    /**
     * Obtiene la siguiente página de resultados y agrega las filas obtenidas al fila de objectList
     */
    getMoreRows() {
        if (this.pagination.hasNextPage && !this.pagination.gettingMore && !this.state.isSaving) {
            this.pagination.gettingMore = true;
            this.pagination.currentPage++;
            this.listDataSub.next();
        }
    }

    getItemDeleteText(ev: any): string {
        throw new Error('Method not implemented: BaseCrud.getItemDeleteText()');
    }

    deleteParams(objects: any[]): string[] {
        const graph_params = [];
        let counter = 0;

        objects.forEach(element => {
            const PK = element[this.PKField];

            if (typeof (PK) === 'string') {
                graph_params.push(`d${++counter}: ${this.graphQuery.delete.name}(${this.graphQuery.delete.pk}: "${PK}") { ${this.PKField} }`);
            } else {
                graph_params.push(`d${++counter}: ${this.graphQuery.delete.name}(${this.graphQuery.delete.pk}: ${PK}) { ${this.PKField} }`);
            }
        });

        return graph_params;

    }

    getSelectedObjects(): any[] {
        throw new Error('Method not implemented: BaseCrud.getSelectedObjects()');
    }

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

                this._http.post(this.graphQLUrl,
                {
                    query: `mutation{${this.deleteParams(selectedObjects).join('')}}`
                })
                .pipe(take(1))
                .subscribe((res: any) => {
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
                }, () => {
                    this.state.isSaving = false;

                    this._snackBar.deleteFail();

                    this.getList();
                });
            }
        });
    }

    downloadList(format: string) {

        if (!this.state.isDownloading) {

            this.state.isDownloading = true;

            this._http.get(`${this.apiUrl}xlsx/download/${this.exportSettings.modelName}`, {
                params: {
                    format,
                    modelName: this.exportSettings.modelName,
                    columnStr: this.exportSettings.columns.join(','),
                    download: 'true'
                }
            })
                .pipe(take(1))
                .subscribe((res: string) => {
                    this.state.isDownloading = false;
                    this._downloadService.get('/' + res);
                }, (e: HttpErrorResponse) => {
                    this.state.isDownloading = false;
                    this._dialogService.error(e);
                });

        }

    }

    protected _initFilters(): void {
        this.filterFG = new FormGroup({});

        this.filterFG.addControl('all', new FormControl(null));

        this.filters.forEach(element => {
            this.filterFG.addControl(element.field, new FormControl(null));
        });

        this.filterFG.valueChanges
            .pipe(
                debounceTime(500),
                distinctUntilChanged((prev, curr) => {
                    return JSON.stringify(prev) === JSON.stringify(curr);
                }),
                takeUntil(this.notifierDestroySubs)
            )
            .subscribe(() => {
                this.getList();
            });
    }

    init(): void { }
    private _init(): void {
        this.pagination = {
            gettingMore: true,
            currentPage: 1,
            rowCount: 0,
            hasNextPage: true,
            showFilters: false,
            sortType: this.paginationParams.orderBy,
            sortReverse: this.paginationParams.sortReverse,
            sortReverseSymbol: this.paginationParams.sortReverse ? 'DESC' : 'ASC'
        };

        this.state = {
            formMode: false,
            isSaving: false,
            isDownloading: false,
            selectedObjects: 0
        };

        this._translate.get(['delete_fail', 'delete_ok']).pipe(take(1)).subscribe((texts: any) => {
            this.state.texts = texts;
        });

        this._initFilters();
        this._setListDataSub();
    }

    afterInit() { }
    private _afterInit() {

        this.scrollEvent.callback.pipe(takeUntil(this.notifierDestroySubs)).subscribe(() => {
            this.getMoreRows();
        });

        this.toolbar.list.pipe(takeUntil(this.notifierDestroySubs)).subscribe(() => {
            this.getList();
        });

        this.toolbar.download.pipe(takeUntil(this.notifierDestroySubs)).subscribe((format: string) => {
            this.downloadList(format);
        });

        this.getList();
    }

    ngOnInit() {
        this._init();
        this.init();
    }

    ngAfterViewInit() {
        this._afterInit();
        this.afterInit();
    }

    ngOnDestroy() {
        this.notifierDestroySubs.next();
        this.notifierDestroySubs.complete();
    }
}
