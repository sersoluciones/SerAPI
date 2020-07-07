import { AuthService } from 'src/app/common/auth/auth.service';
import { FormBuilder } from '@angular/forms';
import { hasValue, ClaimsService, WhenScrolledDirective, AwsService } from '@sersol/ngx';
import { ViewChild, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { GraphQuery } from '../interfaces/base';
import { BaseEventService } from '../base/base-event.service';
import { DownloadService } from '../utils/download.service';
import { getGraphQueryParams } from './utils';
import { Observable } from 'rxjs';
import { BaseCrudRead } from '../base/crud-read';
import { CrudToolbarComponent } from 'src/app/ui/crud/crud-toolbar/crud-toolbar.component';
import { CrudAddObjectComponent } from 'src/app/ui/crud/crud-add-object/crud-add-object.component';
import { SnackbarService } from '../snackbar/snackbar.service';

/**
 * Clase base para crear listados consultando via GraphQL con creación, edición y eliminación en masa sobre los resultados
 */
export class GraphQLCrudRead extends BaseCrudRead {

    @ViewChild(WhenScrolledDirective) scrollEvent: WhenScrolledDirective;
    @ViewChild(CrudToolbarComponent) toolbar: CrudToolbarComponent;
    @ViewChild(CrudAddObjectComponent) toolbarAdd: CrudAddObjectComponent;
    graphQuery: GraphQuery;

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService, protected _snackBar: SnackbarService,
                protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService, public claimService: ClaimsService,
                protected baseService: BaseEventService, public aws: AwsService, protected _downloadService: DownloadService, @Inject('API_URL') public apiUrl: string,
                @Inject('GRAPHQL_URL') public graphQLUrl: string) {
        super(_fb, _http, _modalService, _dialogService, _snackBar, _soundService, _translate, _auth, claimService, baseService, aws, _downloadService, apiUrl, graphQLUrl);
    }

    getListData(): Observable<any> {
        const params = getGraphQueryParams(this);

        return this._http.post(this.graphQLUrl,
        `query {
            ${this.graphQuery.list.name}${hasValue(params) ? `(${params.join(', ')})` : ''} {
                ${this.graphQuery.list.fields.join(',')}
            }
        }`,
        {
            headers: {
                'Content-Type': 'application/graphql'
            }
        });
    }

    processListData(response: any): void {
        response.data[this.graphQuery.list.name].forEach((item: any) => {
            item._isSelected = false;
            this.objectList.push(item);
        });

        if (response.hasOwnProperty('extensions')) {
            this.pagination.rowCount = response.extensions[this.graphQuery.list.name]?.row_count;
            this.pagination.hasNextPage = response.extensions[this.graphQuery.list.name]?.has_next_page;
        }
    }
}
