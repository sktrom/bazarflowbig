import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { PurchaseInvoicesApiService } from './purchase-invoices-api.service';
import {
  CreatePurchaseInvoiceLineRequest,
  CreatePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceLineRequest,
  UpdatePurchaseInvoiceRequest
} from '../models/purchase-invoice.model';

describe('PurchaseInvoicesApiService', () => {
  let service: PurchaseInvoicesApiService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/purchase-invoices`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PurchaseInvoicesApiService]
    });
    service = TestBed.inject(PurchaseInvoicesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should get purchase invoices list', () => {
    service.getAll().subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should get purchase invoice by id', () => {
    service.getById(1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, invoiceNumber: 'PI-1', lines: [] });
  });

  it('should create purchase invoice with payload', () => {
    const payload: CreatePurchaseInvoiceRequest = { supplierId: 1, externalInvoiceNumber: 'EXT-1', notes: 'Notes' };

    service.create(payload).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, ...payload, lines: [] });
  });

  it('should update purchase invoice with payload', () => {
    const payload: UpdatePurchaseInvoiceRequest = { supplierId: 1, externalInvoiceNumber: null, notes: null };

    service.update(1, payload).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, ...payload, lines: [] });
  });

  it('should delete purchase invoice by id', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, action: 'DELETED', message: 'Deleted' });
  });

  it('should add line with payload', () => {
    const payload: CreatePurchaseInvoiceLineRequest = { productId: 3, quantity: 2, unitCostUsd: 1.5, expiryDate: null, notes: null };

    service.addLine(1, payload).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1/lines`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, lines: [] });
  });

  it('should update line with payload', () => {
    const payload: UpdatePurchaseInvoiceLineRequest = { quantity: 3, unitCostUsd: 2, expiryDate: '2026-12-31', notes: 'Updated' };

    service.updateLine(1, 2, payload).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1/lines/2`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, lines: [] });
  });

  it('should delete line by id', () => {
    service.deleteLine(1, 2).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1/lines/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ id: 1, lines: [] });
  });

  it('should lookup products with search query', () => {
    service.productsLookup('milk').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/products-lookup?search=milk`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });
});
