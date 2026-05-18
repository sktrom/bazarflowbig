import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InventoryApiService } from './inventory-api.service';
import { environment } from '../../../../environments/environment';

describe('InventoryApiService', () => {
  let service: InventoryApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [InventoryApiService]
    });
    service = TestBed.inject(InventoryApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get inventory list with params', () => {
    service.getInventoryList({ search: 'test', page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/inventory?search=test&page=2&pageSize=10`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 });
  });

  it('should get inventory details', () => {
    service.getInventoryDetails(123).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/inventory/123`);
    expect(req.request.method).toBe('GET');
    req.flush({ productId: 123, batches: [] });
  });
});
