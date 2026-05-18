import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryApiService } from '../services/inventory-api.service';
import { InventoryListItemDto, InventoryDetailsResponse } from '../models/inventory.model';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      
      <!-- Header & Filters -->
      <div class="bg-white p-4 sm:p-6 border-b border-slate-200 shrink-0">
        <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4">
          <h1 class="text-2xl font-bold text-slate-800">إدارة المخزون</h1>
        </div>

        <div class="flex flex-col sm:flex-row gap-4">
          <!-- Search -->
          <div class="relative flex-1">
            <input 
              type="text" 
              [(ngModel)]="searchQuery"
              (ngModelChange)="onSearchChange($event)"
              placeholder="بحث باسم المنتج أو الباركود..." 
              class="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all"
            >
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
            </span>
          </div>

          <!-- Filters -->
          <div class="flex gap-2 items-center text-sm">
            <label class="flex items-center gap-1 cursor-pointer">
              <input type="checkbox" [(ngModel)]="filterHasStock" (change)="loadInventory()" class="rounded border-slate-300 text-primary focus:ring-primary">
              <span>متوفر فقط</span>
            </label>
            <label class="flex items-center gap-1 cursor-pointer">
              <input type="checkbox" [(ngModel)]="filterHasExpiry" (change)="loadInventory()" class="rounded border-slate-300 text-primary focus:ring-primary">
              <span>له صلاحية</span>
            </label>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="flex-1 overflow-auto p-4 sm:p-6">
        
        <!-- Loading -->
        <div *ngIf="isLoading" class="flex flex-col items-center justify-center h-full space-y-4">
          <div class="w-12 h-12 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
          <p class="text-slate-500 font-medium">جاري تحميل المخزون...</p>
        </div>

        <!-- Error -->
        <div *ngIf="error" class="bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          <span class="font-medium">{{ error }}</span>
          <button (click)="loadInventory()" class="mr-auto text-sm bg-red-100 hover:bg-red-200 px-3 py-1 rounded transition-colors">إعادة المحاولة</button>
        </div>

        <!-- Empty -->
        <div *ngIf="!isLoading && !error && items.length === 0" class="flex flex-col items-center justify-center h-full text-slate-500">
          <svg class="w-16 h-16 mb-4 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"></path></svg>
          <p class="text-lg font-medium">لا توجد منتجات تطابق معايير البحث</p>
        </div>

        <!-- Data Table -->
        <div *ngIf="!isLoading && !error && items.length > 0" class="bg-white rounded-xl border border-slate-200 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-sm text-right">
              <thead class="text-xs text-slate-500 bg-slate-50 border-b border-slate-200">
                <tr>
                  <th class="px-6 py-4 font-semibold">المنتج</th>
                  <th class="px-6 py-4 font-semibold">التصنيف</th>
                  <th class="px-6 py-4 font-semibold">الكمية المتوفرة</th>
                  <th class="px-6 py-4 font-semibold">حالة المخزون</th>
                  <th class="px-6 py-4 font-semibold">عدد الدفعات</th>
                  <th class="px-6 py-4 font-semibold">أقرب صلاحية</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200">
                <tr *ngFor="let item of items" 
                    (click)="openDetails(item.productId)"
                    class="hover:bg-slate-50 transition-colors cursor-pointer group">
                  <td class="px-6 py-4">
                    <div class="font-semibold text-slate-800">{{ item.productName }}</div>
                    <div class="text-xs text-slate-500 mt-1">{{ item.barcode }}</div>
                  </td>
                  <td class="px-6 py-4 text-slate-600">{{ item.categoryName || 'غير محدد' }}</td>
                  <td class="px-6 py-4 font-medium" [class.text-red-600]="item.totalQuantityAvailable <= 0">
                    {{ item.totalQuantityAvailable }} <span class="text-xs text-slate-500 font-normal">{{ item.baseUnit }}</span>
                  </td>
                  <td class="px-6 py-4">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [ngClass]="{
                            'bg-green-100 text-green-800': item.stockStatus === 'InStock',
                            'bg-red-100 text-red-800': item.stockStatus === 'OutOfStock',
                            'bg-yellow-100 text-yellow-800': item.stockStatus === 'LowStock'
                          }">
                      {{ getStockStatusLabel(item.stockStatus) }}
                    </span>
                  </td>
                  <td class="px-6 py-4 text-slate-600">
                    {{ item.batchCount }}
                  </td>
                  <td class="px-6 py-4">
                    <div *ngIf="item.hasExpiry">
                      <div class="text-slate-800">{{ item.nearestExpiryDate ? (item.nearestExpiryDate | date:'yyyy-MM-dd') : 'غير محدد' }}</div>
                      <span *ngIf="item.expiryStatus" 
                            class="inline-flex items-center mt-1 px-2 py-0.5 rounded text-[10px] font-medium"
                            [ngClass]="{
                              'bg-green-100 text-green-800': item.expiryStatus === 'Valid',
                              'bg-yellow-100 text-yellow-800': item.expiryStatus === 'ExpiringSoon',
                              'bg-red-100 text-red-800': item.expiryStatus === 'Expired'
                            }">
                        {{ getExpiryStatusLabel(item.expiryStatus) }}
                      </span>
                    </div>
                    <span *ngIf="!item.hasExpiry" class="text-slate-400 text-xs">لا يوجد</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <!-- Pagination -->
          <div class="bg-slate-50 border-t border-slate-200 px-6 py-3 flex items-center justify-between">
            <span class="text-sm text-slate-500">
              إجمالي المنتجات: <span class="font-semibold text-slate-800">{{ totalCount }}</span>
            </span>
            <div class="flex gap-2">
              <button 
                [disabled]="page === 1" 
                (click)="changePage(page - 1)"
                class="px-3 py-1.5 border border-slate-300 rounded text-sm font-medium text-slate-700 bg-white hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed">
                السابق
              </button>
              <button 
                [disabled]="items.length < pageSize"
                (click)="changePage(page + 1)"
                class="px-3 py-1.5 border border-slate-300 rounded text-sm font-medium text-slate-700 bg-white hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed">
                التالي
              </button>
            </div>
          </div>
        </div>

      </div>

      <!-- Details Modal Overlay -->
      <div *ngIf="selectedProductId" class="fixed inset-0 bg-slate-900/50 z-50 flex items-center justify-center p-4">
        <div class="bg-white rounded-xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
          
          <!-- Modal Header -->
          <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between bg-slate-50">
            <h2 class="text-lg font-bold text-slate-800">تفاصيل المخزون</h2>
            <button (click)="closeDetails()" class="text-slate-400 hover:text-slate-600 transition-colors">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>

          <!-- Modal Body -->
          <div class="p-6 overflow-y-auto flex-1 bg-slate-50/50">
            
            <div *ngIf="detailsLoading" class="flex justify-center py-12">
              <div class="w-10 h-10 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
            </div>

            <div *ngIf="detailsError" class="bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
              <span>{{ detailsError }}</span>
            </div>

            <div *ngIf="productDetails && !detailsLoading && !detailsError" class="space-y-6">
              
              <!-- Summary Cards -->
              <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm flex items-start gap-4">
                  <div class="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center text-blue-600 shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>
                  </div>
                  <div>
                    <h3 class="text-sm font-medium text-slate-500 mb-1">المنتج</h3>
                    <p class="font-bold text-slate-800">{{ productDetails.productName }}</p>
                    <p class="text-xs text-slate-400 mt-0.5">{{ productDetails.barcode }}</p>
                  </div>
                </div>

                <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm flex items-start gap-4">
                  <div class="w-10 h-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600 shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4"></path></svg>
                  </div>
                  <div>
                    <h3 class="text-sm font-medium text-slate-500 mb-1">الكمية الإجمالية</h3>
                    <p class="font-bold text-slate-800 text-lg">{{ productDetails.totalQuantityAvailable }} <span class="text-sm font-normal text-slate-500">{{ productDetails.baseUnit }}</span></p>
                  </div>
                </div>

                <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm flex items-start gap-4">
                  <div class="w-10 h-10 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600 shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                  </div>
                  <div>
                    <h3 class="text-sm font-medium text-slate-500 mb-1">السعر</h3>
                    <p class="font-bold text-slate-800 text-lg">{{ productDetails.priceUsd | currency:'USD' }}</p>
                  </div>
                </div>
              </div>

              <!-- Batches Table -->
              <div class="bg-white rounded-xl border border-slate-200 overflow-hidden shadow-sm">
                <div class="px-6 py-4 border-b border-slate-200 bg-slate-50 flex items-center justify-between">
                  <h3 class="font-bold text-slate-800">تفاصيل الدفعات</h3>
                  <span class="text-xs text-slate-500">{{ productDetails.batches.length }} دفعة</span>
                </div>
                
                <div *ngIf="productDetails.batches.length === 0" class="p-8 text-center text-slate-500">
                  لا توجد دفعات مسجلة في المخزون لهذا المنتج.
                </div>

                <div *ngIf="productDetails.batches.length > 0" class="overflow-x-auto">
                  <table class="w-full text-sm text-right">
                    <thead class="text-xs text-slate-500 bg-slate-50 border-b border-slate-200">
                      <tr>
                        <th class="px-6 py-3 font-semibold">رقم الدفعة</th>
                        <th class="px-6 py-3 font-semibold">تاريخ الدخول</th>
                        <th class="px-6 py-3 font-semibold">الكمية الأصلية</th>
                        <th class="px-6 py-3 font-semibold">الكمية المتوفرة</th>
                        <th class="px-6 py-3 font-semibold" *ngIf="productDetails.hasExpiry">الصلاحية</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-200">
                      <tr *ngFor="let batch of productDetails.batches" class="hover:bg-slate-50 transition-colors">
                        <td class="px-6 py-3 font-mono text-slate-600">#{{ batch.batchId }}</td>
                        <td class="px-6 py-3 text-slate-600">{{ batch.entryDate ? (batch.entryDate | date:'yyyy-MM-dd') : '-' }}</td>
                        <td class="px-6 py-3 text-slate-600">{{ batch.quantityReceived }}</td>
                        <td class="px-6 py-3 font-medium" [class.text-red-600]="batch.quantityAvailable <= 0">{{ batch.quantityAvailable }}</td>
                        <td class="px-6 py-3" *ngIf="productDetails.hasExpiry">
                          <div class="text-slate-800">{{ batch.expiryDate ? (batch.expiryDate | date:'yyyy-MM-dd') : '-' }}</div>
                          <div *ngIf="batch.daysUntilExpiry !== undefined" class="text-xs mt-0.5"
                               [ngClass]="{
                                 'text-green-600': (batch.daysUntilExpiry || 0) > 30,
                                 'text-yellow-600': (batch.daysUntilExpiry || 0) > 0 && (batch.daysUntilExpiry || 0) <= 30,
                                 'text-red-600 font-bold': (batch.daysUntilExpiry || 0) <= 0
                               }">
                            {{ (batch.daysUntilExpiry || 0) > 0 ? ('متبقي ' + batch.daysUntilExpiry + ' يوم') : 'منتهي الصلاحية' }}
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

            </div>
          </div>
          
        </div>
      </div>

    </div>
  `
})
export class InventoryListComponent implements OnInit {
  items: InventoryListItemDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  
  isLoading = false;
  error = '';

  searchQuery = '';
  filterHasStock = false;
  filterHasExpiry = false;

  private searchSubject = new Subject<string>();

  // Details Modal State
  selectedProductId: number | null = null;
  productDetails: InventoryDetailsResponse | null = null;
  detailsLoading = false;
  detailsError = '';

  constructor(private inventoryApi: InventoryApiService) {}

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.page = 1;
      this.loadInventory();
    });

    this.loadInventory();
  }

  loadInventory(): void {
    this.isLoading = true;
    this.error = '';
    
    this.inventoryApi.getInventoryList({
      search: this.searchQuery || undefined,
      hasStock: this.filterHasStock || undefined,
      hasExpiry: this.filterHasExpiry || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.items = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading inventory:', err);
        this.error = 'حدث خطأ أثناء جلب البيانات. يرجى المحاولة لاحقاً.';
        this.isLoading = false;
      }
    });
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  changePage(newPage: number): void {
    if (newPage < 1) return;
    this.page = newPage;
    this.loadInventory();
  }

  openDetails(productId: number): void {
    this.selectedProductId = productId;
    this.detailsLoading = true;
    this.detailsError = '';
    this.productDetails = null;

    this.inventoryApi.getInventoryDetails(productId).subscribe({
      next: (res) => {
        this.productDetails = res;
        this.detailsLoading = false;
      },
      error: (err) => {
        console.error('Error loading product details:', err);
        if (err.status === 404) {
          this.detailsError = 'المنتج غير موجود أو محذوف.';
        } else {
          this.detailsError = 'حدث خطأ أثناء جلب تفاصيل المنتج.';
        }
        this.detailsLoading = false;
      }
    });
  }

  closeDetails(): void {
    this.selectedProductId = null;
    this.productDetails = null;
  }

  getStockStatusLabel(status: string): string {
    switch (status) {
      case 'InStock': return 'متوفر';
      case 'LowStock': return 'منخفض';
      case 'OutOfStock': return 'نفد';
      default: return status;
    }
  }

  getExpiryStatusLabel(status: string): string {
    switch (status) {
      case 'Valid': return 'صالح';
      case 'ExpiringSoon': return 'قريب الانتهاء';
      case 'Expired': return 'منتهي الصلاحية';
      default: return status;
    }
  }
}
