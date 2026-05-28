import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateBlackBoxEventRequest {
  route?: string | null;
  pageName?: string | null;
  actionType: string;
  elementKey?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  result: 'SUCCESS' | 'FAILED' | 'BLOCKED' | 'CANCELLED' | string;
  message?: string | null;
  metadata?: Record<string, unknown> | null;
}

export interface CreateBlackBoxEventResponse {
  success: boolean;
  id: number;
}

export interface BlackBoxEventListItem {
  id: number;
  employeeId?: number | null;
  employeeName?: string | null;
  sessionId?: number | null;
  deviceCode?: string | null;
  route?: string | null;
  pageName?: string | null;
  actionType: string;
  elementKey?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  result: string;
  message?: string | null;
  hasMetadata: boolean;
  metadataTruncated: boolean;
  createdAtUtc: string;
}

export interface BlackBoxEventListResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  items: BlackBoxEventListItem[];
}

export interface BlackBoxEventDetailResponse extends BlackBoxEventListItem {
  metadataJson?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
}

@Injectable({ providedIn: 'root' })
export class BlackBoxApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/black-box/events`;

  constructor(private http: HttpClient) {}

  createEvent(request: CreateBlackBoxEventRequest): Observable<CreateBlackBoxEventResponse> {
    return this.http.post<CreateBlackBoxEventResponse>(this.baseUrl, request);
  }

  getEvents(params?: any): Observable<BlackBoxEventListResponse> {
    return this.http.get<BlackBoxEventListResponse>(this.baseUrl, { params });
  }

  getEvent(id: number | string): Observable<BlackBoxEventDetailResponse> {
    return this.http.get<BlackBoxEventDetailResponse>(`${this.baseUrl}/${id}`);
  }
}
