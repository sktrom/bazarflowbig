import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateSupplierRequest,
  DeleteSupplierResponse,
  SupplierDetailResponse,
  SupplierListResponse,
  UpdateSupplierRequest
} from '../models/supplier.model';

@Injectable({ providedIn: 'root' })
export class SuppliersApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/suppliers`;

  constructor(private http: HttpClient) {}

  getSuppliers(): Observable<SupplierListResponse> {
    return this.http.get<SupplierListResponse>(this.baseUrl);
  }

  getSupplier(id: number): Observable<SupplierDetailResponse> {
    return this.http.get<SupplierDetailResponse>(`${this.baseUrl}/${id}`);
  }

  createSupplier(request: CreateSupplierRequest): Observable<SupplierDetailResponse> {
    return this.http.post<SupplierDetailResponse>(this.baseUrl, request);
  }

  updateSupplier(id: number, request: UpdateSupplierRequest): Observable<SupplierDetailResponse> {
    return this.http.put<SupplierDetailResponse>(`${this.baseUrl}/${id}`, request);
  }

  deleteSupplier(id: number): Observable<DeleteSupplierResponse> {
    return this.http.delete<DeleteSupplierResponse>(`${this.baseUrl}/${id}`);
  }
}
