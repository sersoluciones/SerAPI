import { TranslateService } from '@ngx-translate/core';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { take } from 'rxjs/operators';
import { SoundService } from '../sound/sound.service';

@Injectable({
    providedIn: 'root'
})
export class SnackbarService {

    texts: any;

    constructor(private _snackBar: MatSnackBar, _translate: TranslateService, private soundService: SoundService) {
        _translate.get(['update_success', 'create_success', 'profile_updated', 'delete_ok', 'delete_fail'])
            .pipe(take(1))
            .subscribe((texts) => {
                this.texts = texts;
            });
    }

    created() {
        this.soundService.notify();

        this._snackBar.open(this.texts.create_success, '', {
            horizontalPosition: 'right',
            duration: 4000
        });
    }

    updated() {
        this.soundService.notify();

        this._snackBar.open(this.texts.update_success, '', {
            horizontalPosition: 'right',
            duration: 4000
        });
    }

    deleted() {
        this.soundService.notify();

        this._snackBar.open(this.texts.delete_ok, '', {
            horizontalPosition: 'right',
            duration: 4000
        });
    }

    deleteFail() {
        this.soundService.notify();

        this._snackBar.open(this.texts.delete_fail, '', {
            horizontalPosition: 'right',
            duration: 4000
        });
    }

    profileUpdated() {
        this.soundService.notify();

        this._snackBar.open(this.texts.profile_updated, '', {
            horizontalPosition: 'right'
        });
    }

}
