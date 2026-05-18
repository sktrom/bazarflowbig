import { Component, OnInit, OnDestroy, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsApiService } from '../services/reports-api.service';
import { HttpErrorResponse } from '@angular/common/http';
import Chart from 'chart.js/auto';

type TabType = 'Sales' | 'Products' | 'Employees' | 'Inventory' | 'Expiry';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      
      <!-- Header -->
      <div class="bg-white p-4 sm:p-6 border-b border-slate-200 shrink-0">
        <h1 class="text-2xl font-bold text-slate-800 mb-4">التقارير</h1>
        
        <!-- Tabs -->
        <div class="flex space-x-reverse space-x-2 overflow-x-auto">
          <button *ngFor="let tab of tabs" 
                  (click)="switchTab(tab.id)"
                  [class.bg-primary]="activeTab === tab.id"
                  [class.text-white]="activeTab === tab.id"
                  [class.bg-slate-100]="activeTab !== tab.id"
                  [class.text-slate-600]="activeTab !== tab.id"
                  class="px-4 py-2 rounded-t-lg font-medium transition-colors whitespace-nowrap hover:bg-primary/80 hover:text-white">
            {{ tab.label }}
          </button>
        </div>
      </div>

      <!-- Main Content -->
      <div class="flex-1 overflow-auto p-4 sm:p-6">
        
        <!-- Filters (except Expiry) -->
        <div *ngIf="activeTab !== 'Expiry'" class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm mb-6 flex flex-wrap gap-4 items-end">
          <div *ngIf="['Sales', 'Products', 'Employees', 'Inventory'].includes(activeTab)">
            <label class="block text-xs font-medium text-slate-500 mb-1">من تاريخ</label>
            <input type="date" [(ngModel)]="filters.dateFrom" class="px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm">
          </div>
          <div *ngIf="['Sales', 'Products', 'Employees', 'Inventory'].includes(activeTab)">
            <label class="block text-xs font-medium text-slate-500 mb-1">إلى تاريخ</label>
            <input type="date" [(ngModel)]="filters.dateTo" class="px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm">
          </div>
          <div *ngIf="activeTab === 'Sales'">
            <label class="block text-xs font-medium text-slate-500 mb-1">الحالة</label>
            <select [(ngModel)]="filters.status" class="px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm bg-white">
              <option [ngValue]="null">الكل</option>
              <option value="Completed">مكتمل</option>
              <option value="Cancelled">ملغى</option>
            </select>
          </div>
          <div *ngIf="activeTab === 'Products'">
            <label class="block text-xs font-medium text-slate-500 mb-1">رقم المنتج (للحركات)</label>
            <input type="number" [(ngModel)]="filters.productId" placeholder="الكل" class="w-32 px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm">
          </div>
          <div *ngIf="activeTab === 'Employees'">
            <label class="block text-xs font-medium text-slate-500 mb-1">رقم الموظف</label>
            <input type="number" [(ngModel)]="filters.employeeId" placeholder="الكل" class="w-32 px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm">
          </div>
          <div *ngIf="activeTab === 'Inventory' || activeTab === 'Products'">
            <label class="block text-xs font-medium text-slate-500 mb-1">رقم التصنيف (للملخص)</label>
            <input type="number" [(ngModel)]="filters.categoryId" placeholder="الكل" class="w-32 px-3 py-2 border border-slate-300 rounded-md focus:ring-primary focus:border-primary text-sm">
          </div>
          <div>
            <button (click)="refreshData()" [disabled]="isLoading" class="px-4 py-2 bg-primary hover:bg-primary-dark text-white rounded-md text-sm font-medium transition-colors disabled:opacity-50">
              تحديث
            </button>
          </div>
        </div>

        <!-- Expiry Refresh Button -->
        <div *ngIf="activeTab === 'Expiry'" class="mb-6">
          <button (click)="refreshData()" [disabled]="isLoading" class="px-4 py-2 bg-primary hover:bg-primary-dark text-white rounded-md text-sm font-medium transition-colors disabled:opacity-50">
            تحديث البيانات
          </button>
        </div>

        <!-- Loading State -->
        <div *ngIf="isLoading" class="flex flex-col items-center justify-center py-12">
          <div class="w-10 h-10 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
          <p class="text-slate-500 font-medium mt-4">جاري التحميل...</p>
        </div>

        <!-- Content Area -->
        <div *ngIf="!isLoading">

          <!-- Chart Section -->
          <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm mb-6 h-[300px] flex items-center justify-center relative">
            <canvas #chartCanvas class="w-full h-full" [class.hidden]="isChartEmpty"></canvas>
            <div *ngIf="isChartEmpty" class="text-slate-400 font-medium text-center absolute inset-0 flex items-center justify-center bg-white rounded-xl">
              لا توجد بيانات كافية للرسم البياني
            </div>
          </div>

          <!-- Active Tab specific Tables -->
          <ng-container [ngSwitch]="activeTab">
            
            <!-- 1. Sales -->
            <div *ngSwitchCase="'Sales'" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">الفواتير المنجزة</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">الرقم</th>
                        <th class="px-4 py-3">التاريخ</th>
                        <th class="px-4 py-3">الموظف</th>
                        <th class="px-4 py-3 text-center">الإجمالي (USD)</th>
                        <th class="px-4 py-3 text-center">الإجمالي (SYP)</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!salesInvoices.length"><td colspan="5" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of salesInvoices" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.invoiceNumber }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.createdAt | date:'short' }}</td>
                        <td class="px-4 py-3">{{ item.employeeName }}</td>
                        <td class="px-4 py-3 text-center font-semibold text-green-600">{{ item.totalUsd | currency:'USD' }}</td>
                        <td class="px-4 py-3 text-center font-semibold text-slate-700">{{ item.totalSyp ? (item.totalSyp | number:'1.0-0') + ' SYP' : '-' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">المنتجات المباعة</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3 text-center">الكمية</th>
                        <th class="px-4 py-3 text-center">الإيرادات (USD)</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!salesItems.length"><td colspan="3" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of salesItems" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.totalQuantitySold }}</td>
                        <td class="px-4 py-3 text-center text-green-600 font-semibold">{{ item.totalRevenueUsd | currency:'USD' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <!-- 2. Products -->
            <div *ngSwitchCase="'Products'" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">ملخص المنتجات</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3">التصنيف</th>
                        <th class="px-4 py-3 text-center">الكمية المتاحة</th>
                        <th class="px-4 py-3 text-center">قيمة المخزون</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!productsSummary.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of productsSummary" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.categoryName }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.totalStockQuantity }}</td>
                        <td class="px-4 py-3 text-center font-semibold">{{ item.totalStockValueUsd | currency:'USD' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">حركات المنتجات</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3">التاريخ</th>
                        <th class="px-4 py-3">النوع</th>
                        <th class="px-4 py-3 text-center">الكمية</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!productsMovements.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of productsMovements" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.movementDate | date:'short' }}</td>
                        <td class="px-4 py-3">{{ item.movementType }}</td>
                        <td class="px-4 py-3 text-center font-medium text-blue-600">{{ item.quantity }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <!-- 3. Employees -->
            <div *ngSwitchCase="'Employees'" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">ملخص المبيعات للموظفين</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">الموظف</th>
                        <th class="px-4 py-3 text-center">الفواتير</th>
                        <th class="px-4 py-3 text-center">إجمالي المبيعات (USD)</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!employeesSummary.length"><td colspan="3" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of employeesSummary" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.employeeName }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.totalInvoicesHandled }}</td>
                        <td class="px-4 py-3 text-center text-green-600 font-semibold">{{ item.totalSalesRevenueUsd | currency:'USD' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">سجل النشاط</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">الموظف</th>
                        <th class="px-4 py-3">التاريخ</th>
                        <th class="px-4 py-3">النشاط</th>
                        <th class="px-4 py-3">التفاصيل</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!employeesActivity.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of employeesActivity" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.employeeName }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.activityDate | date:'short' }}</td>
                        <td class="px-4 py-3"><span class="px-2 py-0.5 bg-blue-50 text-blue-700 rounded-full text-xs font-medium">{{ item.activityType }}</span></td>
                        <td class="px-4 py-3 text-slate-500 truncate max-w-[150px]" [title]="item.details">{{ item.details }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <!-- 4. Inventory -->
            <div *ngSwitchCase="'Inventory'" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">ملخص التخزين</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3 text-center">الكمية</th>
                        <th class="px-4 py-3 text-center">القيمة (USD)</th>
                        <th class="px-4 py-3 text-center">الحالة</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!inventorySummary.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of inventorySummary" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.totalQuantityAvailable }}</td>
                        <td class="px-4 py-3 text-center font-semibold">{{ item.totalStockValueUsd | currency:'USD' }}</td>
                        <td class="px-4 py-3 text-center">
                          <span class="text-xs font-medium px-2 py-1 rounded-full" [ngClass]="{
                            'bg-red-100 text-red-700': item.stockStatus === 'Out of Stock',
                            'bg-yellow-100 text-yellow-700': item.stockStatus === 'Low Stock',
                            'bg-green-100 text-green-700': item.stockStatus === 'In Stock'
                          }">{{ item.stockStatus }}</span>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">دفعات الإدخال</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3 text-center">المستلم</th>
                        <th class="px-4 py-3 text-center">المتاح</th>
                        <th class="px-4 py-3">التاريخ</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!inventoryBatches.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of inventoryBatches" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-center text-slate-500">{{ item.quantityReceived }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.quantityAvailable }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.entryDate | date:'shortDate' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            <!-- 5. Expiry -->
            <div *ngSwitchCase="'Expiry'" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">ملخص انتهاء الصلاحية</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3 text-center">دفعات منتهية</th>
                        <th class="px-4 py-3 text-center">قريبة الانتهاء</th>
                        <th class="px-4 py-3 text-center">خسائر (USD)</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!expirySummary.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of expirySummary" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-center text-red-600 font-bold">{{ item.expiredBatchesCount }}</td>
                        <td class="px-4 py-3 text-center text-yellow-600 font-bold">{{ item.expiringSoonBatchesCount }}</td>
                        <td class="px-4 py-3 text-center text-red-700 font-semibold">{{ item.totalExpiredValueUsd | currency:'USD' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
                <div class="px-4 py-3 bg-slate-50 border-b border-slate-200 font-bold text-slate-700">تفاصيل الدفعات</div>
                <div class="overflow-x-auto flex-1">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-white border-b border-slate-200">
                      <tr>
                        <th class="px-4 py-3">المنتج</th>
                        <th class="px-4 py-3 text-center">الكمية</th>
                        <th class="px-4 py-3">التاريخ</th>
                        <th class="px-4 py-3 text-center">الحالة</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr *ngIf="!expiryBatches.length"><td colspan="4" class="px-4 py-6 text-center text-slate-400">لا توجد بيانات</td></tr>
                      <tr *ngFor="let item of expiryBatches" class="hover:bg-slate-50">
                        <td class="px-4 py-3">{{ item.productName }}</td>
                        <td class="px-4 py-3 text-center font-medium">{{ item.quantityAvailable }}</td>
                        <td class="px-4 py-3 text-slate-500">{{ item.expiryDate | date:'shortDate' }}</td>
                        <td class="px-4 py-3 text-center">
                          <span class="text-xs font-medium px-2 py-1 rounded-full" [ngClass]="{
                            'bg-red-100 text-red-700': item.expiryStatus === 'Expired',
                            'bg-yellow-100 text-yellow-700': item.expiryStatus === 'ExpiringSoon',
                            'bg-green-100 text-green-700': item.expiryStatus === 'Fresh'
                          }">{{ item.expiryStatus }}</span>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

          </ng-container>
        </div>

      </div>

      <!-- Global Error Toast -->
      <div *ngIf="globalError" class="fixed bottom-4 left-4 bg-red-600 text-white px-4 py-3 rounded-lg shadow-lg z-50 flex gap-4 items-center animate-fade-in">
        <span class="text-sm font-medium">{{ globalError }}</span>
        <button (click)="globalError = ''" class="text-white/80 hover:text-white transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
        </button>
      </div>

    </div>
  `
})
export class ReportsComponent implements OnInit, OnDestroy {
  tabs: { id: TabType; label: string }[] = [
    { id: 'Sales', label: 'المبيعات' },
    { id: 'Products', label: 'المنتجات' },
    { id: 'Employees', label: 'الموظفون' },
    { id: 'Inventory', label: 'المخزون' },
    { id: 'Expiry', label: 'الصلاحية' }
  ];

  activeTab: TabType = 'Sales';
  
  // Filters
  filters: any = {
    dateFrom: null,
    dateTo: null,
    status: null,
    productId: null,
    employeeId: null,
    categoryId: null
  };

  // States
  isLoading = false;
  globalError = '';

  // Data Arrays
  salesInvoices: any[] = [];
  salesItems: any[] = [];
  
  productsSummary: any[] = [];
  productsMovements: any[] = [];
  
  employeesSummary: any[] = [];
  employeesActivity: any[] = [];
  
  inventorySummary: any[] = [];
  inventoryBatches: any[] = [];
  
  expirySummary: any[] = [];
  expiryBatches: any[] = [];

  // Chart
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;
  chartInstance: Chart | null = null;
  chartTimeoutId: any = null;
  isChartEmpty = true;

  constructor(private api: ReportsApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.refreshData();
  }

  ngOnDestroy() {
    this.destroyChart();
  }

  switchTab(tab: TabType) {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    
    // Retain dateFrom and dateTo, reset specific filters
    this.filters = { 
      ...this.filters,
      status: null, 
      productId: null, 
      employeeId: null, 
      categoryId: null 
    };
    
    this.destroyChart();
    this.refreshData();
  }

  refreshData() {
    this.globalError = '';
    this.isLoading = true;
    this.destroyChart();
    
    // Convert undefined/empty to null for API
    const f = { ...this.filters };

    switch (this.activeTab) {
      case 'Sales':
        this.loadSales(f);
        break;
      case 'Products':
        this.loadProducts(f);
        break;
      case 'Employees':
        this.loadEmployees(f);
        break;
      case 'Inventory':
        this.loadInventory(f);
        break;
      case 'Expiry':
        this.loadExpiry();
        break;
    }
  }

  // --- Loaders ---

  private loadSales(f: any) {
    let pending = 3;
    const checkDone = () => { pending--; if (pending === 0) this.isLoading = false; };
    const errHandler = (e: any) => { this.showError(e); checkDone(); };

    this.api.getSalesInvoices(f.dateFrom, f.dateTo, f.status).subscribe({
      next: (res) => { this.salesInvoices = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getSalesItems(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { this.salesItems = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getSalesCharts(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { 
        const items = res.items || [];
        if (items.length) {
          this.isChartEmpty = false;
          this.cdr.detectChanges(); // Ensure canvas is in DOM
          this.chartTimeoutId = setTimeout(() => {
            this.renderChart('line', items.map(i => i.dateLabel), items.map(i => i.revenueUsd), 'المبيعات (USD)');
          });
        } else {
          this.isChartEmpty = true;
        }
        checkDone(); 
      },
      error: errHandler
    });
  }

  private loadProducts(f: any) {
    let pending = 3;
    const checkDone = () => { pending--; if (pending === 0) this.isLoading = false; };
    const errHandler = (e: any) => { this.showError(e); checkDone(); };

    this.api.getProductsSummary(f.categoryId).subscribe({
      next: (res) => { this.productsSummary = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getProductsMovements(f.dateFrom, f.dateTo, f.productId).subscribe({
      next: (res) => { this.productsMovements = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getProductsCharts(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { 
        const items = res.items || [];
        if (items.length) {
          this.isChartEmpty = false;
          this.cdr.detectChanges();
          this.chartTimeoutId = setTimeout(() => {
            this.renderChart('bar', items.map(i => i.productName), items.map(i => i.totalSalesRevenueUsd), 'إيرادات المبيعات (USD)');
          });
        } else {
          this.isChartEmpty = true;
        }
        checkDone(); 
      },
      error: errHandler
    });
  }

  private loadEmployees(f: any) {
    let pending = 3;
    const checkDone = () => { pending--; if (pending === 0) this.isLoading = false; };
    const errHandler = (e: any) => { this.showError(e); checkDone(); };

    this.api.getEmployeesSummary(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { this.employeesSummary = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getEmployeesActivity(f.dateFrom, f.dateTo, f.employeeId).subscribe({
      next: (res) => { this.employeesActivity = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getEmployeesCharts(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { 
        const items = res.items || [];
        if (items.length) {
          this.isChartEmpty = false;
          this.cdr.detectChanges();
          this.chartTimeoutId = setTimeout(() => {
            this.renderChart('bar', items.map(i => i.employeeName), items.map(i => i.totalSalesRevenueUsd), 'المبيعات حسب الموظف (USD)');
          });
        } else {
          this.isChartEmpty = true;
        }
        checkDone(); 
      },
      error: errHandler
    });
  }

  private loadInventory(f: any) {
    let pending = 3;
    const checkDone = () => { pending--; if (pending === 0) this.isLoading = false; };
    const errHandler = (e: any) => { this.showError(e); checkDone(); };

    this.api.getInventorySummary(f.categoryId).subscribe({
      next: (res) => { this.inventorySummary = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getInventoryBatches(f.dateFrom, f.dateTo).subscribe({
      next: (res) => { this.inventoryBatches = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getInventoryCharts().subscribe({
      next: (res) => { 
        const items = res.items || [];
        if (items.length) {
          this.isChartEmpty = false;
          this.cdr.detectChanges();
          this.chartTimeoutId = setTimeout(() => {
            this.renderChart('doughnut', items.map(i => i.categoryName), items.map(i => i.totalStockValueUsd), 'قيمة المخزون حسب التصنيف (USD)');
          });
        } else {
          this.isChartEmpty = true;
        }
        checkDone(); 
      },
      error: errHandler
    });
  }

  private loadExpiry() {
    let pending = 3;
    const checkDone = () => { pending--; if (pending === 0) this.isLoading = false; };
    const errHandler = (e: any) => { this.showError(e); checkDone(); };

    this.api.getExpirySummary().subscribe({
      next: (res) => { this.expirySummary = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getExpiryBatches().subscribe({
      next: (res) => { this.expiryBatches = res.items || []; checkDone(); },
      error: errHandler
    });

    this.api.getExpiryCharts().subscribe({
      next: (res) => { 
        const items = res.items || [];
        if (items.length) {
          this.isChartEmpty = false;
          this.cdr.detectChanges();
          this.chartTimeoutId = setTimeout(() => {
            this.renderChart('doughnut', items.map(i => i.expiryStatus), items.map(i => i.batchCount), 'حالة الدفعات');
          });
        } else {
          this.isChartEmpty = true;
        }
        checkDone(); 
      },
      error: errHandler
    });
  }

  // --- Chart Handling ---

  private destroyChart() {
    if (this.chartTimeoutId) {
      clearTimeout(this.chartTimeoutId);
      this.chartTimeoutId = null;
    }
    if (this.chartInstance) {
      this.chartInstance.destroy();
      this.chartInstance = null;
    }
  }

  private renderChart(type: any, labels: string[], data: number[], label: string) {
    this.destroyChart();
    if (!this.chartCanvas) return;

    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    let colors: string[] = [];
    if (type === 'doughnut') {
      colors = [
        '#ef4444', // red
        '#eab308', // yellow
        '#22c55e', // green
        '#3b82f6', // blue
        '#a855f7', // purple
        '#f97316'  // orange
      ];
    } else {
      colors = ['#3b82f6']; // primary blue
    }

    this.chartInstance = new Chart(ctx, {
      type: type,
      data: {
        labels: labels,
        datasets: [{
          label: label,
          data: data,
          backgroundColor: colors,
          borderColor: type === 'line' ? '#3b82f6' : '#ffffff',
          borderWidth: 2,
          tension: 0.3
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
            labels: { font: { family: 'Cairo, sans-serif' } }
          }
        }
      }
    });
  }

  // --- Error Handling ---

  private showError(err: HttpErrorResponse) {
    this.globalError = 'حدث خطأ أثناء تحميل البيانات. يرجى المحاولة مرة أخرى.';
    setTimeout(() => { this.globalError = ''; }, 5000);
  }
}
