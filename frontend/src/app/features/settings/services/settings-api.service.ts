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
  AuditLogListResponse, AuditLogDetailResponse
} from '../models/settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly emp = `${environment.apiUrl}/api/employees`;
  private readonly cat = `${environment.apiUrl}/api/categories`;
  private readonly pub = `${environment.apiUrl}/api/settings/public`;
  private readonly backup = `${environment.apiUrl}/api/system/backup`;
  private readonly audit = `${environment.apiUrl}/api/audit-logs`;

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
}
