import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private router: Router, private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        const normalizedError = this.normalizeError(error);

        if (error.status === 401) {
          // Unauthorized, session might be invalid/expired
          this.authService.logout();
          this.router.navigate(['/unauthorized']);
        } else if (error.status === 403) {
          // Forbidden, missing screen permission
          this.router.navigate(['/unauthorized']);
        } else {
          if (!environment.production) {
            console.error('HTTP Error:', {
              status: error.status,
              error: normalizedError.error?.error,
              traceId: normalizedError.error?.traceId
            });
          }
        }
        return throwError(() => normalizedError);
      })
    );
  }

  private normalizeError(error: HttpErrorResponse): HttpErrorResponse {
    const body = this.normalizeBody(error);
    return new HttpErrorResponse({
      error: body,
      headers: error.headers,
      status: error.status,
      statusText: error.statusText,
      url: error.url ?? undefined
    });
  }

  private normalizeBody(error: HttpErrorResponse): { error: string; message: string; traceId?: string } {
    const raw = error.error;
    const rawBody = raw && typeof raw === 'object' ? raw as any : {};
    const code = typeof rawBody.error === 'string' && rawBody.error.trim()
      ? rawBody.error
      : this.defaultErrorCode(error.status);
    const message = typeof rawBody.message === 'string' && rawBody.message.trim()
      ? rawBody.message
      : this.defaultMessage(error.status);
    const traceId = typeof rawBody.traceId === 'string' && rawBody.traceId.trim()
      ? rawBody.traceId
      : undefined;

    return { error: code, message, traceId };
  }

  private defaultErrorCode(status: number): string {
    if (status === 0) return 'NETWORK_ERROR';
    if (status === 400) return 'BAD_REQUEST';
    if (status === 500) return 'INTERNAL_SERVER_ERROR';
    return 'HTTP_ERROR';
  }

  private defaultMessage(status: number): string {
    if (status === 0) return 'تعذر الاتصال بالخادم.';
    if (status === 400) return 'تعذر تنفيذ الطلب. تحقق من البيانات.';
    if (status === 500) return 'حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.';
    return 'حدث خطأ أثناء تنفيذ العملية.';
  }
}
