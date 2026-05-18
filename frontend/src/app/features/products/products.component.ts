import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductsStateService } from './services/products-state.service';
import { ProductsApiService, ProductListItem, ProductDetailResponse, BatchItem, CreateProductRequest, UpdateProductRequest } from './services/products-api.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      
      <!-- Top Header & Actions -->
      <div class="flex justify-between items-center mb-6 shrink-0">
        <div>
          <h1 class="text-2xl font-bold text-slate-900">المنتجات</h1>
          <p class="text-sm text-slate-500 mt-1">إدارة بيانات المنتجات والدفعات التخزينية</p>
        </div>
        <div class="flex gap-3">
          <button class="btn-secondary" (click)="exportProducts()" [disabled]="state.isLoading">
            <svg class="w-4 h-4 ml-2 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"></path></svg>
            تصدير
          </button>
          <button class="btn-primary" (click)="openCreateModal()">
            <svg class="w-4 h-4 ml-2 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            إضافة منتج
          </button>
        </div>
      </div>

      <!-- Toolbar (Search & Filters) -->
      <div class="bg-white p-4 rounded-lg shadow-sm border border-slate-200 mb-4 shrink-0 flex flex-wrap gap-4 items-center">
        <div class="relative flex-1 min-w-[200px]">
          <input type="text" [(ngModel)]="searchTerm" (ngModelChange)="applyFilters()" placeholder="بحث بالاسم أو الباركود..." class="input-field pl-10">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
          </span>
        </div>
        
        <div class="w-48">
          <select [(ngModel)]="filterCategory" (ngModelChange)="applyFilters()" class="input-field">
            <option [ngValue]="null">كل التصنيفات</option>
            <option *ngFor="let cat of state.categories" [value]="cat.id">{{ cat.name }}</option>
          </select>
        </div>

        <div class="w-40">
          <select [(ngModel)]="filterStatus" (ngModelChange)="applyFilters()" class="input-field">
            <option [ngValue]="null">كل الحالات</option>
            <option [value]="'active'">نشط</option>
            <option [value]="'inactive'">غير نشط</option>
          </select>
        </div>
      </div>

      <!-- Main Table -->
      <div class="bg-white rounded-lg shadow-sm border border-slate-200 flex-1 overflow-hidden flex flex-col">
        <div class="overflow-x-auto flex-1">
          <table class="w-full text-right text-sm">
            <thead class="bg-slate-50 text-slate-500 font-medium border-b border-slate-200 sticky top-0">
              <tr>
                <th class="py-3 px-4 w-16 text-center">رقم المنتج</th>
                <th class="py-3 px-4">الاسم</th>
                <th class="py-3 px-4">الباركود</th>
                <th class="py-3 px-4">التصنيف</th>
                <th class="py-3 px-4">السعر</th>
                <th class="py-3 px-4 text-center">الحالة</th>
                <th class="py-3 px-4 text-center w-24">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="state.isLoading && !filteredProducts.length">
                <td colspan="7" class="py-8 text-center text-slate-400">جاري التحميل...</td>
              </tr>
              <tr *ngIf="!state.isLoading && !filteredProducts.length">
                <td colspan="7" class="py-8 text-center text-slate-400">لا توجد بيانات مطابقة</td>
              </tr>
              <tr *ngFor="let p of filteredProducts" class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 text-center font-mono text-slate-500 text-xs">{{ p.id }}</td>
                <td class="py-3 px-4 font-medium">{{ p.name }}</td>
                <td class="py-3 px-4 text-slate-500">{{ p.barcode }}</td>
                <td class="py-3 px-4 text-slate-500">{{ p.categoryName }}</td>
                <td class="py-3 px-4">{{ p.priceUsd | currency:'USD' }}</td>
                <td class="py-3 px-4 text-center">
                  <span class="px-2 py-1 rounded-full text-xs font-medium" [class.bg-green-100]="p.isActive" [class.text-green-800]="p.isActive" [class.bg-slate-100]="!p.isActive" [class.text-slate-600]="!p.isActive">
                    {{ p.isActive ? 'نشط' : 'غير نشط' }}
                  </span>
                </td>
                <td class="py-3 px-4 text-center">
                  <!-- Row Actions Dropdown (Simplified inline for now) -->
                  <div class="flex items-center justify-center gap-2">
                    <button class="text-slate-400 hover:text-primary transition-colors" title="تفاصيل ودفعات" (click)="openDetailsModal(p.id)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path></svg>
                    </button>
                    <button class="text-slate-400 hover:text-primary transition-colors" title="تعديل" (click)="openEditModal(p)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path></svg>
                    </button>
                    <button class="text-slate-400 hover:text-red-600 transition-colors" title="حذف" (click)="openDeleteModal(p)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <!-- Pagination Stub -->
        <div class="bg-slate-50 p-3 border-t border-slate-200 text-sm text-slate-500 flex justify-between items-center">
          <span>إجمالي المنتجات: {{ filteredProducts.length }}</span>
          <!-- Future pagination controls -->
        </div>
      </div>

      <!-- Error Toast -->
      <div *ngIf="state.error" class="fixed bottom-4 left-4 bg-red-600 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span class="text-sm">{{ state.error }}</span>
        <button (click)="productsState.clearError()" class="text-white/80 hover:text-white">x</button>
      </div>

      <!-- Modals Overlay -->
      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-40 flex items-center justify-center p-4">
        
        <!-- Create/Edit Modal -->
        <div *ngIf="activeModal === 'form'" class="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0">
            {{ editId ? 'تعديل منتج' : 'إضافة منتج جديد' }}
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-slate-600 mb-1">الاسم <span class="text-red-500">*</span></label>
                <input type="text" [(ngModel)]="formData.name" class="input-field">
              </div>
              <div>
                <label class="block text-slate-600 mb-1">الباركود <span class="text-red-500">*</span></label>
                <input type="text" [(ngModel)]="formData.barcode" class="input-field" [class.border-red-500]="barcodeError">
                <span *ngIf="barcodeError" class="text-xs text-red-500 mt-1">هذا الباركود مستخدم بالفعل</span>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-slate-600 mb-1">التصنيف <span class="text-red-500">*</span></label>
                <select [(ngModel)]="formData.categoryId" class="input-field">
                  <option [ngValue]="null" disabled>اختر التصنيف</option>
                  <option *ngFor="let cat of state.categories" [value]="cat.id">{{ cat.name }}</option>
                </select>
              </div>
              <div>
                <label class="block text-slate-600 mb-1">الوحدة الأساسية (مثال: حبة) <span class="text-red-500">*</span></label>
                <input type="text" [(ngModel)]="formData.baseUnit" class="input-field">
              </div>
            </div>

            <div>
              <label class="block text-slate-600 mb-1">السعر الأساسي (USD) <span class="text-red-500">*</span></label>
              <input type="number" [(ngModel)]="formData.priceUsd" class="input-field w-1/2">
            </div>

            <div class="border-t border-slate-100 pt-4 mt-4">
              <label class="flex items-center gap-2 cursor-pointer mb-3">
                <input type="checkbox" [(ngModel)]="formData.hasCarton" class="rounded border-slate-300 text-primary focus:ring-primary">
                <span class="font-medium text-slate-700">تفعيل بيع الكرتونة</span>
              </label>
              <div class="grid grid-cols-2 gap-4" *ngIf="formData.hasCarton">
                <div>
                  <label class="block text-slate-600 mb-1">عدد الحبات بالكرتونة <span class="text-red-500">*</span></label>
                  <input type="number" [(ngModel)]="formData.cartonQuantity" class="input-field">
                </div>
                <div>
                  <label class="block text-slate-600 mb-1">سعر الكرتونة (USD) <span class="text-red-500">*</span></label>
                  <input type="number" [(ngModel)]="formData.cartonPriceUsd" class="input-field">
                </div>
              </div>
            </div>

            <div class="border-t border-slate-100 pt-4 mt-4 flex justify-between items-center">
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" [(ngModel)]="formData.hasExpiry" class="rounded border-slate-300 text-primary focus:ring-primary">
                <span class="font-medium text-slate-700">هذا المنتج له تاريخ صلاحية</span>
              </label>
              
              <label class="flex items-center gap-2 cursor-pointer" *ngIf="editId">
                <input type="checkbox" [(ngModel)]="formData.isActive" class="rounded border-slate-300 text-primary focus:ring-primary">
                <span class="font-medium text-slate-700">المنتج نشط</span>
              </label>
            </div>

          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end shrink-0 border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveProduct()" [disabled]="state.isLoading || !isValidForm()">حفظ</button>
          </div>
        </div>

        <!-- Details & Batches Modal -->
        <div *ngIf="activeModal === 'details'" class="bg-white rounded-lg shadow-xl w-full max-w-3xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0 flex justify-between items-center">
            <span>تفاصيل المنتج: {{ detailsData?.name }}</span>
            <button class="text-slate-400 hover:text-slate-600" (click)="closeModal()">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>
          
          <div class="p-6 overflow-y-auto flex-1 text-sm bg-slate-50 space-y-6">
            
            <!-- Read-only Summary -->
            <div class="bg-white p-4 rounded border border-slate-200 grid grid-cols-3 gap-4">
              <div><span class="text-slate-500 block text-xs">الباركود</span><span class="font-medium">{{ detailsData?.barcode }}</span></div>
              <div><span class="text-slate-500 block text-xs">السعر الأساسي</span><span class="font-medium">{{ detailsData?.priceUsd | currency:'USD' }}</span></div>
              <div><span class="text-slate-500 block text-xs">الوحدة</span><span class="font-medium">{{ detailsData?.baseUnit }}</span></div>
              <div *ngIf="detailsData?.hasCarton"><span class="text-slate-500 block text-xs">الكرتونة</span><span class="font-medium">{{ detailsData?.cartonQuantity }} حبة بـ {{ detailsData?.cartonPriceUsd | currency:'USD' }}</span></div>
              <div><span class="text-slate-500 block text-xs">الصلاحية</span><span class="font-medium">{{ detailsData?.hasExpiry ? 'مطلوبة' : 'غير مطلوبة' }}</span></div>
              <div><span class="text-slate-500 block text-xs">الحالة</span><span class="font-medium">{{ detailsData?.isActive ? 'نشط' : 'غير نشط' }}</span></div>
            </div>

            <!-- Batches Section -->
            <div>
              <div class="flex justify-between items-center mb-3">
                <h3 class="font-bold text-slate-700">الدفعات التخزينية (Batches)</h3>
                <button class="btn-primary text-xs py-1" (click)="openAddBatchModal()">إضافة دفعة</button>
              </div>
              <div class="bg-white border border-slate-200 rounded overflow-hidden">
                <table class="w-full text-right text-sm">
                  <thead class="bg-slate-50 text-slate-500 border-b border-slate-200">
                    <tr>
                      <th class="py-2 px-3">رقم الدفعة</th>
                      <th class="py-2 px-3">الكمية (متبقي/أصلي)</th>
                      <th class="py-2 px-3" *ngIf="detailsData?.hasExpiry">الانتهاء</th>
                      <th class="py-2 px-3">تاريخ الدخول</th>
                      <th class="py-2 px-3">رقم الفاتورة</th>
                      <th class="py-2 px-3">الحالة</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    <tr *ngIf="!batchesData.length">
                      <td [colSpan]="detailsData?.hasExpiry ? 6 : 5" class="py-4 text-center text-slate-400">لا توجد دفعات متوفرة</td>
                    </tr>
                    <tr *ngFor="let b of batchesData">
                      <td class="py-2 px-3">#{{ b.id }}</td>
                      <td class="py-2 px-3 font-medium text-slate-700">{{ b.quantityAvailable }} / <span class="text-slate-400 text-xs">{{ b.quantityReceived }}</span></td>
                      <td class="py-2 px-3" *ngIf="detailsData?.hasExpiry">
                        <span [class.text-red-500]="isExpired(b.expiryDate)">{{ b.expiryDate | date:'yyyy-MM-dd' || '—' }}</span>
                      </td>
                      <td class="py-2 px-3 text-slate-500">{{ b.entryDate | date:'yyyy-MM-dd' || '—' }}</td>
                      <td class="py-2 px-3 text-slate-500">{{ b.entryInvoiceNumber || '—' }}</td>
                      <td class="py-2 px-3 text-xs">
                        <span *ngIf="b.quantityAvailable <= 0" class="text-orange-500">منتهية</span>
                        <span *ngIf="b.quantityAvailable > 0" class="text-green-600">نشطة</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

          </div>
        </div>

        <!-- Add Batch Modal -->
        <div *ngIf="activeModal === 'addBatch'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden flex flex-col">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800">إضافة دفعة - {{ detailsData?.name }}</div>
          <div class="p-4 space-y-4 text-sm">
            <div>
              <label class="block text-slate-600 mb-1">الكمية المستلمة <span class="text-red-500">*</span></label>
              <input type="number" [(ngModel)]="batchForm.quantityReceived" class="input-field" min="1">
            </div>
            <div>
              <label class="block text-slate-600 mb-1">رقم الفاتورة المرجعي (اختياري)</label>
              <input type="text" [(ngModel)]="batchForm.entryInvoiceNumber" class="input-field">
            </div>
            <div *ngIf="detailsData?.hasExpiry">
              <label class="block text-slate-600 mb-1">تاريخ الانتهاء <span class="text-red-500">*</span></label>
              <input type="date" [(ngModel)]="batchForm.expiryDate" class="input-field" [class.border-red-500]="batchExpiryError">
            </div>
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="activeModal = 'details'">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveBatch()" [disabled]="state.isLoading || !isValidBatchForm()">حفظ الدفعة</button>
          </div>
        </div>

        <!-- Delete Confirm Modal -->
        <div *ngIf="activeModal === 'delete'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden">
          <div class="p-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد الحذف</div>
          <div class="p-4 text-slate-700 text-sm">
            هل أنت متأكد من رغبتك في حذف أو تعطيل المنتج "{{ deleteCandidate?.name }}"؟
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()">إلغاء</button>
            <button class="btn-danger text-sm" (click)="confirmDelete()" [disabled]="state.isLoading">تأكيد</button>
          </div>
        </div>

      </div>

    </div>
  `
})
export class ProductsComponent implements OnInit {
  state = this.productsState['stateObj'];
  filteredProducts: ProductListItem[] = [];

  // Filters
  searchTerm = '';
  filterCategory: number | null = null;
  filterStatus: 'active' | 'inactive' | null = null;

  // Modals
  activeModal: 'form' | 'details' | 'addBatch' | 'delete' | null = null;
  
  // Product Form
  editId: number | null = null;
  formData: any = {};
  barcodeError = false;

  // Details & Batches
  detailsData: ProductDetailResponse | null = null;
  batchesData: BatchItem[] = [];

  // Batch Form
  batchForm: any = {};
  batchExpiryError = false;

  // Delete
  deleteCandidate: ProductListItem | null = null;

  constructor(
    public productsState: ProductsStateService,
    private api: ProductsApiService
  ) {}

  ngOnInit(): void {
    this.productsState.state$.subscribe(s => {
      this.state = s;
      this.applyFilters();
      
      // Clear barcode error if state error is cleared
      if (!s.error) {
        this.barcodeError = false;
      } else if (s.error.includes('باركود')) {
        this.barcodeError = true;
      }
    });
    this.productsState.loadInitialData();
  }

  // --- Filtering (Current View) ---
  applyFilters() {
    let temp = this.state.products || [];
    
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      temp = temp.filter(p => p.name.toLowerCase().includes(term) || p.barcode.toLowerCase().includes(term));
    }

    if (this.filterCategory) {
      temp = temp.filter(p => p.categoryId === this.filterCategory);
    }

    if (this.filterStatus) {
      const isActive = this.filterStatus === 'active';
      temp = temp.filter(p => p.isActive === isActive);
    }

    this.filteredProducts = temp;
  }

  // --- Create / Edit ---
  openCreateModal() {
    this.editId = null;
    this.formData = {
      name: '', barcode: '', categoryId: null, baseUnit: '', priceUsd: null,
      hasCarton: false, cartonQuantity: null, cartonPriceUsd: null, hasExpiry: false
    };
    this.barcodeError = false;
    this.productsState.clearError();
    this.activeModal = 'form';
  }

  openEditModal(p: ProductListItem) {
    this.productsState.clearError();
    this.api.getProduct(p.id).subscribe({
      next: (details) => {
        this.editId = p.id;
        this.formData = { ...details }; // populate
        this.activeModal = 'form';
      },
      error: () => {} // handled globally or toast
    });
  }

  isValidForm() {
    return this.formData.name && this.formData.barcode && this.formData.categoryId && 
           this.formData.baseUnit && this.formData.priceUsd !== null &&
           (!this.formData.hasCarton || (this.formData.cartonQuantity && this.formData.cartonPriceUsd));
  }

  saveProduct() {
    if (this.editId) {
      this.productsState.updateProduct(this.editId, this.formData).subscribe({
        next: () => this.closeModal(),
        error: () => {} // error handled in state
      });
    } else {
      this.productsState.createProduct(this.formData).subscribe({
        next: () => this.closeModal(),
        error: () => {} // error handled in state
      });
    }
  }

  // --- Details & Batches ---
  openDetailsModal(id: number) {
    this.detailsData = null;
    this.batchesData = [];
    this.api.getProduct(id).subscribe(d => {
      this.detailsData = d;
      this.api.getBatches(id).subscribe(b => {
        this.batchesData = b.items;
        this.activeModal = 'details';
      });
    });
  }

  isExpired(dateStr?: string | null): boolean {
    if (!dateStr) return false;
    return new Date(dateStr) < new Date();
  }

  // --- Add Batch ---
  openAddBatchModal() {
    this.batchForm = { quantityReceived: 1, entryInvoiceNumber: null, expiryDate: null };
    this.batchExpiryError = false;
    this.activeModal = 'addBatch';
  }

  isValidBatchForm() {
    if (!this.batchForm.quantityReceived || this.batchForm.quantityReceived <= 0) return false;
    if (this.detailsData?.hasExpiry && !this.batchForm.expiryDate) return false;
    return true;
  }

  saveBatch() {
    if (!this.detailsData) return;
    
    // Ensure we don't send expiryDate if hasExpiry is false
    const req: any = { 
      quantityReceived: this.batchForm.quantityReceived,
      quantityAvailable: this.batchForm.quantityReceived,
      entryDate: new Date().toISOString(),
      entryInvoiceNumber: this.batchForm.entryInvoiceNumber || null
    };
    
    if (this.detailsData.hasExpiry) {
      req.expiryDate = this.batchForm.expiryDate;
    }

    this.api.createBatch(this.detailsData.id, req).subscribe({
      next: () => {
        // Reload batches for current product
        this.api.getBatches(this.detailsData!.id).subscribe(b => {
          this.batchesData = b.items;
          this.activeModal = 'details'; // return to details
        });
        
        // Reload products state so that UI (like stock) is updated
        this.productsState.loadInitialData();
      },
      error: () => {}
    });
  }

  // --- Delete ---
  openDeleteModal(p: ProductListItem) {
    this.deleteCandidate = p;
    this.activeModal = 'delete';
  }

  confirmDelete() {
    if (!this.deleteCandidate) return;
    this.productsState.deleteProduct(this.deleteCandidate.id).subscribe({
      next: () => this.closeModal(),
      error: () => this.closeModal()
    });
  }

  // --- Export ---
  exportProducts() {
    const request = {
      format: 'excel',
      search: this.searchTerm || undefined,
      categoryId: this.filterCategory || undefined,
      isActive: this.filterStatus ? (this.filterStatus === 'active') : undefined
    };

    this.api.exportProducts(request).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;

        let filename = 'products_export.xlsx';
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
        this.productsState.setError('فشل تصدير البيانات');
      }
    });
  }

  closeModal() {
    this.activeModal = null;
    this.productsState.clearError();
  }
}
