import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
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
  private cachedCompleted: boolean | null = null;

  constructor(private http: HttpClient) {}

  getStatus(): Observable<SetupStatusResponse> {
    if (this.cachedCompleted !== null) {
      return of({ setupCompleted: this.cachedCompleted });
    }
    return this.http.get<SetupStatusResponse>(`${this.baseUrl}/status`).pipe(
      tap(res => this.cachedCompleted = res.setupCompleted)
    );
  }

  complete(request: SetupCompleteRequest): Observable<SetupCompleteResponse> {
    return this.http.post<SetupCompleteResponse>(`${this.baseUrl}/complete`, request);
  }

  setCompletedCache(completed: boolean): void {
    this.cachedCompleted = completed;
  }
}

