import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SessionService } from '../services/session.service';

@Injectable()
export class SessionInterceptor implements HttpInterceptor {
  constructor(private sessionService: SessionService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const sessionId = this.sessionService.getSessionId();

    if (sessionId) {
      // Inject X-Session-Id header for protected API requests. No JWT logic.
      const clonedReq = req.clone({
        headers: req.headers.set('X-Session-Id', sessionId)
      });
      return next.handle(clonedReq);
    }

    return next.handle(req);
  }
}
