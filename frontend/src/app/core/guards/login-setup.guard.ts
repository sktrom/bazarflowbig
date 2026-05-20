import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, map, catchError, of } from 'rxjs';
import { SetupApiService } from '../services/setup-api.service';

@Injectable({ providedIn: 'root' })
export class LoginSetupGuard implements CanActivate {
  constructor(private setupApi: SetupApiService, private router: Router) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.setupApi.getStatus().pipe(
      map(res => {
        if (!res.setupCompleted) {
          return this.router.parseUrl('/setup');
        }
        return true;
      }),
      catchError(() => {
        // Safe fallback: allow login page access if API fails
        return of(true);
      })
    );
  }
}
