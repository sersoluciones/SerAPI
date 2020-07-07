import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ProductCrudComponent } from './modules/inventory/product/list/product-crud.component';
import { PermissionGuardService } from './common/auth/permission-guard.service';
import { BrandComponent } from './modules/inventory/brand/brand.component';
import { CategoryListComponent } from './modules/inventory/category/list/category.component';
import { CountryComponent } from './modules/parameterization/country/country.component';
import { CommonOptionComponent } from './modules/parameterization/common-option/common-option.component';
import { RoleComponent } from './modules/security/role/list/role.component';
import { PermissionComponent } from './modules/security/permission/permission.component';
import { ErrorPermissionComponent } from './ui/403/error-permission/error-permission.component';
import { StoreListComponent } from './modules/inventory/store/list/store.component';
import { UserComponent } from './modules/security/user/list/user.component';
import { CompanyComponent } from './modules/security/company/list/company.component';

const routes: Routes = [
    {
        path: 'inventory',
        children: [{
            path: 'product', component: ProductCrudComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'products.view'
            }
        }, {
            path: 'brand', component: BrandComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'brands.view'
            }
        }, {
            path: 'category', component: CategoryListComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'categories.view'
            }
        }, {
            path: 'store', component: StoreListComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'stores.view'
            }
        }]
    },
    {
        path: 'parameterization',
        children: [{
            path: 'country', component: CountryComponent,
        }, {
            path: 'common-option', component: CommonOptionComponent,
        }]
    },
    {
        path: 'security',
        children: [{
            path: 'user', component: UserComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'users.view'
            }
        },
        {
            path: 'company', component: CompanyComponent,
            canActivate: [PermissionGuardService],
            data: {
                expectedPermission: 'companies.view'
            }
        }, {
            path: 'role', component: RoleComponent,
        }, {
            path: 'permission', component: PermissionComponent,
        }]
    },
    {
        path: 'error',
        children: [{
            path: '403', component: ErrorPermissionComponent
        }]
    }
];

@NgModule({
    imports: [
        RouterModule.forRoot(
            routes,
            { enableTracing: false }
        )
    ],
    exports: [RouterModule]
})
export class AppRoutingModule { }
