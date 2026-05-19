import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ActionCenterResponseDto } from '../models/action-center.model';

@Injectable({
  providedIn: 'root'
})
export class ActionCenterApiService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/inventory/action-center`;

  getActionCenterSummary(): Observable<ActionCenterResponseDto> {
    return this.http.get<ActionCenterResponseDto>(this.baseUrl);
  }
}
