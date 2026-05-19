import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PermissionsService } from '../../core/services/permissions.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

interface MenuItem {
  label: string;
  route: string;
  screenKey: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="bg-surface h-full shadow-sm flex flex-col">
      <div class="p-6 border-b border-slate-200">
        <h1 class="text-2xl font-bold text-primary">Bazarflow</h1>
      </div>
      <nav class="flex-1 overflow-y-auto py-4">
        <ul class="space-y-1 px-3">
          <ng-container *ngFor="let item of menuItems">
            <li *ngIf="hasPermission(item.screenKey) | async">
              <a [routerLink]="item.route" 
                 routerLinkActive="bg-primary text-white" 
                 class="block px-4 py-2 rounded-md text-slate-700 hover:bg-slate-100 transition-colors">
                {{ item.label }}
              </a>
            </li>
          </ng-container>
        </ul>
      </nav>
    </aside>
  `
})
export class SidebarComponent implements OnInit {
  menuItems: MenuItem[] = [
    { label: 'الرئيسية', route: '/dashboard', screenKey: 'Dashboard' },
    { label: 'نقطة البيع', route: '/cashier', screenKey: 'Sales' },
    { label: 'الفواتير', route: '/invoices', screenKey: 'Invoices' },
    { label: 'المخزون', route: '/inventory', screenKey: 'Inventory' },
    { label: 'الموردون', route: '/suppliers', screenKey: 'Purchases' },
    { label: 'المشتريات', route: '/purchases', screenKey: 'Purchases' },
    { label: 'مركز القرارات', route: '/inventory/action-center', screenKey: 'Inventory' },
    { label: 'المنتجات', route: '/products', screenKey: 'Products' },
    { label: 'العروض', route: '/offers', screenKey: 'Offers' },
    { label: 'التقارير', route: '/reports', screenKey: 'Reports' },
    { label: 'الإعدادات', route: '/settings', screenKey: 'Settings' }
  ];

  constructor(private permissionsService: PermissionsService) {}

  ngOnInit() {}

  hasPermission(key: string): Observable<boolean> {
    // We observe the permissions list and return true if key exists.
    return this.permissionsService.permissions$.pipe(
      map(perms => perms.includes(key))
    );
  }
}
