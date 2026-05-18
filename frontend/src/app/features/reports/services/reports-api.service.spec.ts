import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReportsApiService } from './reports-api.service';
import { environment } from '../../../../environments/environment';

describe('ReportsApiService', () => {
  let service: ReportsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReportsApiService]
    });
    service = TestBed.inject(ReportsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call getSalesInvoices without params', () => {
    service.getSalesInvoices().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/reports/sales/invoices`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should call getSalesInvoices with params', () => {
    service.getSalesInvoices('2023-01-01', '2023-12-31', 'Completed').subscribe();
    const req = httpMock.expectOne(request => 
      request.url === `${environment.apiUrl}/api/reports/sales/invoices` &&
      request.params.get('dateFrom') === '2023-01-01' &&
      request.params.get('dateTo') === '2023-12-31' &&
      request.params.get('status') === 'Completed'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should call getExpirySummary without any params', () => {
    service.getExpirySummary().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/reports/expiry/summary`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush({ items: [] });
  });

  it('should call getInventoryCharts', () => {
    service.getInventoryCharts().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/reports/inventory/charts`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });
});
