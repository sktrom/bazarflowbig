import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { SetupGuard } from './core/guards/setup.guard';
import { LoginSetupGuard } from './core/guards/login-setup.guard';
import { ScreenPermissionGuard } from './core/guards/screen-permission.guard';
import { ShellComponent } from './shell/shell.component';
import { UnauthorizedComponent } from './core/pages/unauthorized.component';
import { NotFoundComponent } from './core/pages/not-found.component';

export const routes: Routes = [
  // Default: redirect to login
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // Public Setup
  {
    path: 'setup',
    canActivate: [SetupGuard],
    loadComponent: () =>
      import('./features/setup/setup.component').then(m => m.SetupComponent)
  },

  // Public: Login
  {
    path: 'login',
    canActivate: [LoginSetupGuard],
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
      },
      {
        path: 'inventory',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Inventory' },
        loadComponent: () =>
          import('./features/inventory/pages/inventory-list.component').then(m => m.InventoryListComponent)
      },
      {
        path: 'suppliers',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Purchases' },
        loadComponent: () =>
          import('./features/suppliers/pages/suppliers.component').then(m => m.SuppliersComponent)
      },
      {
        path: 'purchases',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Purchases' },
        loadComponent: () =>
          import('./features/purchases/pages/purchases.component').then(m => m.PurchasesComponent)
      },
      {
        path: 'inventory/action-center',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Inventory' },
        loadComponent: () =>
          import('./features/inventory/pages/action-center/action-center.component').then(m => m.ActionCenterComponent)
      },
      {
        path: 'offers',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Offers' },
        loadComponent: () =>
          import('./features/offers/pages/offers.component').then(m => m.OffersComponent)
      },
      {
        path: 'reports',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Reports' },
        loadComponent: () =>
          import('./features/reports/pages/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'settings',
        canActivate: [ScreenPermissionGuard],
        data: { screenKey: 'Settings' },
        loadComponent: () =>
          import('./features/settings/pages/settings.component').then(m => m.SettingsComponent)
      }
    ]
  },

  // Public/Error routes
  { path: 'unauthorized', component: UnauthorizedComponent },
  { path: '**', component: NotFoundComponent }
];
