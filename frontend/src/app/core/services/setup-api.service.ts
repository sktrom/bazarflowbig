import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SetupStatusResponse {
  setupCompleted: boolean;
}

export interface SetupCompleteRequest {
  storeName: string;
  exchangeRate: number;
  adminFullName: string;
  adminUsername: string;
  adminPassword?: string;
  deviceCode: string;
  deviceName: string;
}

export interface SetupCompleteResponse {
  success: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SetupApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/setup`;

  constructor(private http: HttpClient) {}

  getStatus(): Observable<SetupStatusResponse> {
    return this.http.get<SetupStatusResponse>(`${this.baseUrl}/status`);
  }

  complete(request: SetupCompleteRequest): Observable<SetupCompleteResponse> {
    return this.http.post<SetupCompleteResponse>(`${this.baseUrl}/complete`, request);
  }
}
