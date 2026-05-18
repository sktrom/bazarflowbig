import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { InventoryListResponse, InventoryDetailsResponse } from '../models/inventory.model';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/inventory`;

  constructor(private http: HttpClient) {}

  getInventoryList(params?: {
    search?: string;
    categoryId?: number;
    isActive?: boolean;
    hasStock?: boolean;
    hasExpiry?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<InventoryListResponse> {
    let httpParams = new HttpParams();
    
    if (params) {
      if (params.search) httpParams = httpParams.set('search', params.search);
      if (params.categoryId !== undefined && params.categoryId !== null) httpParams = httpParams.set('categoryId', params.categoryId.toString());
      if (params.isActive !== undefined && params.isActive !== null) httpParams = httpParams.set('isActive', params.isActive.toString());
      if (params.hasStock !== undefined && params.hasStock !== null) httpParams = httpParams.set('hasStock', params.hasStock.toString());
      if (params.hasExpiry !== undefined && params.hasExpiry !== null) httpParams = httpParams.set('hasExpiry', params.hasExpiry.toString());
      if (params.page !== undefined) httpParams = httpParams.set('page', params.page.toString());
      if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<InventoryListResponse>(this.baseUrl, { params: httpParams });
  }

  getInventoryDetails(productId: number): Observable<InventoryDetailsResponse> {
    return this.http.get<InventoryDetailsResponse>(`${this.baseUrl}/${productId}`);
  }

  exportInventory(request: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${environment.apiUrl}/api/exports/inventory`, request, {
      responseType: 'blob',
      observe: 'response'
    });
  }
}
