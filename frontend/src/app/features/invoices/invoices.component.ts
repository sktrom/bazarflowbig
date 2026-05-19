import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoicesStateService } from './services/invoices-state.service';
import { InvoicesApiService, InvoiceListItem, InvoiceDetailsResponse, CreateAdjustmentRequest } from './services/invoices-api.service';
import { Router } from '@angular/router';
import { ReceiptPrintComponent } from '../../shared/components/receipt-print/receipt-print.component';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [CommonModule, FormsModule, ReceiptPrintComponent],
  template: `
    <div class="p-6 h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      
      <!-- Header -->
      <div class="flex justify-between items-center mb-6 shrink-0">
        <div>
          <h1 class="text-2xl font-bold text-slate-900">سجل الفواتير</h1>
          <p class="text-sm text-slate-500 mt-1">عرض وتدقيق فواتير المبيعات وطلبات التعديل</p>
        </div>
        <div class="flex gap-3">
          <div class="relative group">
            <button class="btn-secondary">
              الإجراءات
              <svg class="w-4 h-4 mr-2 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path></svg>
            </button>
            <div class="absolute left-0 mt-2 w-48 bg-white border border-slate-200 rounded-md shadow-lg hidden group-hover:block z-50">
              <button (click)="exportInvoices()" class="block w-full text-right px-4 py-2 text-sm text-slate-700 hover:bg-slate-100">تصدير الفواتير (Export)</button>
            </div>
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-white p-4 rounded-lg shadow-sm border border-slate-200 mb-4 shrink-0 flex flex-wrap gap-4 items-end">
        <div class="w-48">
          <label class="block text-xs text-slate-500 mb-1">اسم الزبون</label>
          <input type="text" [(ngModel)]="filters.customerName" placeholder="بحث..." class="input-field">
        </div>
        <div class="w-32">
          <label class="block text-xs text-slate-500 mb-1">حالة الفاتورة</label>
          <select [(ngModel)]="filters.status" class="input-field">
            <option value="">الكل</option>
            <option value="Completed">مكتملة</option>
            <option value="Suspended">معلقة</option>
            <option value="Modified">معدلة</option>
            <option value="Cancelled">ملغاة</option>
          </select>
        </div>
        <div class="w-32">
          <label class="block text-xs text-slate-500 mb-1">حالة طلب التعديل</label>
          <select [(ngModel)]="filters.adjustmentRequestStatus" class="input-field">
            <option value="">الكل</option>
            <option value="Pending">قيد الانتظار</option>
            <option value="Approved">مقبول</option>
            <option value="Rejected">مرفوض</option>
          </select>
        </div>
        <div class="w-32">
          <label class="block text-xs text-slate-500 mb-1">الموظف (ID)</label>
          <input type="number" [(ngModel)]="filters.employeeId" class="input-field">
        </div>
        <div class="w-32">
          <label class="block text-xs text-slate-500 mb-1">من تاريخ</label>
          <input type="date" [(ngModel)]="filters.dateFrom" class="input-field">
        </div>
        <div class="w-32">
          <label class="block text-xs text-slate-500 mb-1">إلى تاريخ</label>
          <input type="date" [(ngModel)]="filters.dateTo" class="input-field">
        </div>
        <div class="flex items-center gap-2 mb-2">
          <input type="checkbox" [(ngModel)]="filters.manualPriceEdited" id="manualPrice">
          <label for="manualPrice" class="text-xs text-slate-600">تعديل يدوي</label>
        </div>
        <button class="btn-primary py-2 px-6" (click)="search()">تحديث</button>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-lg shadow-sm border border-slate-200 flex-1 overflow-hidden flex flex-col">
        <div class="overflow-x-auto flex-1">
          <table class="w-full text-right text-sm">
            <thead class="bg-slate-50 text-slate-500 font-medium border-b border-slate-200 sticky top-0">
              <tr>
                <th class="py-3 px-4">رقم الفاتورة</th>
                <th class="py-3 px-4 text-center">الحالة</th>
                <th class="py-3 px-4">اسم الزبون</th>
                <th class="py-3 px-4">الموظف (ID)</th>
                <th class="py-3 px-4">الإجمالي</th>
                <th class="py-3 px-4 text-center">التاريخ</th>
                <th class="py-3 px-4 text-center">الوقت</th>
                <th class="py-3 px-4 text-center w-32">إجراءات السطر</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="(state$ | async)?.isLoading && !(state$ | async)?.items?.length">
                <td colspan="8" class="py-8 text-center text-slate-400">جاري التحميل...</td>
              </tr>
              <tr *ngFor="let inv of (state$ | async)?.items" class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold">{{ inv.invoiceNumber }}</td>
                <td class="py-3 px-4 text-center">
                  <span [class]="getStatusClass(inv.status)" class="px-2 py-1 rounded text-[10px] font-medium block mb-1">
                    {{ getStatusLabel(inv.status) }}
                  </span>
                  <span *ngIf="inv.hasAdjustmentRequest" [class]="getAdjStatusClass(inv.adjustmentRequestStatus)" class="px-2 py-1 rounded-full text-[9px] font-bold">
                    {{ getAdjustmentLabel(inv.adjustmentRequestStatus) }}
                  </span>
                </td>
                <td class="py-3 px-4">{{ inv.customerName || '—' }}</td>
                <td class="py-3 px-4 text-slate-500 font-mono text-xs">{{ inv.originalEmployeeId }}</td>
                <td class="py-3 px-4 font-bold">{{ inv.totalUsd | currency:'USD' }}</td>
                <td class="py-3 px-4 text-center text-slate-500">{{ inv.createdAt | date:'yyyy-MM-dd' }}</td>
                <td class="py-3 px-4 text-center text-slate-400 font-mono text-xs">{{ inv.createdAt | date:'HH:mm:ss' }}</td>
                <td class="py-3 px-4">
                  <div class="flex items-center justify-center gap-2">
                    <button class="text-slate-400 hover:text-primary" title="التفاصيل" (click)="viewDetails(inv.invoiceId)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path></svg>
                    </button>
                    <button *ngIf="inv.status === 'Suspended'" class="text-green-500 hover:text-green-700" title="تحميل للكاشير" (click)="loadToCashier(inv.invoiceId)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path></svg>
                    </button>
                    <button *ngIf="inv.status === 'Completed' && !inv.hasAdjustmentRequest" class="text-amber-500 hover:text-amber-700" title="طلب تعديل" (click)="openAdjustment(inv.invoiceId)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                    </button>
                    <button *ngIf="inv.adjustmentRequestStatus === 'Pending'" class="text-blue-500 hover:text-blue-700 font-bold" title="مراجعة الطلب" (click)="reviewAdjustment(inv.invoiceId, inv.adjustmentRequestId!)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        
        <!-- Pagination -->
        <div class="bg-slate-50 p-3 border-t border-slate-200 text-sm text-slate-500 flex justify-between items-center shrink-0">
          <span>إجمالي الفواتير: {{ (state$ | async)?.totalCount }}</span>
          <div class="flex gap-2">
            <button class="px-3 py-1 bg-white border border-slate-200 rounded disabled:opacity-50" [disabled]="(state$ | async)?.page === 1" (click)="changePage(-1)">السابق</button>
            <span class="px-3 py-1">صفحة {{ (state$ | async)?.page }}</span>
            <button class="px-3 py-1 bg-white border border-slate-200 rounded disabled:opacity-50" [disabled]="isLastPage()" (click)="changePage(1)">التالي</button>
          </div>
        </div>
      </div>

      <!-- Modals Overlay -->
      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-40 flex items-center justify-center p-4">
        
        <!-- Details Modal -->
        <div *ngIf="activeModal === 'details'" class="bg-white rounded-lg shadow-xl w-full max-w-4xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 flex justify-between items-center shrink-0">
            <span>تفاصيل الفاتورة: {{ selectedInvoice?.invoiceNumber }}</span>
            <div class="flex items-center gap-2 no-print">
              <button
                class="btn-secondary text-xs py-1.5 px-3"
                data-testid="invoice-print-button"
                [disabled]="!selectedInvoice || isPrinting"
                title="طباعة الإيصال"
                (click)="printReceipt()"
              >
                <svg class="w-4 h-4 ml-1 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 9V2h12v7M6 18H4a2 2 0 01-2-2v-5a2 2 0 012-2h16a2 2 0 012 2v5a2 2 0 01-2 2h-2m-12 0h12v4H6v-4z"></path></svg>
                طباعة
              </button>
              <button (click)="closeModal()" class="text-slate-400 hover:text-slate-600">x</button>
            </div>
          </div>
          <app-receipt-print class="print-only" [invoice]="selectedInvoice" [reprint]="true"></app-receipt-print>
          <div class="p-6 overflow-y-auto flex-1 space-y-6 text-sm">
            
            <div class="grid grid-cols-4 gap-4 bg-slate-50 p-4 rounded border border-slate-100">
              <div><span class="text-slate-500 block text-[10px]">الزبون</span><span class="font-bold">{{ selectedInvoice?.customerName || 'زبون نقدي' }}</span></div>
              <div><span class="text-slate-500 block text-[10px]">التاريخ</span><span class="font-bold">{{ selectedInvoice?.createdAt | date:'yyyy-MM-dd HH:mm' }}</span></div>
              <div><span class="text-slate-500 block text-[10px]">الحالة</span><span [class]="getStatusClass(selectedInvoice?.status || '')" class="font-bold">{{ getStatusLabel(selectedInvoice?.status || '') }}</span></div>
              <div><span class="text-slate-500 block text-[10px]">الإجمالي</span><span class="font-bold text-lg text-primary">{{ selectedInvoice?.totalUsd | currency:'USD' }}</span></div>
            </div>

            <div class="space-y-2">
              <h3 class="font-bold text-slate-700">المواد المباعة</h3>
              <div class="border border-slate-200 rounded overflow-hidden">
                <table class="w-full text-right">
                  <thead class="bg-slate-50 text-slate-500 text-xs border-b border-slate-200">
                    <tr>
                      <th class="py-2 px-3">المنتج</th>
                      <th class="py-2 px-3">الكمية</th>
                      <th class="py-2 px-3">السعر الإفرادي</th>
                      <th class="py-2 px-3">الإجمالي</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    <tr *ngFor="let line of selectedInvoice?.lines">
                      <td class="py-2 px-3">{{ line.productName }}</td>
                      <td class="py-2 px-3">{{ line.quantity }}</td>
                      <td class="py-2 px-3">{{ line.unitPriceUsdOriginal | currency:'USD' }}</td>
                      <td class="py-2 px-3 font-bold">{{ line.lineTotalUsdEffective | currency:'USD' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div *ngIf="selectedInvoice?.hasAdjustmentRequest" class="mt-6 border-t border-slate-200 pt-4">
              <h3 class="font-bold text-slate-700 mb-3">حالة طلب التعديل المرتبط</h3>
              <div class="bg-slate-50 border border-slate-200 rounded-lg p-4 flex justify-between items-center">
                <div class="flex gap-6">
                  <div>
                    <span class="text-slate-500 block text-[10px]">نوع الطلب</span>
                    <span class="font-bold">{{ selectedInvoice?.adjustmentRequestType || '—' }}</span>
                  </div>
                  <div>
                    <span class="text-slate-500 block text-[10px]">الحالة الحالية</span>
                    <span [class]="getAdjStatusClass(selectedInvoice?.adjustmentRequestStatus)" class="px-2 py-0.5 rounded text-[10px] font-bold">
                      {{ getAdjustmentLabel(selectedInvoice?.adjustmentRequestStatus) }}
                    </span>
                  </div>
                  <div *ngIf="selectedInvoice?.adjustmentRequestId">
                    <span class="text-slate-500 block text-[10px]">رقم الطلب</span>
                    <span class="font-mono text-xs">#{{ selectedInvoice?.adjustmentRequestId }}</span>
                  </div>
                </div>
                <div class="text-xs text-slate-400 italic">
                  * التعديل يتم حصراً عبر نظام طلبات التعديل.
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Adjustment Request Modal -->
        <div *ngIf="activeModal === 'adjust'" class="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0">إنشاء طلب تعديل للفاتورة {{ selectedInvoice?.invoiceNumber }}</div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            <div>
              <label class="block text-slate-600 mb-1">نوع الطلب</label>
              <select [(ngModel)]="adjForm.requestType" class="input-field">
                <option value="DeleteLine">حذف سطر</option>
                <option value="ChangeQuantity">تعديل كمية</option>
                <option value="ChangeLineTotal">تعديل إجمالي السطر (سعر خاص)</option>
                <option value="CancelInvoice">إلغاء فاتورة بالكامل</option>
              </select>
            </div>
            <div>
              <label class="block text-slate-600 mb-1">السبب</label>
              <textarea [(ngModel)]="adjForm.reason" class="input-field h-20" placeholder="اذكر سبب طلب التعديل..."></textarea>
            </div>

            <div class="space-y-2">
              <p class="font-bold text-slate-700">تعديل الأصناف (اختياري حسب نوع الطلب):</p>
              <div class="border border-slate-200 rounded overflow-hidden max-h-60 overflow-y-auto">
                <table class="w-full text-right">
                  <thead class="bg-slate-50 text-slate-500 text-[10px] border-b border-slate-200 sticky top-0">
                    <tr>
                      <th class="py-2 px-3">المنتج</th>
                      <th class="py-2 px-3">الأصلي</th>
                      <th class="py-2 px-3 w-32" *ngIf="adjForm.requestType === 'ChangeQuantity'">الكمية الجديدة</th>
                      <th class="py-2 px-3 w-32" *ngIf="adjForm.requestType === 'ChangeLineTotal'">الإجمالي الجديد ($)</th>
                      <th class="py-2 px-3 w-32" *ngIf="adjForm.requestType === 'DeleteLine'">تحديد للحذف</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    <tr *ngFor="let line of selectedInvoice?.lines; let i = index">
                      <td class="py-2 px-3">{{ line.productName }}</td>
                      <td class="py-2 px-3 text-slate-400">{{ adjForm.requestType === 'ChangeLineTotal' ? (line.lineTotalUsdEffective | currency:'USD') : line.quantity }}</td>
                      <td class="py-2 px-3">
                        <input *ngIf="adjForm.requestType === 'ChangeQuantity'" type="number" [(ngModel)]="adjLineInputs[line.lineId]" class="input-field py-1 text-center" [placeholder]="line.quantity">
                        <input *ngIf="adjForm.requestType === 'ChangeLineTotal'" type="number" [(ngModel)]="adjLineInputs[line.lineId]" class="input-field py-1 text-center" [placeholder]="line.lineTotalUsdEffective">
                        <input *ngIf="adjForm.requestType === 'DeleteLine'" type="checkbox" (change)="toggleLineDelete(line.lineId)" class="w-4 h-4">
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
          <div class="p-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-2 shrink-0">
            <button class="btn-secondary text-xs" (click)="closeModal()">إلغاء</button>
            <button class="btn-primary text-xs" (click)="submitAdjustment()" [disabled]="!adjForm.reason || !adjForm.requestType">إرسال الطلب</button>
          </div>
        </div>

        <!-- Review Modal -->
        <div *ngIf="activeModal === 'review'" class="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 flex justify-between items-center shrink-0">
            <span>مراجعة طلب التعديل رقم: {{ selectedAdjustment?.requestId }}</span>
            <button (click)="closeModal()" class="text-slate-400 hover:text-slate-600">x</button>
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            <div class="grid grid-cols-2 gap-4 bg-blue-50 p-3 rounded border border-blue-100">
              <div><span class="text-blue-600 block text-[10px]">نوع الطلب</span><span class="font-bold">{{ selectedAdjustment?.requestType }}</span></div>
              <div><span class="text-blue-600 block text-[10px]">السبب</span><span class="font-bold">{{ selectedAdjustment?.reason }}</span></div>
            </div>

            <div class="space-y-2">
              <h4 class="font-bold text-slate-700">التعديلات المطلوبة:</h4>
              <div class="border border-slate-200 rounded overflow-hidden">
                <table class="w-full text-right">
                  <thead class="bg-slate-50 text-slate-500 text-[10px] border-b border-slate-200">
                    <tr>
                      <th class="py-2 px-3">معرف السطر</th>
                      <th class="py-2 px-3">الإجراء</th>
                      <th class="py-2 px-3">الكمية المطلوبة</th>
                      <th class="py-2 px-3">الإجمالي المطلوب</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    <tr *ngFor="let line of selectedAdjustment?.lines">
                      <td class="py-2 px-3 font-mono">{{ line.invoiceLineId }}</td>
                      <td class="py-2 px-3">{{ line.actionType }}</td>
                      <td class="py-2 px-3 font-bold text-blue-600">{{ line.requestedQuantity || '—' }}</td>
                      <td class="py-2 px-3 font-bold text-blue-600">{{ line.requestedLineTotalUsd | currency:'USD' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <p class="text-xs text-slate-400 mt-4 italic">* سيتم تعديل الفاتورة وتغيير قيمها فور الموافقة على هذا الطلب.</p>
          </div>
          <div class="p-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-2 shrink-0">
            <button class="btn-secondary text-xs" (click)="closeModal()">إغلاق</button>
            <button class="bg-red-500 hover:bg-red-600 text-white px-4 py-2 rounded text-xs" (click)="rejectAdjustment()">رفض الطلب</button>
            <button class="bg-green-600 hover:bg-green-700 text-white px-4 py-2 rounded text-xs font-bold" (click)="approveAdjustment()">الموافقة على التعديل</button>
          </div>
        </div>

      </div>

      <!-- Notifications -->
      <div *ngIf="(state$ | async)?.error" class="fixed bottom-4 left-4 bg-red-600 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span>{{ (state$ | async)?.error }}</span>
        <button (click)="invoicesState.clearError()">x</button>
      </div>

    </div>
  `
})
export class InvoicesComponent implements OnInit {
  state$ = this.invoicesState.state$;
  filters: any = {
    customerName: '',
    status: '',
    dateFrom: '',
    dateTo: '',
    employeeId: null,
    adjustmentRequestStatus: '',
    manualPriceEdited: false
  };

  activeModal: 'details' | 'adjust' | 'review' | null = null;
  selectedInvoice: InvoiceDetailsResponse | null = null;
  selectedAdjustment: any = null; // AdjustmentRequestResponse
  isPrinting = false;
  
  // Adjustment Form
  adjForm: any = { requestType: 'ChangeQuantity', reason: '' };
  adjLineInputs: { [key: number]: number } = {};
  deletedLines: Set<number> = new Set();

  constructor(
    public invoicesState: InvoicesStateService,
    private api: InvoicesApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.invoicesState.loadInvoices();
  }

  search() {
    this.invoicesState.loadInvoices(this.filters, 1);
  }

  changePage(delta: number) {
    const current = this.getSnapshot();
    const newPage = current.page + delta;
    this.invoicesState.loadInvoices(this.filters, newPage);
  }

  isLastPage() {
    const s = this.getSnapshot();
    return s.page * s.pageSize >= s.totalCount;
  }

  private getSnapshot() {
    let snapshot: any;
    this.state$.subscribe(s => snapshot = s).unsubscribe();
    return snapshot;
  }

  viewDetails(id: number) {
    this.api.getInvoiceDetails(id).subscribe(res => {
      this.selectedInvoice = res;
      this.activeModal = 'details';
    });
  }

  printReceipt() {
    if (!this.selectedInvoice || this.isPrinting) return;
    this.isPrinting = true;
    window.print();
    window.setTimeout(() => this.isPrinting = false, 500);
  }

  loadToCashier(id: number) {
    this.api.loadSuspended(id).subscribe({
      next: () => {
        this.router.navigate(['/cashier']);
      },
      error: (err) => {
        // Error handled in state or global
      }
    });
  }

  openAdjustment(id: number) {
    this.api.getInvoiceDetails(id).subscribe({
      next: (res) => {
        this.selectedInvoice = res;
        this.adjForm = { requestType: 'ChangeQuantity', reason: '' };
        this.adjLineInputs = {};
        this.deletedLines = new Set();
        this.activeModal = 'adjust';
      },
      error: (err) => {
        this.invoicesState.setError(err.error?.error || 'خطأ في جلب بيانات الفاتورة');
      }
    });
  }

  toggleLineDelete(lineId: number) {
    if (this.deletedLines.has(lineId)) this.deletedLines.delete(lineId);
    else this.deletedLines.add(lineId);
  }

  submitAdjustment() {
    if (!this.selectedInvoice) return;
    
    let lines: any[] = [];

    if (this.adjForm.requestType === 'DeleteLine') {
      lines = Array.from(this.deletedLines).map(id => ({ invoiceLineId: id }));
    } else if (this.adjForm.requestType === 'ChangeQuantity') {
      lines = Object.keys(this.adjLineInputs).map(id => ({
        invoiceLineId: Number(id),
        requestedQuantity: this.adjLineInputs[Number(id)]
      }));
    } else if (this.adjForm.requestType === 'ChangeLineTotal') {
      lines = Object.keys(this.adjLineInputs).map(id => ({
        invoiceLineId: Number(id),
        requestedLineTotalUsd: this.adjLineInputs[Number(id)]
      }));
    }

    const request: CreateAdjustmentRequest = {
      requestType: this.adjForm.requestType,
      reason: this.adjForm.reason,
      lines: lines.length ? lines : undefined
    };

    this.api.createAdjustmentRequest(this.selectedInvoice.invoiceId, request).subscribe({
      next: () => {
        this.closeModal();
        this.invoicesState.loadInvoices();
      },
      error: (err) => {
        this.invoicesState.setError(err.error?.error || 'خطأ أثناء إرسال طلب التعديل');
      }
    });
  }

  reviewAdjustment(invoiceId: number, requestId: number) {
    this.api.getAdjustmentRequest(invoiceId, requestId).subscribe({
      next: (res) => {
        this.selectedAdjustment = res;
        this.activeModal = 'review';
      },
      error: (err) => {
        this.invoicesState.setError(err.error?.error || 'خطأ في جلب تفاصيل الطلب');
      }
    });
  }

  approveAdjustment() {
    if (!this.selectedAdjustment) return;
    const { invoiceId, requestId } = this.selectedAdjustment;
    this.api.approveAdjustmentRequest(invoiceId, requestId).subscribe({
      next: () => {
        this.closeModal();
        this.invoicesState.loadInvoices();
      },
      error: (err) => {
        this.invoicesState.setError(err.error?.error || 'خطأ أثناء الموافقة على الطلب');
      }
    });
  }

  rejectAdjustment() {
    if (!this.selectedAdjustment) return;
    const { invoiceId, requestId } = this.selectedAdjustment;
    this.api.rejectAdjustmentRequest(invoiceId, requestId).subscribe({
      next: () => {
        this.closeModal();
        this.invoicesState.loadInvoices();
      },
      error: (err) => {
        this.invoicesState.setError(err.error?.error || 'خطأ أثناء رفض الطلب');
      }
    });
  }

  closeModal() {
    this.activeModal = null;
    this.selectedInvoice = null;
    this.selectedAdjustment = null;
    this.isPrinting = false;
  }

  exportInvoices() {
    const request = {
      format: 'excel',
      customerName: this.filters.customerName || undefined,
      status: this.filters.status || undefined,
      dateFrom: this.filters.dateFrom || undefined,
      dateTo: this.filters.dateTo || undefined,
      employeeId: this.filters.employeeId || undefined,
      adjustmentRequestStatus: this.filters.adjustmentRequestStatus || undefined,
      manualPriceEdited: this.filters.manualPriceEdited ? true : undefined
    };

    this.api.exportInvoices(request).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;

        let filename = 'invoices_export.xlsx';
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
        this.invoicesState.setError('فشل تصدير الفواتير');
      }
    });
  }

  getStatusLabel(s: string) {
    switch(s) {
      case 'Completed': return 'مكتملة';
      case 'Suspended': return 'معلقة';
      case 'Modified': return 'معدلة';
      case 'Cancelled': return 'ملغاة';
      default: return s;
    }
  }

  getStatusClass(s: string) {
    switch(s) {
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Suspended': return 'bg-amber-100 text-amber-800';
      case 'Modified': return 'bg-blue-100 text-blue-800';
      case 'Cancelled': return 'bg-red-100 text-red-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  }

  getAdjustmentLabel(s?: string) {
    switch(s) {
      case 'Pending': return 'قيد الانتظار';
      case 'Approved': return 'تمت الموافقة';
      case 'Rejected': return 'مرفوض';
      default: return s || 'غير معروف';
    }
  }

  getAdjStatusClass(s?: string) {
    switch(s) {
      case 'Pending': return 'bg-amber-100 text-amber-800';
      case 'Approved': return 'bg-green-100 text-green-800';
      case 'Rejected': return 'bg-red-100 text-red-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  }
}
