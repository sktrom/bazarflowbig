import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OffersApiService } from '../services/offers-api.service';
import { OfferListItem, OfferProductLookupItem } from '../models/offer.model';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-offers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      
      <!-- Header & Actions -->
      <div class="bg-white p-4 sm:p-6 border-b border-slate-200 shrink-0">
        <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4">
          <h1 class="text-2xl font-bold text-slate-800">العروض</h1>
          <div class="flex gap-3">
            <button class="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md font-medium transition-colors shadow-sm flex items-center gap-2" (click)="exportOffers()" [disabled]="isLoading">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"></path></svg>
              تصدير
            </button>
            <button class="px-4 py-2 bg-primary hover:bg-primary-dark text-white rounded-md font-medium transition-colors shadow-sm flex items-center gap-2" (click)="openCreateModal()">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
              إضافة عرض
            </button>
          </div>
        </div>

        <div class="flex flex-col sm:flex-row gap-4">
          <!-- Search -->
          <div class="relative flex-1">
            <input 
              type="text" 
              [(ngModel)]="searchTerm"
              (ngModelChange)="applyFilters()"
              placeholder="بحث باسم المنتج..." 
              class="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all"
            >
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
            </span>
          </div>

          <!-- Filters -->
          <div class="w-full sm:w-48">
            <select [(ngModel)]="filterStatus" (ngModelChange)="applyFilters()" class="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all bg-white">
              <option [ngValue]="null">كل الحالات</option>
              <option [value]="'active'">نشط</option>
              <option [value]="'cancelled'">ملغى</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="flex-1 overflow-auto p-4 sm:p-6">
        
        <!-- Loading -->
        <div *ngIf="isLoading" class="flex flex-col items-center justify-center h-full space-y-4">
          <div class="w-12 h-12 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
          <p class="text-slate-500 font-medium">جاري التحميل...</p>
        </div>

        <!-- Empty -->
        <div *ngIf="!isLoading && filteredItems.length === 0" class="flex flex-col items-center justify-center h-full text-slate-500">
          <svg class="w-16 h-16 mb-4 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"></path></svg>
          <p class="text-lg font-medium">لا توجد عروض مطابقة</p>
        </div>

        <!-- Data Table -->
        <div *ngIf="!isLoading && filteredItems.length > 0" class="bg-white rounded-xl border border-slate-200 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-sm text-right">
              <thead class="text-xs text-slate-500 bg-slate-50 border-b border-slate-200">
                <tr>
                  <th class="px-6 py-4 font-semibold">المنتج</th>
                  <th class="px-6 py-4 font-semibold">رقم المنتج</th>
                  <th class="px-6 py-4 font-semibold">نوع الخصم</th>
                  <th class="px-6 py-4 font-semibold">قيمة الخصم</th>
                  <th class="px-6 py-4 font-semibold">الحالة</th>
                  <th class="px-6 py-4 font-semibold text-center">إجراءات</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200">
                <tr *ngFor="let item of filteredItems" class="hover:bg-slate-50 transition-colors">
                  <td class="px-6 py-4 font-medium text-slate-800">{{ item.productName }}</td>
                  <td class="px-6 py-4 text-slate-500">{{ item.productId }}</td>
                  <td class="px-6 py-4">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [ngClass]="{
                            'bg-green-100 text-green-800': item.discountType === 'Amount',
                            'bg-blue-100 text-blue-800': item.discountType === 'Percent'
                          }">
                      {{ item.discountType === 'Amount' ? 'مبلغ ثابت' : 'نسبة %' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 font-bold text-slate-700">
                    {{ item.discountType === 'Percent' ? item.discountValue + '%' : (item.discountValue | currency:'USD') }}
                  </td>
                  <td class="px-6 py-4">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [ngClass]="{
                            'bg-green-100 text-green-800': item.isActive,
                            'bg-slate-100 text-slate-600': !item.isActive
                          }">
                      {{ item.isActive ? 'نشط' : 'ملغى' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 text-center">
                    <div class="flex items-center justify-center gap-3">
                      <button class="text-slate-400 hover:text-primary transition-colors" title="تعديل" (click)="openEditModal(item)">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path></svg>
                      </button>
                      <button *ngIf="item.isActive" class="text-slate-400 hover:text-orange-500 transition-colors" title="إلغاء العرض" (click)="openCancelModal(item)">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"></path></svg>
                      </button>
                      <button class="text-slate-400 hover:text-red-600 transition-colors" title="حذف" (click)="openDeleteModal(item)">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <div class="bg-slate-50 border-t border-slate-200 px-6 py-3 flex items-center justify-between">
            <span class="text-sm text-slate-500">إجمالي العروض: <span class="font-semibold text-slate-800">{{ filteredItems.length }}</span></span>
          </div>
        </div>

      </div>

      <!-- Modals Overlay -->
      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-50 flex items-center justify-center p-4">
        
        <!-- Form Modal (Create / Edit) -->
        <div *ngIf="activeModal === 'form'" class="bg-white rounded-xl shadow-xl w-full max-w-md overflow-hidden flex flex-col">
          <div class="px-6 py-4 border-b border-slate-200 bg-slate-50 flex justify-between items-center">
            <h2 class="text-lg font-bold text-slate-800">{{ editId ? 'تعديل عرض' : 'إضافة عرض جديد' }}</h2>
            <button (click)="closeModal()" class="text-slate-400 hover:text-slate-600 transition-colors">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            
            <div *ngIf="formError" class="bg-red-50 text-red-600 p-3 rounded-md text-sm mb-4">
              {{ formError }}
            </div>

            <!-- Product lookup (create mode) -->
            <div *ngIf="!editId">
              <label class="block text-sm font-medium text-slate-700 mb-1">المنتج <span class="text-red-500">*</span></label>
              <div *ngIf="selectedProduct" class="flex items-center justify-between bg-green-50 border border-green-200 rounded-md px-3 py-2 mb-2">
                <div>
                  <span class="font-medium text-green-800">{{ selectedProduct.name }}</span>
                  <span class="text-xs text-green-600 mr-2">{{ selectedProduct.barcode }}</span>
                </div>
                <button type="button" (click)="clearSelectedProduct()" class="text-green-600 hover:text-red-500 transition-colors text-lg leading-none">&times;</button>
              </div>
              <div *ngIf="!selectedProduct" class="relative">
                <input type="text" [(ngModel)]="productSearchTerm" (ngModelChange)="onProductSearch($event)"
                  placeholder="ابحث باسم المنتج أو الباركود"
                  class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all">
                <div *ngIf="productLookupResults.length > 0" class="absolute z-10 w-full bg-white border border-slate-200 rounded-md shadow-lg mt-1 max-h-52 overflow-y-auto">
                  <button *ngFor="let p of productLookupResults" type="button"
                    (click)="selectProduct(p)"
                    class="w-full text-right px-3 py-2 hover:bg-slate-50 transition-colors flex items-center justify-between border-b border-slate-100 last:border-0">
                    <span class="font-medium text-slate-800">{{ p.name }}</span>
                    <span class="text-xs text-slate-500">{{ p.barcode }} &mdash; {{ p.priceUsd | currency:'USD' }}</span>
                  </button>
                </div>
                <p *ngIf="productSearchLoading" class="text-xs text-slate-400 mt-1">جاري البحث...</p>
                <p *ngIf="!formData.productId" class="text-xs text-red-500 mt-1">اختر المنتج أولاً</p>
              </div>
            </div>
            <!-- Product display (edit mode) -->
            <div *ngIf="editId">
              <label class="block text-sm font-medium text-slate-700 mb-1">المنتج</label>
              <div class="px-3 py-2 bg-slate-100 rounded-md text-slate-600 text-sm">{{ formData.productName || 'المنتج #' + formData.productId }}</div>
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">نوع الخصم <span class="text-red-500">*</span></label>
              <select [(ngModel)]="formData.discountType" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all bg-white">
                <option value="Amount">مبلغ ثابت</option>
                <option value="Percent">نسبة %</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">قيمة الخصم <span class="text-red-500">*</span></label>
              <input type="number" [(ngModel)]="formData.discountValue" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all">
            </div>

          </div>
          <div class="px-6 py-4 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
            <button class="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md text-sm font-medium transition-colors" (click)="closeModal()">إلغاء</button>
            <button class="px-4 py-2 bg-primary hover:bg-primary-dark text-white rounded-md text-sm font-medium transition-colors disabled:opacity-50" (click)="saveOffer()" [disabled]="!isValidForm() || isSaving">
              {{ isSaving ? 'جاري الحفظ...' : 'حفظ' }}
            </button>
          </div>
        </div>

        <!-- Cancel Confirm Modal -->
        <div *ngIf="activeModal === 'cancel'" class="bg-white rounded-xl shadow-xl w-full max-w-sm overflow-hidden flex flex-col">
          <div class="px-6 py-4 border-b border-orange-100 bg-orange-50 font-bold text-orange-800">تأكيد إلغاء العرض</div>
          <div class="p-6 text-slate-700 text-sm">
            هل أنت متأكد من رغبتك في إلغاء العرض الحالي للمنتج "{{ actionCandidate?.productName }}"؟
          </div>
          <div class="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-2">
            <button class="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md text-sm font-medium transition-colors" (click)="closeModal()">تراجع</button>
            <button class="px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white rounded-md text-sm font-medium transition-colors disabled:opacity-50" (click)="confirmCancel()" [disabled]="isSaving">تأكيد الإلغاء</button>
          </div>
        </div>

        <!-- Delete Confirm Modal -->
        <div *ngIf="activeModal === 'delete'" class="bg-white rounded-xl shadow-xl w-full max-w-sm overflow-hidden flex flex-col">
          <div class="px-6 py-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد حذف العرض</div>
          <div class="p-6 text-slate-700 text-sm">
            هل أنت متأكد من رغبتك في حذف العرض نهائياً للمنتج "{{ actionCandidate?.productName }}"؟
          </div>
          <div class="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-2">
            <button class="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md text-sm font-medium transition-colors" (click)="closeModal()">تراجع</button>
            <button class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-md text-sm font-medium transition-colors disabled:opacity-50" (click)="confirmDelete()" [disabled]="isSaving">حذف نهائي</button>
          </div>
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
export class OffersComponent implements OnInit {
  items: OfferListItem[] = [];
  filteredItems: OfferListItem[] = [];
  
  isLoading = false;
  isSaving = false;
  globalError = '';

  searchTerm = '';
  filterStatus: 'active' | 'cancelled' | null = null;

  activeModal: 'form' | 'cancel' | 'delete' | null = null;
  editId: number | null = null;
  formData: any = {};
  formError = '';

  actionCandidate: OfferListItem | null = null;

  // Product lookup state
  productSearchTerm = '';
  productLookupResults: OfferProductLookupItem[] = [];
  productSearchLoading = false;
  selectedProduct: OfferProductLookupItem | null = null;
  private productSearch$ = new Subject<string>();

  constructor(private api: OffersApiService) {}

  ngOnInit() {
    this.loadOffers();
    this.productSearch$.pipe(debounceTime(300), distinctUntilChanged()).subscribe(term => {
      this.runProductSearch(term);
    });
  }

  loadOffers() {
    this.isLoading = true;
    this.api.getAll().subscribe({
      next: (res) => {
        this.items = res.items || [];
        this.applyFilters();
        this.isLoading = false;
      },
      error: () => {
        this.globalError = 'حدث خطأ أثناء جلب العروض';
        this.isLoading = false;
      }
    });
  }

  applyFilters() {
    let temp = this.items;

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      temp = temp.filter(o => o.productName.toLowerCase().includes(term));
    }

    if (this.filterStatus) {
      const isAct = this.filterStatus === 'active';
      temp = temp.filter(o => o.isActive === isAct);
    }

    this.filteredItems = temp;
  }

  // --- Create / Edit ---
  openCreateModal() {
    this.editId = null;
    this.formData = { productId: null, discountType: 'Amount', discountValue: null };
    this.formError = '';
    this.selectedProduct = null;
    this.productSearchTerm = '';
    this.productLookupResults = [];
    this.activeModal = 'form';
    // Pre-load top 20
    this.runProductSearch('');
  }

  openEditModal(item: OfferListItem) {
    this.editId = item.id;
    this.formData = { 
      productId: item.productId,
      productName: item.productName,
      discountType: item.discountType, 
      discountValue: item.discountValue 
    };
    this.formError = '';
    this.selectedProduct = null;
    this.productSearchTerm = '';
    this.productLookupResults = [];
    this.activeModal = 'form';
  }

  isValidForm(): boolean {
    return !!(this.formData.productId && this.formData.discountType && this.formData.discountValue > 0);
  }

  saveOffer() {
    if (!this.isValidForm()) return;

    this.isSaving = true;
    this.formError = '';

    const req = {
      productId: Number(this.formData.productId),
      discountType: this.formData.discountType,
      discountValue: Number(this.formData.discountValue)
    };

    if (this.editId) {
      this.api.update(this.editId, req).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.loadOffers();
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving = false;
          this.handleFormError(err);
        }
      });
    } else {
      this.api.create(req).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.loadOffers();
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving = false;
          this.handleFormError(err);
        }
      });
    }
  }

  onProductSearch(term: string) {
    this.productSearch$.next(term);
  }

  private runProductSearch(term: string) {
    this.productSearchLoading = true;
    this.api.productsLookup(term).subscribe({
      next: (res) => {
        this.productLookupResults = res.items || [];
        this.productSearchLoading = false;
      },
      error: () => { this.productSearchLoading = false; }
    });
  }

  selectProduct(p: OfferProductLookupItem) {
    this.selectedProduct = p;
    this.formData.productId = p.productId;
    this.productLookupResults = [];
    this.productSearchTerm = '';
  }

  clearSelectedProduct() {
    this.selectedProduct = null;
    this.formData.productId = null;
    this.productSearchTerm = '';
    this.productLookupResults = [];
    this.runProductSearch('');
  }

  private handleFormError(err: HttpErrorResponse) {
    const errorMsg = err.error?.error || '';
    if (errorMsg === 'PRODUCT_NOT_FOUND') {
      this.formError = 'المنتج غير موجود، تأكد من اختيار المنتج الصحيح';
    } else if (errorMsg === 'INVALID_DISCOUNT_TYPE') {
      this.formError = 'نوع الخصم غير صحيح';
    } else if (errorMsg === 'OFFER_NOT_FOUND') {
      this.formError = 'العرض غير موجود';
    } else {
      this.formError = 'حدث خطأ أثناء تنفيذ العملية';
    }
  }

  // --- Cancel ---
  openCancelModal(item: OfferListItem) {
    this.actionCandidate = item;
    this.activeModal = 'cancel';
  }

  confirmCancel() {
    if (!this.actionCandidate) return;
    this.isSaving = true;
    this.api.cancel(this.actionCandidate.id).subscribe({
      next: () => {
        this.isSaving = false;
        // update locally
        if (this.actionCandidate) {
          this.actionCandidate.isActive = false;
          this.applyFilters();
        }
        this.closeModal();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving = false;
        this.closeModal();
        this.showGlobalError(err);
      }
    });
  }

  // --- Delete ---
  openDeleteModal(item: OfferListItem) {
    this.actionCandidate = item;
    this.activeModal = 'delete';
  }

  confirmDelete() {
    if (!this.actionCandidate) return;
    this.isSaving = true;
    this.api.delete(this.actionCandidate.id).subscribe({
      next: () => {
        this.isSaving = false;
        // remove locally
        this.items = this.items.filter(i => i.id !== this.actionCandidate?.id);
        this.applyFilters();
        this.closeModal();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving = false;
        this.closeModal();
        this.showGlobalError(err);
      }
    });
  }

  private showGlobalError(err: HttpErrorResponse) {
    const errorMsg = err.error?.error || '';
    if (errorMsg === 'CANNOT_DELETE_USED_OFFER' || errorMsg === 'CANNOT_DELETE_LEGACY_OFFER') {
      this.globalError = 'لا يمكن حذف هذا العرض لأنه مستخدم أو قديم، يمكن إلغاؤه فقط';
    } else if (errorMsg === 'OFFER_NOT_FOUND') {
      this.globalError = 'العرض غير موجود';
    } else {
      this.globalError = 'حدث خطأ أثناء تنفيذ العملية';
    }
    
    // Auto clear global error after 5s
    setTimeout(() => { this.globalError = ''; }, 5000);
  }

  exportOffers() {
    const request = {
      format: 'excel',
      isActive: this.filterStatus ? (this.filterStatus === 'active') : undefined
    };

    this.api.exportOffers(request).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;

        let filename = 'offers_export.xlsx';
        const contentDisposition = response.headers.get('Content-Disposition');
        if (contentDisposition) {
          const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
          const matches = filenameRegex.exec(contentDisposition);
          if (matches != null && matches[1]) {
            filename = matches[1].replace(/['"]/g, '');
          }
        }

        // Trigger download
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.globalError = 'فشل تصدير العروض';
        setTimeout(() => { this.globalError = ''; }, 5000);
      }
    });
  }

  closeModal() {
    this.activeModal = null;
    this.formError = '';
    this.actionCandidate = null;
    this.selectedProduct = null;
    this.productSearchTerm = '';
    this.productLookupResults = [];
  }
}
