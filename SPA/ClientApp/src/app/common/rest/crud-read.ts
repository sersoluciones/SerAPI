import { hasValue, ClaimsService, AwsService, WhenScrolledDirective, objHasValue } from '@sersol/ngx';
import { HttpParams, HttpClient } from '@angular/common/http';
import { PaginationParams } from '../interfaces/base';
import { BaseCrudRead } from '../base/crud-read';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { DialogService } from '../dialog/dialog.service';
import { SoundService } from '../sound/sound.service';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { BaseEventService } from '../base/base-event.service';
import { DownloadService } from '../utils/download.service';
import { Inject, ViewChild } from '@angular/core';
import { getQueryParams } from './utils';
import { Observable } from 'rxjs';

/**
 * Clase base para crear listados consultando via Rest
 */
export class RestCrudRead extends BaseCrudRead {

    @ViewChild(WhenScrolledDirective) scrollEvent: WhenScrolledDirective;

    constructor(protected _fb: FormBuilder, protected _http: HttpClient, protected _modalService: MatDialog, protected _dialogService: DialogService,
                protected _soundService: SoundService, protected _translate: TranslateService, protected _auth: AuthService, public claimService: ClaimsService,
                protected baseService: BaseEventService, public aws: AwsService, protected _downloadService: DownloadService, @Inject('API_URL') public apiUrl: string,
                @Inject('GRAPHQL_URL') public graphQLUrl: string) {
        super(_fb, _http, _modalService, _dialogService, _soundService, _translate, _auth, claimService, baseService, aws, _downloadService, apiUrl, graphQLUrl);
    }

    getListData(): Observable<any> {
        const params = getQueryParams(this);

        return this._http.get(this.apiUrl + this.restUrl, {
            params
        });
    }

    processListData(response: any): void {
        response.results.forEach((item: any) => {
            item._isSelected = false;
            this.objectList.push(item);
        });

        this.pagination.rowCount = response.row_count;
        this.pagination.hasNextPage = response.has_next_page;
    }

}
