import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private router: Router, private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Unauthorized, session might be invalid/expired
          this.authService.logout();
          this.router.navigate(['/unauthorized']);
        } else if (error.status === 403) {
          // Forbidden, missing screen permission
          this.router.navigate(['/unauthorized']);
        } else {
          // General error handling (toast notification could be triggered here)
          console.error('HTTP Error:', error.message);
        }
        return throwError(() => error);
      })
    );
  }
}
