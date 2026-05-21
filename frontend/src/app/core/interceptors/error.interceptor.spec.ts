import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { ErrorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

describe('ErrorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let consoleErrorSpy: jasmine.Spy;
  let originalProduction: boolean;

  beforeEach(() => {
    originalProduction = environment.production;
    authServiceSpy = jasmine.createSpyObj('AuthService', ['logout']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    consoleErrorSpy = spyOn(console, 'error');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    environment.production = originalProduction;
    httpMock.verify();
  });

  it('should map generic 500 to a safe Arabic message', (done) => {
    http.get('/api/test').subscribe({
      error: (error) => {
        expect(error.error.error).toBe('INTERNAL_SERVER_ERROR');
        expect(error.error.message).toBe('حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.');
        done();
      }
    });

    httpMock.expectOne('/api/test').flush({}, { status: 500, statusText: 'Server Error' });
  });

  it('should map network error to connection message', (done) => {
    http.get('/api/test').subscribe({
      error: (error) => {
        expect(error.error.error).toBe('NETWORK_ERROR');
        expect(error.error.message).toBe('تعذر الاتصال بالخادم.');
        done();
      }
    });

    httpMock.expectOne('/api/test').error(new ProgressEvent('Network error'));
  });

  it('should preserve known backend error code and message for local mappings', (done) => {
    http.get('/api/test').subscribe({
      error: (error) => {
        expect(error.error.error).toBe('PRODUCT_NOT_FOUND');
        expect(error.error.message).toBe('المنتج غير موجود.');
        expect(error.error.traceId).toBe('trace-123');
        done();
      }
    });

    httpMock.expectOne('/api/test').flush(
      { error: 'PRODUCT_NOT_FOUND', message: 'المنتج غير موجود.', traceId: 'trace-123' },
      { status: 404, statusText: 'Not Found' });
  });

  it('should not log to console in production', (done) => {
    environment.production = true;

    http.get('/api/test').subscribe({
      error: () => {
        expect(consoleErrorSpy).not.toHaveBeenCalled();
        done();
      }
    });

    httpMock.expectOne('/api/test').flush({}, { status: 500, statusText: 'Server Error' });
  });
});
