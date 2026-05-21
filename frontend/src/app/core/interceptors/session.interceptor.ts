import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SessionService } from '../services/session.service';

@Injectable()
export class SessionInterceptor implements HttpInterceptor {
  constructor(private sessionService: SessionService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const bypassUrls = [
      '/api/auth/login',
      '/api/setup/status',
      '/api/setup/complete',
      '/api/settings/public'
    ];

    const shouldBypass = bypassUrls.some(url => req.url.includes(url));
    const sessionToken = this.sessionService.getSessionToken();

    if (sessionToken && !shouldBypass) {
      const clonedReq = req.clone({
        headers: req.headers.set('X-Session-Token', sessionToken)
      });
      return next.handle(clonedReq);
    }

    return next.handle(req);
  }
}
