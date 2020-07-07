import { ErrorMessage, ErrorMessageItem } from './../message';
import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { hasValue } from '@sersol/ngx';

@Component({
    selector: 'app-error',
    templateUrl: './error.component.html',
    styleUrls: ['./error.component.scss']
})
export class DialogErrorComponent {

    closeButton = true;
    error_message = '';

    constructor(public dialogRef: MatDialogRef<DialogErrorComponent>, @Inject(MAT_DIALOG_DATA) public data: ErrorMessage) {
        if (hasValue(data.closeButton)) {
            this.closeButton = data.closeButton;
        }

        console.error(data.reject);

        if (hasValue(data.reject.error)) {

            switch (data.reject.status) {

                case 400:

                    if (data.reject.error.hasOwnProperty('succeeded') && data.reject.error.hasOwnProperty('errors')) {

                        data.reject.error.errors.forEach((error: ErrorMessageItem) => {
                            this.error_message += `<div><strong>${ error.code }</strong><ul>`;

                            if (Array.isArray(error.description)) {

                                error.description.forEach((msj: string) => {
                                    this.error_message += `<li>${ msj }</li>`;
                                });

                            } else {
                                this.error_message += `<li>${ error.description }</li>`;
                            }

                        });

                    } else {

                        for (const [key, error] of Object.entries(data.reject.error)) {
                            this.error_message += `<div><strong>${ key }</strong><ul>`;

                            if (Array.isArray(error)) {

                                error.forEach((msj: string) => {
                                    this.error_message += `<li>${ msj }</li>`;
                                });

                            } else {
                                this.error_message += `<li>${ error }</li>`;
                            }
                        }

                    }

                    break;

                case 403:
                    this.error_message = data.reject.error?.message;
                    break;
            }

        }
    }

    close() {
        this.dialogRef.close();
    }

}
