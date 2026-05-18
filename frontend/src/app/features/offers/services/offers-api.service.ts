import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  OfferListResponse,
  OfferDetailResponse,
  CreateOfferRequest,
  UpdateOfferRequest,
  CancelOfferResponse,
  DeleteOfferResponse
} from '../models/offer.model';

@Injectable({ providedIn: 'root' })
export class OffersApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/offers`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<OfferListResponse> {
    return this.http.get<OfferListResponse>(this.baseUrl);
  }

  create(req: CreateOfferRequest): Observable<OfferDetailResponse> {
    return this.http.post<OfferDetailResponse>(this.baseUrl, req);
  }

  update(id: number, req: UpdateOfferRequest): Observable<OfferDetailResponse> {
    return this.http.put<OfferDetailResponse>(`${this.baseUrl}/${id}`, req);
  }

  cancel(id: number): Observable<CancelOfferResponse> {
    return this.http.post<CancelOfferResponse>(`${this.baseUrl}/${id}/cancel`, {});
  }

  delete(id: number): Observable<DeleteOfferResponse> {
    return this.http.delete<DeleteOfferResponse>(`${this.baseUrl}/${id}`);
  }
}
