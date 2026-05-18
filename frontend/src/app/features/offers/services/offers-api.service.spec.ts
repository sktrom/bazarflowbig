import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OffersApiService } from './offers-api.service';
import { environment } from '../../../../environments/environment';

describe('OffersApiService', () => {
  let service: OffersApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [OffersApiService]
    });
    service = TestBed.inject(OffersApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get all offers', () => {
    service.getAll().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/offers`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should create an offer', () => {
    const dummyReq = { productId: 1, discountType: 'Amount', discountValue: 10 };
    service.create(dummyReq).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/offers`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dummyReq);
    req.flush({ id: 1, ...dummyReq });
  });

  it('should update an offer', () => {
    const dummyReq = { productId: 1, discountType: 'Percent', discountValue: 5 };
    service.update(1, dummyReq).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/offers/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dummyReq);
    req.flush({ id: 1, ...dummyReq });
  });

  it('should cancel an offer', () => {
    service.cancel(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/offers/1/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, message: 'Cancelled' });
  });

  it('should delete an offer', () => {
    service.delete(1).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/api/offers/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, message: 'Deleted' });
  });
});
