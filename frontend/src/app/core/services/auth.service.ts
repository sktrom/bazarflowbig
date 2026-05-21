import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { SessionService } from './session.service';
import { AuthApiService } from './auth-api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(
    private sessionService: SessionService,
    private authApiService: AuthApiService
  ) {
    // Initialize based on session token presence
    this.isAuthenticatedSubject.next(!!this.sessionService.getSessionToken());
  }

  setAuthenticated(isAuthenticated: boolean): void {
    this.isAuthenticatedSubject.next(isAuthenticated);
  }

  isLoggedIn(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  logout(): Observable<any> {
    return this.authApiService.logout().pipe(
      catchError(() => of(null)), // Ignore errors (e.g. 401)
      tap(() => {
        this.sessionService.clearSession();
        this.setAuthenticated(false);
      })
    );
  }

  logoutLocalOnly(): void {
    this.sessionService.clearSession();
    this.setAuthenticated(false);
  }
}
