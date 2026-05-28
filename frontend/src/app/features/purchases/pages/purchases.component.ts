import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { SupplierListItem } from '../../suppliers/models/supplier.model';
import { SuppliersApiService } from '../../suppliers/services/suppliers-api.service';
import {
  CreatePurchaseInvoiceLineRequest,
  PurchaseInvoiceDetailResponse,
  PurchaseInvoiceLineDto,
  PurchaseInvoiceListItem,
  PurchaseProductLookupItem
} from '../models/purchase-invoice.model';
import { PurchaseInvoicesApiService } from '../services/purchase-invoices-api.service';
import { BlackBoxRecorderService } from '../../../core/services/black-box-recorder.service';

type PurchaseModal = 'form' | 'details' | 'delete' | 'line' | 'complete' | null;

interface PurchaseInvoiceForm {
  supplierId: number | null;
  externalInvoiceNumber: string | null;
  notes: string | null;
}

interface PurchaseLineForm {
  productId: number | null;
  quantity: number | null;
  unitCostUsd: number | null;
  expiryDate: string | null;
  notes: string | null;
}

@Component({
  selector: 'app-purchases',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      <div class="flex justify-between items-center mb-6 shrink-0">
        <div>
          <h1 class="text-2xl font-bold text-slate-900">المشتريات</h1>
          <p class="text-sm text-slate-500 mt-1">إدارة فواتير الشراء والمسودات</p>
        </div>
        <button class="btn-primary" (click)="openCreateModal()" [disabled]="isLoading">
          <svg class="w-4 h-4 ml-2 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
          </svg>
          إنشاء فاتورة شراء
        </button>
      </div>

      <div class="bg-white p-4 rounded-lg shadow-sm border border-slate-200 mb-4 shrink-0 grid grid-cols-1 md:grid-cols-5 gap-4 items-center">
        <input
          type="text"
          [(ngModel)]="searchTerm"
          (ngModelChange)="applyFilters()"
          placeholder="بحث برقم الفاتورة أو المورد..."
          class="input-field md:col-span-2">
        <select [(ngModel)]="filterSupplierId" (ngModelChange)="applyFilters()" class="input-field">
          <option [ngValue]="null">كل الموردين</option>
          <option *ngFor="let supplier of suppliers" [ngValue]="supplier.id">{{ supplier.name }}</option>
        </select>
        <select [(ngModel)]="filterStatus" (ngModelChange)="applyFilters()" class="input-field">
          <option [ngValue]="null">كل الحالات</option>
          <option value="Draft">Draft</option>
          <option value="Completed">Completed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        <div class="grid grid-cols-2 gap-2">
          <input type="date" [(ngModel)]="dateFrom" (ngModelChange)="applyFilters()" class="input-field">
          <input type="date" [(ngModel)]="dateTo" (ngModelChange)="applyFilters()" class="input-field">
        </div>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-slate-200 flex-1 overflow-hidden flex flex-col">
        <div class="overflow-x-auto flex-1">
          <table class="w-full text-right text-sm">
            <thead class="bg-slate-50 text-slate-500 font-medium border-b border-slate-200 sticky top-0">
              <tr>
                <th class="py-3 px-4">رقم فاتورة الشراء</th>
                <th class="py-3 px-4">المورد</th>
                <th class="py-3 px-4 text-center">الحالة</th>
                <th class="py-3 px-4">تاريخ الفاتورة</th>
                <th class="py-3 px-4">رقم فاتورة المورد</th>
                <th class="py-3 px-4 text-left">الإجمالي USD</th>
                <th class="py-3 px-4 text-center w-36">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="isLoading && !invoices.length">
                <td colspan="7" class="py-8 text-center text-slate-400">جاري التحميل...</td>
              </tr>
              <tr *ngIf="!isLoading && !invoices.length">
                <td colspan="7" class="py-8 text-center text-slate-400">لا توجد فواتير شراء</td>
              </tr>
              <tr *ngIf="!isLoading && invoices.length && !filteredInvoices.length">
                <td colspan="7" class="py-8 text-center text-slate-400">لا توجد نتائج مطابقة</td>
              </tr>
              <tr *ngFor="let invoice of filteredInvoices" class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono text-slate-700">{{ invoice.invoiceNumber }}</td>
                <td class="py-3 px-4 font-medium">{{ invoice.supplierName }}</td>
                <td class="py-3 px-4 text-center">
                  <span class="px-2 py-1 rounded-full text-xs font-medium"
                        [class.bg-amber-100]="isDraft(invoice.status)"
                        [class.text-amber-800]="isDraft(invoice.status)"
                        [class.bg-green-100]="invoice.status === 'Completed'"
                        [class.text-green-800]="invoice.status === 'Completed'"
                        [class.bg-slate-100]="invoice.status === 'Cancelled'"
                        [class.text-slate-600]="invoice.status === 'Cancelled'">
                    {{ invoice.status }}
                  </span>
                </td>
                <td class="py-3 px-4 text-slate-500">{{ invoice.createdAt | date:'yyyy-MM-dd HH:mm' }}</td>
                <td class="py-3 px-4 text-slate-500">{{ invoice.externalInvoiceNumber || '—' }}</td>
                <td class="py-3 px-4 text-left font-mono">{{ invoice.totalUsd | number:'1.2-2' }}</td>
                <td class="py-3 px-4 text-center">
                  <div class="flex items-center justify-center gap-2">
                    <button class="text-slate-500 hover:text-primary transition-colors" title="فتح/تفاصيل" (click)="openDetails(invoice)">
                      فتح
                    </button>
                    <button *ngIf="isDraft(invoice.status)" class="text-slate-500 hover:text-primary transition-colors" title="تعديل" (click)="openEditModal(invoice)">
                      تعديل
                    </button>
                    <button *ngIf="isDraft(invoice.status)" class="text-slate-500 hover:text-red-600 transition-colors" title="حذف" (click)="openDeleteModal(invoice)">
                      حذف
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="bg-slate-50 p-3 border-t border-slate-200 text-sm text-slate-500 flex justify-between items-center">
          <span>إجمالي الفواتير: {{ filteredInvoices.length }}</span>
        </div>
      </div>

      <div *ngIf="toast" class="fixed bottom-4 left-4 bg-slate-900 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span class="text-sm">{{ toast }}</span>
        <button (click)="toast = ''" class="text-white/80 hover:text-white">x</button>
      </div>

      <div *ngIf="error && !activeModal" class="fixed bottom-4 left-4 bg-red-600 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span class="text-sm">{{ error }}</span>
        <button (click)="error = ''" class="text-white/80 hover:text-white">x</button>
      </div>

      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-40 flex items-center justify-center p-4">
        <div *ngIf="activeModal === 'form'" class="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0">
            {{ editId ? 'تعديل فاتورة شراء' : 'إنشاء فاتورة شراء' }}
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            <div *ngIf="formErr" class="bg-red-50 border border-red-200 text-red-700 rounded p-3">{{ formErr }}</div>
            <div>
              <label class="block text-slate-600 mb-1">المورد <span class="text-red-500">*</span></label>
              <select [(ngModel)]="formData.supplierId" class="input-field">
                <option [ngValue]="null">اختر المورد</option>
                <option *ngFor="let supplier of activeSuppliers" [ngValue]="supplier.id">{{ supplier.name }}</option>
              </select>
            </div>
            <div>
              <label class="block text-slate-600 mb-1">رقم فاتورة المورد</label>
              <input type="text" [(ngModel)]="formData.externalInvoiceNumber" class="input-field">
            </div>
            <div>
              <label class="block text-slate-600 mb-1">الملاحظات</label>
              <textarea [(ngModel)]="formData.notes" class="input-field min-h-[90px] resize-y"></textarea>
            </div>
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end shrink-0 border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveInvoice()" [disabled]="actionLoading || !formData.supplierId">حفظ</button>
          </div>
        </div>

        <div *ngIf="activeModal === 'details' && selectedInvoice" class="bg-white rounded-lg shadow-xl w-full max-w-6xl overflow-hidden flex flex-col max-h-[92vh]">
          <div class="p-4 border-b border-slate-100 flex justify-between items-start gap-4 shrink-0">
            <div>
              <h2 class="font-bold text-slate-900">فاتورة شراء {{ selectedInvoice.invoiceNumber }}</h2>
              <p class="text-xs text-slate-500 mt-1">
                {{ selectedInvoice.supplierName }} · {{ selectedInvoice.createdAt | date:'yyyy-MM-dd HH:mm' }}
              </p>
            </div>
            <div class="flex items-center gap-2">
              <span class="px-2 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-700">{{ selectedInvoice.status }}</span>
              <button
                *ngIf="isDraft(selectedInvoice.status)"
                class="btn-primary text-sm"
                (click)="openCompleteModal()"
                [disabled]="actionLoading || !selectedInvoice.lines.length"
                [title]="!selectedInvoice.lines.length ? 'لا يمكن إتمام فاتورة بلا خطوط' : 'إتمام فاتورة الشراء'">
                إتمام الفاتورة
              </button>
              <button class="btn-secondary text-sm" (click)="closeModal()">إغلاق</button>
            </div>
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-5 text-sm">
            <div *ngIf="detailLoading" class="py-8 text-center text-slate-400">جاري تحميل التفاصيل...</div>
            <ng-container *ngIf="!detailLoading">
              <div *ngIf="!isDraft(selectedInvoice.status)" class="bg-slate-50 border border-slate-200 text-slate-600 rounded p-3">
                هذه الفاتورة غير قابلة للتعديل
              </div>
              <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
                <div class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">رقم فاتورة المورد</div>
                  <div class="font-medium mt-1">{{ selectedInvoice.externalInvoiceNumber || '—' }}</div>
                </div>
                <div class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">أنشأها</div>
                  <div class="font-medium mt-1">{{ selectedInvoice.createdByEmployeeName }}</div>
                </div>
                <div *ngIf="selectedInvoice.completedAt" class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">تم الإتمام</div>
                  <div class="font-medium mt-1">{{ selectedInvoice.completedAt | date:'yyyy-MM-dd HH:mm' }}</div>
                </div>
                <div *ngIf="selectedInvoice.completedByEmployeeName" class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">أتمها</div>
                  <div class="font-medium mt-1">{{ selectedInvoice.completedByEmployeeName }}</div>
                </div>
                <div class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">Subtotal USD</div>
                  <div class="font-mono font-bold mt-1">{{ selectedInvoice.subtotalUsd | number:'1.2-2' }}</div>
                </div>
                <div class="bg-slate-50 rounded p-3">
                  <div class="text-slate-500 text-xs">Total USD</div>
                  <div class="font-mono font-bold mt-1">{{ selectedInvoice.totalUsd | number:'1.2-2' }}</div>
                </div>
              </div>
              <div *ngIf="selectedInvoice.notes" class="bg-slate-50 rounded p-3 text-slate-600">{{ selectedInvoice.notes }}</div>

              <div class="flex justify-between items-center">
                <h3 class="font-bold text-slate-800">الخطوط</h3>
                <button *ngIf="isDraft(selectedInvoice.status)" class="btn-primary text-sm" (click)="openAddLineModal()">إضافة خط</button>
              </div>
              <div class="border border-slate-200 rounded-lg overflow-hidden">
                <table class="w-full text-right text-sm">
                  <thead class="bg-slate-50 text-slate-500">
                    <tr>
                      <th class="py-3 px-4">المنتج</th>
                      <th class="py-3 px-4">الباركود</th>
                      <th class="py-3 px-4 text-left">الكمية</th>
                      <th class="py-3 px-4 text-left">تكلفة الوحدة</th>
                      <th class="py-3 px-4">الصلاحية</th>
                      <th class="py-3 px-4 text-left">إجمالي السطر</th>
                      <th *ngIf="isDraft(selectedInvoice.status)" class="py-3 px-4 text-center">إجراءات</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    <tr *ngIf="!selectedInvoice.lines.length">
                      <td [attr.colspan]="isDraft(selectedInvoice.status) ? 7 : 6" class="py-8 text-center text-slate-400">لا توجد خطوط في هذه الفاتورة</td>
                    </tr>
                    <tr *ngFor="let line of selectedInvoice.lines">
                      <td class="py-3 px-4 font-medium">{{ line.productName }}</td>
                      <td class="py-3 px-4 text-slate-500 font-mono">{{ line.barcode }}</td>
                      <td class="py-3 px-4 text-left font-mono">{{ line.quantity | number:'1.0-4' }}</td>
                      <td class="py-3 px-4 text-left font-mono">{{ line.unitCostUsd | number:'1.2-4' }}</td>
                      <td class="py-3 px-4 text-slate-500">{{ line.expiryDate ? (line.expiryDate | date:'yyyy-MM-dd') : '—' }}</td>
                      <td class="py-3 px-4 text-left font-mono">{{ line.lineTotalUsd | number:'1.2-2' }}</td>
                      <td *ngIf="isDraft(selectedInvoice.status)" class="py-3 px-4 text-center">
                        <button class="text-slate-500 hover:text-primary ml-3" (click)="openEditLineModal(line)">تعديل</button>
                        <button class="text-slate-500 hover:text-red-600" (click)="deleteLine(line)">حذف</button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </ng-container>
          </div>
        </div>

        <div *ngIf="activeModal === 'line'" class="bg-white rounded-lg shadow-xl w-full max-w-3xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0">
            {{ lineEditId ? 'تعديل خط شراء' : 'إضافة خط شراء' }}
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            <div *ngIf="lineErr" class="bg-red-50 border border-red-200 text-red-700 rounded p-3">{{ lineErr }}</div>
            <div *ngIf="!lineEditId" class="relative">
              <label class="block text-slate-600 mb-1">بحث المنتج بالاسم أو الباركود</label>
              <input type="text" [(ngModel)]="productSearch" (ngModelChange)="onProductSearchChange($event)" class="input-field" placeholder="اكتب للبحث...">
              <div *ngIf="lookupItems.length || lookupLoading || productSearch" class="absolute z-50 mt-1 bg-white border border-slate-200 rounded shadow-lg w-full max-h-56 overflow-y-auto">
                <div *ngIf="lookupLoading" class="p-3 text-slate-400">جاري البحث...</div>
                <button *ngFor="let product of lookupItems" type="button" class="block w-full text-right p-3 hover:bg-slate-50 border-b border-slate-100" (click)="selectProduct(product)">
                  <span class="block font-medium">{{ product.name }}</span>
                  <span class="block text-xs text-slate-500">{{ product.barcode }} · {{ product.baseUnit }} · {{ product.priceUsd | number:'1.2-2' }} USD</span>
                </button>
                <div *ngIf="!lookupLoading && productSearch && !lookupItems.length" class="p-3 text-slate-400">لا توجد منتجات مطابقة</div>
              </div>
            </div>
            <div *ngIf="selectedProduct || lineEditId" class="bg-slate-50 rounded p-3">
              <div class="text-slate-500 text-xs">المنتج المحدد</div>
              <div class="font-medium mt-1">{{ selectedProduct?.name || selectedLineProductName }}</div>
              <div class="text-xs text-slate-500 mt-1">{{ selectedProduct?.barcode || selectedLineBarcode }}</div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-slate-600 mb-1">الكمية <span class="text-red-500">*</span></label>
                <input type="number" [(ngModel)]="lineForm.quantity" min="0" step="0.01" class="input-field">
              </div>
              <div>
                <label class="block text-slate-600 mb-1">تكلفة الوحدة USD <span class="text-red-500">*</span></label>
                <input type="number" [(ngModel)]="lineForm.unitCostUsd" min="0" step="0.01" class="input-field">
              </div>
            </div>
            <div *ngIf="selectedProduct?.hasExpiry || lineProductHasExpiry">
              <label class="block text-slate-600 mb-1">تاريخ الصلاحية <span class="text-red-500">*</span></label>
              <input type="date" [(ngModel)]="lineForm.expiryDate" class="input-field">
            </div>
            <div>
              <label class="block text-slate-600 mb-1">ملاحظات السطر</label>
              <textarea [(ngModel)]="lineForm.notes" class="input-field min-h-[80px] resize-y"></textarea>
            </div>
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end shrink-0 border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="returnToDetails()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveLine()" [disabled]="actionLoading">حفظ</button>
          </div>
        </div>

        <div *ngIf="activeModal === 'delete'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden">
          <div class="p-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد حذف المسودة</div>
          <div class="p-4 text-slate-700 text-sm">
            هل أنت متأكد من حذف فاتورة الشراء "{{ deleteCandidate?.invoiceNumber }}"؟
          </div>
          <div *ngIf="formErr" class="mx-4 mb-4 bg-red-50 border border-red-200 text-red-700 rounded p-3 text-sm">{{ formErr }}</div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-danger text-sm" (click)="confirmDelete()" [disabled]="actionLoading">تأكيد</button>
          </div>
        </div>

        <div *ngIf="activeModal === 'complete'" class="bg-white rounded-lg shadow-xl w-full max-w-md overflow-hidden">
          <div class="p-4 border-b border-amber-100 bg-amber-50 font-bold text-amber-800">تأكيد إتمام فاتورة الشراء</div>
          <div class="p-4 text-slate-700 text-sm space-y-3">
            <div>
              هل تريد إتمام فاتورة الشراء "{{ completeCandidate?.invoiceNumber }}" للمورد "{{ completeCandidate?.supplierName }}"؟
            </div>
            <div class="bg-slate-50 border border-slate-200 rounded p-3 text-slate-600">
              سيتم إنشاء دفعات منتجات وزيادة المخزون. لا يمكن تعديل الفاتورة بعد الإتمام.
            </div>
          </div>
          <div *ngIf="formErr" class="mx-4 mb-4 bg-red-50 border border-red-200 text-red-700 rounded p-3 text-sm">{{ formErr }}</div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="returnToDetails()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-primary text-sm" (click)="confirmComplete()" [disabled]="actionLoading">تأكيد الإتمام</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PurchasesComponent implements OnInit, OnDestroy {
  invoices: PurchaseInvoiceListItem[] = [];
  filteredInvoices: PurchaseInvoiceListItem[] = [];
  suppliers: SupplierListItem[] = [];
  activeSuppliers: SupplierListItem[] = [];

  isLoading = false;
  detailLoading = false;
  actionLoading = false;
  lookupLoading = false;
  error = '';
  formErr = '';
  lineErr = '';
  toast = '';

  searchTerm = '';
  filterSupplierId: number | null = null;
  filterStatus: string | null = null;
  dateFrom = '';
  dateTo = '';

  activeModal: PurchaseModal = null;
  editId: number | null = null;
  formData: PurchaseInvoiceForm = this.emptyInvoiceForm();
  deleteCandidate: PurchaseInvoiceListItem | null = null;
  selectedInvoice: PurchaseInvoiceDetailResponse | null = null;
  completeCandidate: PurchaseInvoiceDetailResponse | null = null;

  productSearch = '';
  lookupItems: PurchaseProductLookupItem[] = [];
  selectedProduct: PurchaseProductLookupItem | null = null;
  selectedLineProductName = '';
  selectedLineBarcode = '';
  lineEditId: number | null = null;
  lineProductHasExpiry = false;
  lineForm: PurchaseLineForm = this.emptyLineForm();

  private productSearch$ = new Subject<string>();
  private productSearchSub?: Subscription;

  constructor(
    private api: PurchaseInvoicesApiService,
    private suppliersApi: SuppliersApiService,
    private blackBox: BlackBoxRecorderService
  ) {}

  ngOnInit(): void {
    this.loadInvoices();
    this.loadSuppliers();
    this.productSearchSub = this.productSearch$.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(search => {
        this.lookupLoading = true;
        return this.api.productsLookup(search).pipe(
          catchError(error => {
            this.lineErr = this.mapError(error);
            return of({ items: [] });
          })
        );
      })
    ).subscribe(response => {
      this.lookupItems = response.items || [];
      this.lookupLoading = false;
    });
  }

  ngOnDestroy(): void {
    this.productSearchSub?.unsubscribe();
  }

  loadInvoices(): void {
    this.isLoading = true;
    this.error = '';
    this.api.getAll().subscribe({
      next: response => {
        this.invoices = response.items || [];
        this.applyFilters();
        this.isLoading = false;
      },
      error: error => {
        this.error = this.mapError(error);
        this.isLoading = false;
      }
    });
  }

  loadSuppliers(): void {
    this.suppliersApi.getSuppliers().subscribe({
      next: response => {
        this.suppliers = response.items || [];
        this.activeSuppliers = this.suppliers.filter(s => s.isActive);
      },
      error: error => {
        this.error = this.mapError(error);
      }
    });
  }

  applyFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();
    let result = this.invoices;

    if (term) {
      result = result.filter(invoice =>
        invoice.invoiceNumber.toLowerCase().includes(term) ||
        (invoice.externalInvoiceNumber || '').toLowerCase().includes(term) ||
        invoice.supplierName.toLowerCase().includes(term)
      );
    }

    if (this.filterSupplierId !== null) {
      result = result.filter(invoice => invoice.supplierId === this.filterSupplierId);
    }

    if (this.filterStatus) {
      result = result.filter(invoice => invoice.status === this.filterStatus);
    }

    if (this.dateFrom) {
      result = result.filter(invoice => invoice.createdAt.slice(0, 10) >= this.dateFrom);
    }

    if (this.dateTo) {
      result = result.filter(invoice => invoice.createdAt.slice(0, 10) <= this.dateTo);
    }

    this.filteredInvoices = result;
  }

  isDraft(status: string): boolean {
    return status === 'Draft';
  }

  openCreateModal(): void {
    this.editId = null;
    this.formData = this.emptyInvoiceForm();
    this.formErr = '';
    this.activeModal = 'form';
  }

  openEditModal(invoice: PurchaseInvoiceListItem): void {
    this.formErr = '';
    this.actionLoading = true;
    this.api.getById(invoice.id).subscribe({
      next: detail => {
        this.editId = detail.id;
        this.formData = {
          supplierId: detail.supplierId,
          externalInvoiceNumber: detail.externalInvoiceNumber || null,
          notes: detail.notes || null
        };
        this.activeModal = 'form';
        this.actionLoading = false;
      },
      error: error => {
        this.error = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  saveInvoice(): void {
    if (!this.formData.supplierId) {
      this.formErr = 'المورد مطلوب';
      return;
    }

    this.actionLoading = true;
    this.formErr = '';
    const request = {
      supplierId: this.formData.supplierId,
      externalInvoiceNumber: this.clean(this.formData.externalInvoiceNumber),
      notes: this.clean(this.formData.notes)
    };
    const save$ = this.editId
      ? this.api.update(this.editId, request)
      : this.api.create(request);

    save$.subscribe({
      next: detail => {
        this.selectedInvoice = detail;
        this.activeModal = 'details';
        this.toast = this.editId ? 'تم تحديث فاتورة الشراء' : 'تم إنشاء فاتورة الشراء';
        this.editId = null;
        this.actionLoading = false;
        this.loadInvoices();
      },
      error: error => {
        this.formErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  openDetails(invoice: PurchaseInvoiceListItem): void {
    this.detailLoading = true;
    this.activeModal = 'details';
    this.api.getById(invoice.id).subscribe({
      next: detail => {
        this.selectedInvoice = detail;
        this.detailLoading = false;
      },
      error: error => {
        this.error = this.mapError(error);
        this.detailLoading = false;
        this.activeModal = null;
      }
    });
  }

  openDeleteModal(invoice: PurchaseInvoiceListItem): void {
    this.deleteCandidate = invoice;
    this.formErr = '';
    this.activeModal = 'delete';
  }

  confirmDelete(): void {
    if (!this.deleteCandidate) {
      return;
    }

    this.actionLoading = true;
    this.api.delete(this.deleteCandidate.id).subscribe({
      next: () => {
        this.toast = 'تم حذف فاتورة الشراء';
        this.closeModal();
        this.actionLoading = false;
        this.loadInvoices();
      },
      error: error => {
        this.formErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  openCompleteModal(): void {
    if (!this.selectedInvoice || !this.isDraft(this.selectedInvoice.status) || !this.selectedInvoice.lines.length) {
      return;
    }

    this.completeCandidate = this.selectedInvoice;
    this.formErr = '';
    this.activeModal = 'complete';
  }

  confirmComplete(): void {
    if (!this.completeCandidate) {
      return;
    }

    this.actionLoading = true;
    this.formErr = '';
    this.api.complete(this.completeCandidate.id).subscribe({
      next: detail => {
        this.blackBox.recordSuccess('COMPLETE_PURCHASE', {
          pageName: 'Purchases',
          entityType: 'PurchaseInvoice',
          entityId: detail.id,
          metadata: {
            invoiceNumber: detail.invoiceNumber,
            supplierId: detail.supplierId,
            lineCount: detail.lines?.length || 0,
            totalUsd: detail.totalUsd
          }
        });
        this.selectedInvoice = detail;
        this.completeCandidate = null;
        this.activeModal = 'details';
        this.toast = 'تم إتمام فاتورة الشراء وتحديث المخزون';
        this.actionLoading = false;
        this.loadInvoices();
      },
      error: error => {
        this.blackBox.recordFailure('COMPLETE_PURCHASE', {
          pageName: 'Purchases',
          entityType: 'PurchaseInvoice',
          entityId: this.completeCandidate?.id,
          message: error instanceof HttpErrorResponse ? error.error?.error : 'COMPLETE_PURCHASE_FAILED'
        });
        this.formErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  openAddLineModal(): void {
    if (!this.selectedInvoice || !this.isDraft(this.selectedInvoice.status)) {
      return;
    }

    this.lineEditId = null;
    this.lineForm = this.emptyLineForm();
    this.selectedProduct = null;
    this.selectedLineProductName = '';
    this.selectedLineBarcode = '';
    this.lineProductHasExpiry = false;
    this.productSearch = '';
    this.lookupItems = [];
    this.lineErr = '';
    this.activeModal = 'line';
  }

  openEditLineModal(line: PurchaseInvoiceLineDto): void {
    if (!this.selectedInvoice || !this.isDraft(this.selectedInvoice.status)) {
      return;
    }

    this.lineEditId = line.id;
    this.selectedProduct = null;
    this.selectedLineProductName = line.productName;
    this.selectedLineBarcode = line.barcode;
    this.lineProductHasExpiry = !!line.expiryDate;
    this.lineForm = {
      productId: line.productId,
      quantity: line.quantity,
      unitCostUsd: line.unitCostUsd,
      expiryDate: line.expiryDate ? line.expiryDate.slice(0, 10) : null,
      notes: line.notes || null
    };
    this.lineErr = '';
    this.activeModal = 'line';
  }

  onProductSearchChange(search: string): void {
    this.productSearch$.next(search.trim());
  }

  selectProduct(product: PurchaseProductLookupItem): void {
    this.selectedProduct = product;
    this.lineProductHasExpiry = product.hasExpiry;
    this.lineForm.productId = product.productId;
    this.lineForm.expiryDate = product.hasExpiry ? this.lineForm.expiryDate : null;
    this.lookupItems = [];
    this.productSearch = product.name;
  }

  saveLine(): void {
    if (!this.selectedInvoice) {
      return;
    }

    const validationError = this.validateLineForm();
    if (validationError) {
      this.lineErr = validationError;
      return;
    }

    const request: CreatePurchaseInvoiceLineRequest = {
      productId: this.lineForm.productId as number,
      quantity: Number(this.lineForm.quantity),
      unitCostUsd: Number(this.lineForm.unitCostUsd),
      expiryDate: this.lineProductHasExpiry ? this.lineForm.expiryDate : null,
      notes: this.clean(this.lineForm.notes)
    };

    this.actionLoading = true;
    this.lineErr = '';
    const save$ = this.lineEditId
      ? this.api.updateLine(this.selectedInvoice.id, this.lineEditId, {
          quantity: request.quantity,
          unitCostUsd: request.unitCostUsd,
          expiryDate: request.expiryDate,
          notes: request.notes
        })
      : this.api.addLine(this.selectedInvoice.id, request);

    save$.subscribe({
      next: detail => {
        this.selectedInvoice = detail;
        this.activeModal = 'details';
        this.toast = this.lineEditId ? 'تم تحديث السطر' : 'تمت إضافة السطر';
        this.actionLoading = false;
        this.loadInvoices();
      },
      error: error => {
        this.lineErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  deleteLine(line: PurchaseInvoiceLineDto): void {
    if (!this.selectedInvoice || !this.isDraft(this.selectedInvoice.status)) {
      return;
    }

    this.actionLoading = true;
    const invoiceId = this.selectedInvoice.id;
    this.api.deleteLine(this.selectedInvoice.id, line.id).subscribe({
      next: () => {
        this.api.getById(invoiceId).subscribe({
          next: detail => {
            this.selectedInvoice = detail;
            this.toast = 'تم حذف السطر';
            this.actionLoading = false;
            this.loadInvoices();
          },
          error: error => {
            this.lineErr = this.mapError(error);
            this.actionLoading = false;
          }
        });
      },
      error: error => {
        this.lineErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  returnToDetails(): void {
    this.activeModal = 'details';
  }

  closeModal(): void {
    this.activeModal = null;
    this.editId = null;
    this.deleteCandidate = null;
    this.completeCandidate = null;
    this.formErr = '';
    this.lineErr = '';
  }

  mapError(error: unknown): string {
    const code = error instanceof HttpErrorResponse
      ? error.error?.error || error.error?.code || error.error?.message
      : null;

    switch (code) {
      case 'PURCHASE_INVOICE_NOT_FOUND':
        return 'فاتورة الشراء غير موجودة';
      case 'SUPPLIER_NOT_FOUND':
        return 'المورد غير موجود';
      case 'SUPPLIER_INACTIVE':
        return 'المورد غير نشط';
      case 'PURCHASE_INVOICE_NOT_DRAFT':
        return 'يمكن إتمام المسودات فقط';
      case 'PURCHASE_INVOICE_ALREADY_COMPLETED':
        return 'تم إتمام هذه الفاتورة مسبقًا';
      case 'PURCHASE_INVOICE_HAS_NO_LINES':
        return 'لا يمكن إتمام فاتورة بلا خطوط';
      case 'PURCHASE_INVOICE_LINE_NOT_FOUND':
        return 'سطر الفاتورة غير موجود';
      case 'PRODUCT_NOT_FOUND':
        return 'المنتج غير موجود';
      case 'PRODUCT_INACTIVE':
        return 'أحد المنتجات غير نشط';
      case 'INVALID_QUANTITY':
        return 'الكمية يجب أن تكون أكبر من صفر';
      case 'INVALID_UNIT_COST':
        return 'تكلفة الوحدة غير صالحة';
      case 'EXPIRY_DATE_REQUIRED':
        return 'تاريخ الصلاحية مطلوب لأحد المنتجات';
      case 'EXPIRY_DATE_NOT_ALLOWED':
        return 'أحد المنتجات لا يدعم تاريخ الصلاحية';
      case 'PURCHASE_INVOICE_NUMBER_ALREADY_EXISTS':
        return 'رقم فاتورة الشراء موجود مسبقًا';
      default:
        return 'حدث خطأ أثناء تنفيذ العملية';
    }
  }

  private validateLineForm(): string {
    if (!this.lineForm.productId) {
      return 'المنتج مطلوب';
    }

    if (this.lineForm.quantity === null || Number(this.lineForm.quantity) <= 0) {
      return 'الكمية يجب أن تكون أكبر من صفر';
    }

    if (this.lineForm.unitCostUsd === null || Number(this.lineForm.unitCostUsd) < 0) {
      return 'تكلفة الوحدة غير صالحة';
    }

    if (this.lineProductHasExpiry && !this.lineForm.expiryDate) {
      return 'تاريخ الصلاحية مطلوب لهذا المنتج';
    }

    return '';
  }

  private emptyInvoiceForm(): PurchaseInvoiceForm {
    return {
      supplierId: null,
      externalInvoiceNumber: null,
      notes: null
    };
  }

  private emptyLineForm(): PurchaseLineForm {
    return {
      productId: null,
      quantity: 1,
      unitCostUsd: null,
      expiryDate: null,
      notes: null
    };
  }

  private clean(value: string | null): string | null {
    const cleaned = (value || '').trim();
    return cleaned || null;
  }
}
