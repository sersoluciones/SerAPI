import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { ClaimsService } from '@sersol/ngx';

@Injectable({
  providedIn: 'root'
})
export class PermissionGuardService implements CanActivate {

  constructor(private claimService: ClaimsService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {

    const expectedPermission = route.data.expectedPermission;

    if (!this.claimService.hasPermission(expectedPermission)) {
      this.router.navigate(['error/403']);
      return false;
    }

    return true;
  }

}
