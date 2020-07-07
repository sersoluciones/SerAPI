import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Patterns, CustomValidators } from '@sersol/ngx';
import { DialogService } from 'src/app/common/dialog/dialog.service';
import { take } from 'rxjs/operators';

@Component({
    selector: 'app-change-password',
    templateUrl: './change-password.component.html',
    styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {

    loading = false;

    passwordForm = this._fb.group({
        old_password: ['', Validators.required],
        new_password: [null, [Validators.required, Validators.pattern(Patterns.PASSWORD)]],
        confirm_password: ['', [Validators.required]]
    },
        {
            validators: CustomValidators.match('new_password', 'confirm_password')
        });

    constructor(private _fb: FormBuilder, private _http: HttpClient, private _dialog: DialogService) { }

    /**
     * @description Oculta/muestra contraseña
     */
    toggleVisibility(ev: any): void {

        if (ev.target.innerText === 'visibility_off') {
            ev.target.parentElement.parentElement.children[0].type = 'text';
            ev.target.innerText = 'visibility';
        } else {
            ev.target.parentElement.parentElement.children[0].type = 'password';
            ev.target.innerText = 'visibility_off';
        }

    }

    submit() {

        this.loading = true;

        this._http.post('/api/User/ChangePassword', this.passwordForm.value)
        .pipe(take(1))
        .subscribe(
            () => {
                this.loading = false;
                this._dialog.simple({
                    message: 'password_updated'
                });

                this.passwordForm.reset();
            },
            (reject) => {
                this.loading = false;
                this._dialog.error(reject);
            }
        );
    }

}
