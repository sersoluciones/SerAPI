import { HttpErrorResponse } from '@angular/common/http';

export interface SimpleMessage {
    message: string;
    closeButton?: boolean;
}

export interface ErrorMessage {
    reject: HttpErrorResponse;
    closeButton?: boolean;
}

export interface ErrorMessageItem {
    code?: string;
    description?: any;
}
