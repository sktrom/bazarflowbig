import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SettingsApiService } from './settings-api.service';
import { environment } from '../../../../environments/environment';

describe('SettingsApiService', () => {
  let service: SettingsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SettingsApiService]
    });
    service = TestBed.inject(SettingsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should get employees list', () => {
    service.getEmployees().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/employees`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should get employee by id', () => {
    service.getEmployee(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/employees/1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, fullName: 'Test', permissions: [] });
  });

  it('should create employee with permissions', () => {
    const body = { fullName: 'A', username: 'a', password: '123', permissions: [{ screenKey: 'Sales', canAccess: true }] };
    service.createEmployee(body).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/employees`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.permissions).toBeDefined();
    req.flush({ id: 1 });
  });

  it('should call reset-password endpoint', () => {
    service.resetPassword(1, { newPassword: 'abc' }).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/employees/1/reset-password`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true });
  });

  it('should get categories list', () => {
    service.getCategories().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/categories`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should get public settings', () => {
    service.getPublicSettings().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/settings/public`);
    expect(req.request.method).toBe('GET');
    req.flush({ storeName: 'Test', exchangeRate: 15000 });
  });
});
