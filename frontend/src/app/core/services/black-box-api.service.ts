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

@Injectable({ providedIn: 'root' })
export class BlackBoxApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/black-box/events`;

  constructor(private http: HttpClient) {}

  createEvent(request: CreateBlackBoxEventRequest): Observable<CreateBlackBoxEventResponse> {
    return this.http.post<CreateBlackBoxEventResponse>(this.baseUrl, request);
  }
}
