import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  EmployeeListResponse, EmployeeDetailResponse,
  CreateEmployeeRequest, UpdateEmployeeRequest,
  DeleteEmployeeResponse, ResetPasswordRequest, ResetPasswordResponse,
  CategoryListResponse, CategoryItem,
  CreateCategoryRequest, UpdateCategoryRequest, DeleteCategoryResponse,
  PublicSettingsResponse, CreateBackupResponse,
  AuditLogListResponse, AuditLogDetailResponse,
  PosDeviceListItem, PosDeviceDetailsResponse,
  CreatePosDeviceRequest, UpdatePosDeviceRequest,
  DeletePosDeviceResponse, EnableDisablePosDeviceResponse
} from '../models/settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly emp = `${environment.apiUrl}/api/employees`;
  private readonly cat = `${environment.apiUrl}/api/categories`;
  private readonly pub = `${environment.apiUrl}/api/settings/public`;
  private readonly backup = `${environment.apiUrl}/api/system/backup`;
  private readonly audit = `${environment.apiUrl}/api/audit-logs`;
  private readonly dev = `${environment.apiUrl}/api/devices`;

  constructor(private http: HttpClient) {}

  // --- Employees ---
  getEmployees(): Observable<EmployeeListResponse> {
    return this.http.get<EmployeeListResponse>(this.emp);
  }

  getEmployee(id: number): Observable<EmployeeDetailResponse> {
    return this.http.get<EmployeeDetailResponse>(`${this.emp}/${id}`);
  }

  createEmployee(req: CreateEmployeeRequest): Observable<EmployeeDetailResponse> {
    return this.http.post<EmployeeDetailResponse>(this.emp, req);
  }

  updateEmployee(id: number, req: UpdateEmployeeRequest): Observable<EmployeeDetailResponse> {
    return this.http.put<EmployeeDetailResponse>(`${this.emp}/${id}`, req);
  }

  deleteEmployee(id: number): Observable<DeleteEmployeeResponse> {
    return this.http.delete<DeleteEmployeeResponse>(`${this.emp}/${id}`);
  }

  resetPassword(id: number, req: ResetPasswordRequest): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.emp}/${id}/reset-password`, req);
  }

  // --- Categories ---
  getCategories(): Observable<CategoryListResponse> {
    return this.http.get<CategoryListResponse>(this.cat);
  }

  createCategory(req: CreateCategoryRequest): Observable<CategoryItem> {
    return this.http.post<CategoryItem>(this.cat, req);
  }

  updateCategory(id: number, req: UpdateCategoryRequest): Observable<CategoryItem> {
    return this.http.put<CategoryItem>(`${this.cat}/${id}`, req);
  }

  deleteCategory(id: number): Observable<DeleteCategoryResponse> {
    return this.http.delete<DeleteCategoryResponse>(`${this.cat}/${id}`);
  }

  // --- Settings ---
  getPublicSettings(): Observable<PublicSettingsResponse> {
    return this.http.get<PublicSettingsResponse>(this.pub);
  }

  createBackup(): Observable<CreateBackupResponse> {
    return this.http.post<CreateBackupResponse>(this.backup, {});
  }

  // --- Audit Logs ---
  getAuditLogs(params: any): Observable<AuditLogListResponse> {
    return this.http.get<AuditLogListResponse>(this.audit, { params });
  }

  getAuditLog(id: number): Observable<AuditLogDetailResponse> {
    return this.http.get<AuditLogDetailResponse>(`${this.audit}/${id}`);
  }

  // --- POS Devices ---
  getDevices(): Observable<PosDeviceListItem[]> {
    return this.http.get<PosDeviceListItem[]>(this.dev);
  }

  getDevice(id: number): Observable<PosDeviceDetailsResponse> {
    return this.http.get<PosDeviceDetailsResponse>(`${this.dev}/${id}`);
  }

  createDevice(req: CreatePosDeviceRequest): Observable<PosDeviceDetailsResponse> {
    return this.http.post<PosDeviceDetailsResponse>(this.dev, req);
  }

  updateDevice(id: number, req: UpdatePosDeviceRequest): Observable<PosDeviceDetailsResponse> {
    return this.http.put<PosDeviceDetailsResponse>(`${this.dev}/${id}`, req);
  }

  enableDevice(id: number): Observable<EnableDisablePosDeviceResponse> {
    return this.http.post<EnableDisablePosDeviceResponse>(`${this.dev}/${id}/enable`, {});
  }

  disableDevice(id: number): Observable<EnableDisablePosDeviceResponse> {
    return this.http.post<EnableDisablePosDeviceResponse>(`${this.dev}/${id}/disable`, {});
  }

  deleteDevice(id: number): Observable<DeletePosDeviceResponse> {
    return this.http.delete<DeletePosDeviceResponse>(`${this.dev}/${id}`);
  }
}
