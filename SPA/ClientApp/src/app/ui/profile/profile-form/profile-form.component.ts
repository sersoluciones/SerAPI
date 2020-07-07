import { OidcUser } from 'src/app/common/auth/oidc';
import { Component } from '@angular/core';
import { Validators } from '@angular/forms';
import { BaseForm } from 'src/app/common/base/form';
import { hasValue, Patterns } from '@sersol/ngx';
import { take } from 'rxjs/operators';

@Component({
    selector: 'app-profile-form',
    templateUrl: './profile-form.component.html',
    styleUrls: ['./profile-form.component.scss']
})
export class ProfileFormComponent extends BaseForm {

    oidcUser: OidcUser;

    texts: any;

    finishForm() {
        this.oidcUser.name = this.modelForm.value.name;
        this.oidcUser.last_name = this.modelForm.value.last_name;
        this.oidcUser.email = this.modelForm.value.email;
        this.oidcUser.phone_number = this.modelForm.value.phone_number;
        this.oidcUser.name_initial = hasValue(this.oidcUser.name) ? this.oidcUser.name[0] : this.oidcUser.username[0];
        this.oidcUser.name_to_show = hasValue(this.oidcUser.name) ? this.oidcUser.name : this.oidcUser.username;
        localStorage.setItem('oidc_user', JSON.stringify(this.oidcUser));

        this._soundService.notify();

        this._snackBar.profileUpdated();

        setTimeout(() => {
            window.location.reload();
        }, 1000);
    }

    processSubmit() {
        this._http.put('/api/User/UpdateProfile', this.modelForm.value)
        .pipe(take(1))
        .subscribe((response: any) => {
            console.log(response);
            this.finishForm();
        }, (reject) => {
            this._dialogService.error(reject);
        });
    }

    init() {
        this.oidcUser = this._auth.oidcUser;

        this.modelForm = this._fb.group({
            name: [this.oidcUser?.name, Validators.required],
            last_name: [this.oidcUser?.last_name, [Validators.required]],
            email: [this.oidcUser?.email, [Validators.required, Validators.pattern(Patterns.EMAIL)]],
            phone_number: [this.oidcUser?.phone_number]
        });

        this.state.loading = false;
    }

}
