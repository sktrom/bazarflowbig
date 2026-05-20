import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, map, catchError, of } from 'rxjs';
import { SetupApiService } from '../services/setup-api.service';

@Injectable({ providedIn: 'root' })
export class SetupGuard implements CanActivate {
  constructor(private setupApi: SetupApiService, private router: Router) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.setupApi.getStatus().pipe(
      map(res => {
        if (res.setupCompleted) {
          return this.router.parseUrl('/login');
        }
        return true;
      }),
      catchError(() => {
        // Allow entering setup page on API error so the component can handle it
        return of(true);
      })
    );
  }
}
