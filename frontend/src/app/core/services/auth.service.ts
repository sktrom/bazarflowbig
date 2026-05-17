import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private sessionService: SessionService) {
    // Initialize based on session presence
    this.isAuthenticatedSubject.next(!!this.sessionService.getSessionId());
  }

  setAuthenticated(isAuthenticated: boolean): void {
    this.isAuthenticatedSubject.next(isAuthenticated);
  }

  isLoggedIn(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  logout(): void {
    this.sessionService.clearSession();
    this.setAuthenticated(false);
  }
}
