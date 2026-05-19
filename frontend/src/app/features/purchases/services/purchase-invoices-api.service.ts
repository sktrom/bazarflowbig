import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreatePurchaseInvoiceLineRequest,
  CreatePurchaseInvoiceRequest,
  DeletePurchaseInvoiceLineResponse,
  DeletePurchaseInvoiceResponse,
  PurchaseInvoiceDetailResponse,
  PurchaseInvoiceListResponse,
  PurchaseProductLookupResponse,
  UpdatePurchaseInvoiceLineRequest,
  UpdatePurchaseInvoiceRequest
} from '../models/purchase-invoice.model';

@Injectable({ providedIn: 'root' })
export class PurchaseInvoicesApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/purchase-invoices`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<PurchaseInvoiceListResponse> {
    return this.http.get<PurchaseInvoiceListResponse>(this.baseUrl);
  }

  getById(id: number): Observable<PurchaseInvoiceDetailResponse> {
    return this.http.get<PurchaseInvoiceDetailResponse>(`${this.baseUrl}/${id}`);
  }

  create(request: CreatePurchaseInvoiceRequest): Observable<PurchaseInvoiceDetailResponse> {
    return this.http.post<PurchaseInvoiceDetailResponse>(this.baseUrl, request);
  }

  update(id: number, request: UpdatePurchaseInvoiceRequest): Observable<PurchaseInvoiceDetailResponse> {
    return this.http.put<PurchaseInvoiceDetailResponse>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<DeletePurchaseInvoiceResponse> {
    return this.http.delete<DeletePurchaseInvoiceResponse>(`${this.baseUrl}/${id}`);
  }

  addLine(invoiceId: number, request: CreatePurchaseInvoiceLineRequest): Observable<PurchaseInvoiceDetailResponse> {
    return this.http.post<PurchaseInvoiceDetailResponse>(`${this.baseUrl}/${invoiceId}/lines`, request);
  }

  updateLine(invoiceId: number, lineId: number, request: UpdatePurchaseInvoiceLineRequest): Observable<PurchaseInvoiceDetailResponse> {
    return this.http.put<PurchaseInvoiceDetailResponse>(`${this.baseUrl}/${invoiceId}/lines/${lineId}`, request);
  }

  deleteLine(invoiceId: number, lineId: number): Observable<DeletePurchaseInvoiceLineResponse> {
    return this.http.delete<DeletePurchaseInvoiceLineResponse>(`${this.baseUrl}/${invoiceId}/lines/${lineId}`);
  }

  productsLookup(search?: string): Observable<PurchaseProductLookupResponse> {
    let params = new HttpParams();
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<PurchaseProductLookupResponse>(`${this.baseUrl}/products-lookup`, { params });
  }
}
