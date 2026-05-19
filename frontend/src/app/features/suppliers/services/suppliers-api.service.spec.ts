import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SuppliersApiService } from './suppliers-api.service';
import { environment } from '../../../../environments/environment';
import { CreateSupplierRequest, UpdateSupplierRequest } from '../models/supplier.model';

describe('SuppliersApiService', () => {
  let service: SuppliersApiService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/suppliers`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SuppliersApiService]
    });
    service = TestBed.inject(SuppliersApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should get suppliers list', () => {
    service.getSuppliers().subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [] });
  });

  it('should get supplier by id', () => {
    service.getSupplier(1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 1, name: 'Supplier', isActive: true });
  });

  it('should create supplier with payload', () => {
    const payload: CreateSupplierRequest = { name: 'Supplier', phone: '123', email: null, address: null, notes: null };

    service.createSupplier(payload).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, ...payload, isActive: true });
  });

  it('should update supplier with payload', () => {
    const payload: UpdateSupplierRequest = { name: 'Supplier', phone: null, email: null, address: null, notes: null, isActive: false };

    service.updateSupplier(1, payload).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, ...payload });
  });

  it('should delete supplier by id', () => {
    service.deleteSupplier(1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, action: 'DELETED', message: 'Deleted' });
  });
});
