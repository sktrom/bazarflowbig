import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { PermissionsService } from '../services/permissions.service';

@Injectable({ providedIn: 'root' })
export class ScreenPermissionGuard implements CanActivate {
  constructor(private permissionsService: PermissionsService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree | Observable<boolean | UrlTree> | Promise<boolean | UrlTree> {
    const screenKey = route.data['screenKey'];
    
    if (screenKey) {
      if (Array.isArray(screenKey)) {
        if (screenKey.some(k => this.permissionsService.hasPermission(k))) {
          return true;
        }
      } else if (typeof screenKey === 'string' && this.permissionsService.hasPermission(screenKey)) {
        return true;
      }
    }
    
    // Forbidden
    return this.router.parseUrl('/unauthorized');
  }
}
