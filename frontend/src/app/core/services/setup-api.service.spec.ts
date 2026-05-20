import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SetupApiService } from './setup-api.service';
import { environment } from '../../../environments/environment';

describe('SetupApiService', () => {
  let service: SetupApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SetupApiService]
    });
    service = TestBed.inject(SetupApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch setup status from HTTP on first call', (done) => {
    service.getStatus().subscribe(res => {
      expect(res.setupCompleted).toBeFalse();
      done();
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/setup/status`);
    expect(req.request.method).toBe('GET');
    req.flush({ setupCompleted: false });
  });

  it('should return cached value on second call without HTTP request', (done) => {
    // First call — populates cache
    service.getStatus().subscribe(() => {
      // Second call — should use cache (no HTTP)
      service.getStatus().subscribe(res => {
        expect(res.setupCompleted).toBeTrue();
        done();
      });
      // No second HTTP request should be made
      httpMock.expectNone(`${environment.apiUrl}/api/setup/status`);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/setup/status`);
    req.flush({ setupCompleted: true });
  });

  it('should update cached value via setCompletedCache', (done) => {
    service.setCompletedCache(true);

    service.getStatus().subscribe(res => {
      expect(res.setupCompleted).toBeTrue();
      // No HTTP call expected since cache was manually set
      done();
    });

    httpMock.expectNone(`${environment.apiUrl}/api/setup/status`);
  });
});
