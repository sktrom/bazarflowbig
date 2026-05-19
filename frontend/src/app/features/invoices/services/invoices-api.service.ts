import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface InvoiceListItem {
  invoiceId: number;
  invoiceNumber: string;
  status: string;
  customerName?: string;
  originalEmployeeId: number;
  createdAt: string;
  completedAt?: string;
  totalUsd: number;
  totalSyp?: number;
  hasManualPriceEdit: boolean;
  hasAdjustmentRequest: boolean;
  adjustmentRequestStatus?: string;
  adjustmentRequestId?: number;
  suspensionReason?: string;
}

export interface InvoiceListResponse {
  items: InvoiceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface InvoiceLine {
  lineId: number;
  productId: number;
  productName: string;
  offerId?: number;
  quantity: number;
  unitPriceUsdOriginal: number;
  lineTotalUsdOriginal: number;
  lineTotalUsdEffective: number;
  isPriceOverridden: boolean;
  sortOrder: number;
}

export interface InvoiceDetailsResponse {
  invoiceId: number;
  invoiceNumber: string;
  status: string;
  customerName?: string;
  originalEmployeeId: number;
  employeeName?: string;
  invoiceDiscountType?: string;
  invoiceDiscountValue?: number;
  subtotalUsd: number;
  totalUsd: number;
  exchangeRateSypSnapshot?: number;
  totalSyp?: number;
  hasManualPriceEdit: boolean;
  hasAdjustmentRequest: boolean;
  adjustmentRequestStatus?: string;
  adjustmentRequestType?: string;
  adjustmentRequestId?: number;
  createdAt: string;
  completedAt?: string;
  suspensionReason?: string;
  lines: InvoiceLine[];
}

export interface CreateAdjustmentRequestLine {
  invoiceLineId: number;
  requestedQuantity?: number;
  requestedLineTotalUsd?: number;
}

export interface CreateAdjustmentRequest {
  requestType: string;
  reason: string;
  lines?: CreateAdjustmentRequestLine[];
}

export interface AdjustmentRequestLineResponse {
  invoiceLineId?: number;
  actionType: string;
  requestedQuantity?: number;
  requestedLineTotalUsd?: number;
}

export interface AdjustmentRequestResponse {
  requestId: number;
  invoiceId: number;
  status: string;
  requestType: string;
  reason: string;
  requestedByEmployeeId: number;
  reviewedByEmployeeId?: number;
  createdAt: string;
  reviewedAt?: string;
  lines: AdjustmentRequestLineResponse[];
}

@Injectable({ providedIn: 'root' })
export class InvoicesApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/invoices`;

  constructor(private http: HttpClient) {}

  getInvoices(filters: any): Observable<InvoiceListResponse> {
    let params = new HttpParams();
    Object.keys(filters).forEach(key => {
      if (filters[key] !== null && filters[key] !== undefined && filters[key] !== '') {
        params = params.set(key, filters[key]);
      }
    });
    return this.http.get<InvoiceListResponse>(this.baseUrl, { params });
  }

  getInvoiceDetails(invoiceId: number): Observable<InvoiceDetailsResponse> {
    return this.http.get<InvoiceDetailsResponse>(`${this.baseUrl}/${invoiceId}/details`);
  }

  createAdjustmentRequest(invoiceId: number, request: CreateAdjustmentRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/${invoiceId}/adjustment-requests`, request);
  }

  loadSuspended(invoiceId: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/cashier/cart/load-suspended/${invoiceId}`, {});
  }

  exportInvoices(filters: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${environment.apiUrl}/api/exports/invoices`, filters, { 
      responseType: 'blob',
      observe: 'response'
    });
  }

  getAdjustmentRequest(invoiceId: number, requestId: number): Observable<AdjustmentRequestResponse> {
    return this.http.get<AdjustmentRequestResponse>(`${this.baseUrl}/${invoiceId}/adjustment-requests/${requestId}`);
  }

  approveAdjustmentRequest(invoiceId: number, requestId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${invoiceId}/adjustment-requests/${requestId}/approve`, {});
  }

  rejectAdjustmentRequest(invoiceId: number, requestId: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${invoiceId}/adjustment-requests/${requestId}/reject`, {});
  }
}
