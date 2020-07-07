import { take } from 'rxjs/operators';
import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AttachmentService {

    constructor(protected _http: HttpClient, @Inject('API_URL') public apiUrl: string) { }

    upload(file: Blob, model?: string): Observable<any> {

        const formData = new FormData();
        formData.append('file', file);
        formData.append('model', model);

        return this._http.post(`${this.apiUrl}Attachment`, formData);

    }

}
