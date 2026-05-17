import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { ScreenPermissionGuard } from './core/guards/screen-permission.guard';
import { ShellComponent } from './shell/shell.component';
import { UnauthorizedComponent } from './core/pages/unauthorized.component';
import { NotFoundComponent } from './core/pages/not-found.component';

export const routes: Routes = [
  // Default: redirect to login
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // Public: Login
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then(m => m.LoginComponent)
  },

  // Protected Shell Routes
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuard],
    children: [
      // Cashier is the primary route (cashier-first system)
      {
        path: 'cashier',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Sales' },
        loadComponent: () =>
          import('./features/cashier/cashier.component').then(m => m.CashierComponent)
      },
      {
        path: 'products',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Products' },
        loadComponent: () =>
          import('./features/products/products.component').then(m => m.ProductsComponent)
      },
      {
        path: 'invoices',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Invoices' },
        loadComponent: () =>
          import('./features/invoices/invoices.component').then(m => m.InvoicesComponent)
      }
    ]
  },

  // Public/Error routes
  { path: 'unauthorized', component: UnauthorizedComponent },
  { path: '**', component: NotFoundComponent }
];

