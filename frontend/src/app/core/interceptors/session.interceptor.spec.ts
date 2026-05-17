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
    const sessionSpy = jasmine.createSpyObj('SessionService', ['getSessionId']);
    
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

  it('should inject X-Session-Id header when session exists', () => {
    sessionService.getSessionId.and.returnValue('mock-session-123');

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('X-Session-Id')).toBeTrue();
    expect(req.request.headers.get('X-Session-Id')).toEqual('mock-session-123');
    req.flush({});
  });

  it('should not inject X-Session-Id header when session is missing', () => {
    sessionService.getSessionId.and.returnValue(null);

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('X-Session-Id')).toBeFalse();
    req.flush({});
  });
});
