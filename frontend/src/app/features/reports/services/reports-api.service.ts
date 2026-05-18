import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ReportList,
  SalesInvoiceReportDto,
  SalesItemReportDto,
  SalesChartDto,
  ProductSummaryReportDto,
  ProductMovementReportDto,
  ProductChartDto,
  EmployeeSummaryReportDto,
  EmployeeActivityReportDto,
  EmployeeChartDto,
  InventorySummaryReportDto,
  InventoryBatchReportDto,
  InventoryChartDto,
  ExpirySummaryReportDto,
  ExpiryBatchReportDto,
  ExpiryChartDto
} from '../models/reports.model';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/reports`;

  constructor(private http: HttpClient) {}

  private buildParams(params: Record<string, any>): HttpParams {
    let httpParams = new HttpParams();
    Object.keys(params).forEach(key => {
      if (params[key] !== null && params[key] !== undefined && params[key] !== '') {
        httpParams = httpParams.set(key, params[key]);
      }
    });
    return httpParams;
  }

  // --- Sales ---
  getSalesInvoices(dateFrom?: string, dateTo?: string, status?: string): Observable<ReportList<SalesInvoiceReportDto>> {
    return this.http.get<ReportList<SalesInvoiceReportDto>>(`${this.baseUrl}/sales/invoices`, { params: this.buildParams({ dateFrom, dateTo, status }) });
  }

  getSalesItems(dateFrom?: string, dateTo?: string): Observable<ReportList<SalesItemReportDto>> {
    return this.http.get<ReportList<SalesItemReportDto>>(`${this.baseUrl}/sales/items`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  getSalesCharts(dateFrom?: string, dateTo?: string): Observable<ReportList<SalesChartDto>> {
    return this.http.get<ReportList<SalesChartDto>>(`${this.baseUrl}/sales/charts`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  // --- Products ---
  getProductsSummary(categoryId?: number): Observable<ReportList<ProductSummaryReportDto>> {
    return this.http.get<ReportList<ProductSummaryReportDto>>(`${this.baseUrl}/products/summary`, { params: this.buildParams({ categoryId }) });
  }

  getProductsMovements(dateFrom?: string, dateTo?: string, productId?: number): Observable<ReportList<ProductMovementReportDto>> {
    return this.http.get<ReportList<ProductMovementReportDto>>(`${this.baseUrl}/products/movements`, { params: this.buildParams({ dateFrom, dateTo, productId }) });
  }

  getProductsCharts(dateFrom?: string, dateTo?: string): Observable<ReportList<ProductChartDto>> {
    return this.http.get<ReportList<ProductChartDto>>(`${this.baseUrl}/products/charts`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  // --- Employees ---
  getEmployeesSummary(dateFrom?: string, dateTo?: string): Observable<ReportList<EmployeeSummaryReportDto>> {
    return this.http.get<ReportList<EmployeeSummaryReportDto>>(`${this.baseUrl}/employees/summary`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  getEmployeesActivity(dateFrom?: string, dateTo?: string, employeeId?: number): Observable<ReportList<EmployeeActivityReportDto>> {
    return this.http.get<ReportList<EmployeeActivityReportDto>>(`${this.baseUrl}/employees/activity`, { params: this.buildParams({ dateFrom, dateTo, employeeId }) });
  }

  getEmployeesCharts(dateFrom?: string, dateTo?: string): Observable<ReportList<EmployeeChartDto>> {
    return this.http.get<ReportList<EmployeeChartDto>>(`${this.baseUrl}/employees/charts`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  // --- Inventory ---
  getInventorySummary(categoryId?: number): Observable<ReportList<InventorySummaryReportDto>> {
    return this.http.get<ReportList<InventorySummaryReportDto>>(`${this.baseUrl}/inventory/summary`, { params: this.buildParams({ categoryId }) });
  }

  getInventoryBatches(dateFrom?: string, dateTo?: string): Observable<ReportList<InventoryBatchReportDto>> {
    return this.http.get<ReportList<InventoryBatchReportDto>>(`${this.baseUrl}/inventory/batches`, { params: this.buildParams({ dateFrom, dateTo }) });
  }

  getInventoryCharts(): Observable<ReportList<InventoryChartDto>> {
    return this.http.get<ReportList<InventoryChartDto>>(`${this.baseUrl}/inventory/charts`);
  }

  // --- Expiry ---
  getExpirySummary(): Observable<ReportList<ExpirySummaryReportDto>> {
    return this.http.get<ReportList<ExpirySummaryReportDto>>(`${this.baseUrl}/expiry/summary`);
  }

  getExpiryBatches(): Observable<ReportList<ExpiryBatchReportDto>> {
    return this.http.get<ReportList<ExpiryBatchReportDto>>(`${this.baseUrl}/expiry/batches`);
  }

  getExpiryCharts(): Observable<ReportList<ExpiryChartDto>> {
    return this.http.get<ReportList<ExpiryChartDto>>(`${this.baseUrl}/expiry/charts`);
  }
}
