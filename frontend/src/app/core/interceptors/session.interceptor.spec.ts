import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { SessionInterceptor } from './session.interceptor';
import { SessionService } from '../services/session.service';

describe('SessionInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let sessionService: jasmine.SpyObj<SessionService>;

  beforeEach(() => {
    const sessionSpy = jasmine.createSpyObj('SessionService', ['getSessionToken']);
    
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: SessionService, useValue: sessionSpy },
        { provide: HTTP_INTERCEPTORS, useClass: SessionInterceptor, multi: true }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
    sessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should inject X-Session-Token header when session token exists', () => {
    sessionService.getSessionToken.and.returnValue('mock-token-123');

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('X-Session-Token')).toBeTrue();
    expect(req.request.headers.get('X-Session-Token')).toEqual('mock-token-123');
    expect(req.request.headers.has('X-Session-Id')).toBeFalse();
    req.flush({});
  });

  it('should not inject session headers when session token is missing', () => {
    sessionService.getSessionToken.and.returnValue(null);

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('X-Session-Id')).toBeFalse();
    expect(req.request.headers.has('X-Session-Token')).toBeFalse();
    req.flush({});
  });

  it('should not inject X-Session-Token for public bypass endpoints even when token exists', () => {
    sessionService.getSessionToken.and.returnValue('mock-token-123');

    const bypassUrls = [
      '/api/auth/login',
      '/api/setup/status',
      '/api/setup/complete',
      '/api/settings/public'
    ];

    bypassUrls.forEach(url => {
      httpClient.get(url).subscribe();
      const req = httpMock.expectOne(url);
      expect(req.request.headers.has('X-Session-Token')).toBeFalse();
      req.flush({});
    });
  });
});
