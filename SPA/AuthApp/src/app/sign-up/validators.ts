import { ValidatorFn, AbstractControl, ValidationErrors, AsyncValidator } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import {map, catchError} from 'rxjs/operators';

/**
 * @description Validadores asincronos
 */
@Injectable({ providedIn: 'root' })
export class UniqueEmail implements AsyncValidator {
  constructor( private http: HttpClient) { }

  validate(control: AbstractControl): Promise<ValidationErrors> | Observable<ValidationErrors> {

    const validationError: ValidationErrors = { emailTaken: true };
    const url = '/User/VerifyEmail/' + control.value;
    return this.http.get(url).pipe(
      map(response => {
        console.log(response, 'async validator');

        if (response == null) {
          return validationError;
        }
      }),
      catchError(() => of(null))
    );
  }
}
