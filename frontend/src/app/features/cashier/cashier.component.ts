import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CashierStateService } from './services/cashier-state.service';
import { ProductsPaneComponent } from './components/products-pane.component';
import { InvoicePaneComponent } from './components/invoice-pane.component';
import { InvoicesApiService, InvoiceDetailsResponse } from '../invoices/services/invoices-api.service';
import { ReceiptPrintComponent } from '../../shared/components/receipt-print/receipt-print.component';

@Component({
  selector: 'app-cashier',
  standalone: true,
  imports: [CommonModule, FormsModule, ProductsPaneComponent, InvoicePaneComponent, ReceiptPrintComponent],
  template: `
    <div class="flex flex-col h-screen bg-slate-100 overflow-hidden text-slate-800" dir="rtl">
      
      <!-- Top Bar -->
      <div class="h-14 bg-primary text-white flex items-center justify-between px-4 shrink-0 shadow-md z-20">
        <div class="flex items-center gap-3">
          <h1 class="font-bold text-lg">نقطة البيع</h1>
          <span class="bg-white/20 px-2 py-0.5 rounded text-xs">
            {{ state.cart?.status === 'Working' ? 'فاتورة نشطة' : 'فاتورة جديدة' }}
          </span>
        </div>
        <div>
          <!-- Tab toggles for Mobile -->
          <div class="md:hidden flex bg-primary-dark rounded p-1">
            <button 
              class="px-3 py-1 text-sm rounded transition-colors"
              [class.bg-white]="activeTab === 'products'"
              [class.text-primary]="activeTab === 'products'"
              (click)="activeTab = 'products'"
            >
              المنتجات
            </button>
            <button 
              class="px-3 py-1 text-sm rounded transition-colors"
              [class.bg-white]="activeTab === 'invoice'"
              [class.text-primary]="activeTab === 'invoice'"
              (click)="activeTab = 'invoice'"
            >
              الفاتورة
            </button>
          </div>
        </div>
      </div>

      <!-- Main Content (Split Desktop, Tabbed Mobile) -->
      <div class="flex-1 flex overflow-hidden relative">
        
        <!-- Left Pane: Products -->
        <div 
          class="w-full md:w-[60%] lg:w-[65%] h-full absolute md:relative z-10 md:z-auto transition-transform duration-300"
          [class.-translate-x-full]="activeTab !== 'products'"
          [class.md:translate-x-0]="true"
        >
          <app-products-pane
            [products]="state.products"
            [isLoading]="state.isLoading && state.products.length === 0"
            (productSelected)="onProductSelected($event)"
          ></app-products-pane>
        </div>

        <!-- Right Pane: Invoice -->
        <div 
          class="w-full md:w-[40%] lg:w-[35%] h-full absolute md:relative z-10 md:z-auto bg-white shadow-xl md:shadow-none transition-transform duration-300"
          [class.translate-x-full]="activeTab !== 'invoice'"
          [class.md:translate-x-0]="true"
        >
          <app-invoice-pane
            [cart]="state.cart"
            [isLoading]="state.isLoading"
            (updateLine)="onUpdateLine($event)"
            (deleteLine)="onDeleteLine($event)"
            (customerClicked)="openCustomerModal()"
            (discountClicked)="openDiscountModal()"
            (suspendClicked)="openSuspendModal()"
            (cancelClicked)="openCancelModal()"
            (completeClicked)="openCompleteModal()"
          ></app-invoice-pane>
        </div>

      </div>

      <!-- Error Toast -->
      <div *ngIf="state.error" class="fixed bottom-4 left-4 right-4 md:right-auto md:w-96 bg-red-600 text-white p-4 rounded shadow-lg z-50 flex justify-between items-center animate-fade-in-up">
        <span class="text-sm">{{ state.error }}</span>
        <button (click)="cashierState.clearError()" class="text-white/80 hover:text-white">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
        </button>
      </div>

      <!-- Overlay for Modals -->
      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-40 flex items-center justify-center p-4">
        
        <!-- Customer Modal -->
        <div *ngIf="activeModal === 'customer'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden animate-scale-in">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800">بيانات العميل</div>
          <div class="p-4">
            <label class="block text-sm text-slate-600 mb-1">اسم العميل</label>
            <input type="text" [(ngModel)]="modalData.customerName" class="input-field" placeholder="أدخل اسم العميل" (keyup.enter)="saveCustomer()">
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end">
            <button class="btn-secondary text-sm" (click)="closeModal()">إلغاء</button>
            <button class="btn-danger text-sm" *ngIf="state.cart?.customerName" (click)="deleteCustomer()">مسح العميل</button>
            <button class="btn-primary text-sm" (click)="saveCustomer()">حفظ</button>
          </div>
        </div>

        <!-- Discount Modal -->
        <div *ngIf="activeModal === 'discount'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden animate-scale-in">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800">خصم الفاتورة</div>
          <div class="p-4 space-y-4">
            <div>
              <label class="block text-sm text-slate-600 mb-1">نوع الخصم</label>
              <select [(ngModel)]="modalData.discountType" class="input-field">
                <option value="Fixed">مبلغ ثابت</option>
                <option value="Percentage">نسبة مئوية (%)</option>
              </select>
            </div>
            <div>
              <label class="block text-sm text-slate-600 mb-1">القيمة</label>
              <input type="number" [(ngModel)]="modalData.discountValue" class="input-field" placeholder="0" (keyup.enter)="saveDiscount()">
            </div>
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end">
            <button class="btn-secondary text-sm" (click)="closeModal()">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveDiscount()">حفظ الخصم</button>
          </div>
        </div>

        <!-- Suspend Modal -->
        <div *ngIf="activeModal === 'suspend'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden animate-scale-in">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800">تعليق الفاتورة</div>
          <div class="p-4">
            <label class="block text-sm text-slate-600 mb-1">سبب التعليق (إلزامي)</label>
            <input type="text" [(ngModel)]="modalData.suspensionReason" class="input-field" placeholder="مثال: ذهب لإحضار المحفظة" (keyup.enter)="confirmSuspend()">
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end">
            <button class="btn-secondary text-sm" (click)="closeModal()">إلغاء</button>
            <button class="btn-primary text-sm" [disabled]="!modalData.suspensionReason" (click)="confirmSuspend()">تأكيد التعليق</button>
          </div>
        </div>

        <!-- Cancel Confirm Modal -->
        <div *ngIf="activeModal === 'cancel'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden animate-scale-in">
          <div class="p-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تحذير</div>
          <div class="p-4 text-slate-700">
            هل أنت متأكد من إلغاء الفاتورة الحالية بالكامل؟ لا يمكن التراجع عن هذا الإجراء.
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end">
            <button class="btn-secondary text-sm" (click)="closeModal()">تراجع</button>
            <button class="btn-danger text-sm" (click)="confirmCancel()">تأكيد الإلغاء</button>
          </div>
        </div>

        <!-- Complete Confirm Modal -->
        <div *ngIf="activeModal === 'complete'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden animate-scale-in">
          <div class="p-4 border-b border-green-100 bg-green-50 font-bold text-green-800">إتمام الدفع</div>
          <div class="p-4 space-y-4">
            <div class="flex justify-between items-center p-3 bg-slate-50 rounded">
              <span class="text-slate-600">الإجمالي المطلوب:</span>
              <span class="text-xl font-bold text-primary">{{ (state.cart?.totalUsd || 0) | currency:'USD' }}</span>
            </div>
            <!-- Future: Tendered amount inputs could go here -->
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end">
            <button class="btn-secondary text-sm" (click)="closeModal()">تراجع</button>
            <button class="btn-primary text-sm" (click)="confirmComplete()">تأكيد الدفع</button>
          </div>
        </div>

        <!-- Receipt Preview Modal -->
        <div *ngIf="activeModal === 'receipt'" class="bg-white rounded-lg shadow-xl w-full max-w-lg overflow-hidden animate-scale-in flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-green-100 bg-green-50 font-bold text-green-800 flex items-center justify-between shrink-0 no-print">
            <span>تمت عملية الدفع</span>
            <button (click)="closeModal()" class="text-green-700/70 hover:text-green-900">x</button>
          </div>
          <div class="p-4 overflow-y-auto flex-1 bg-slate-50">
            <div *ngIf="isReceiptLoading" class="py-10 text-center text-slate-500 text-sm no-print">
              جاري تحميل بيانات الإيصال...
            </div>

            <div *ngIf="receiptError" class="bg-amber-50 border border-amber-200 text-amber-800 rounded p-3 text-sm no-print">
              {{ receiptError }}
            </div>

            <app-receipt-print *ngIf="completedInvoice" [invoice]="completedInvoice"></app-receipt-print>
          </div>
          <div class="p-4 bg-white border-t border-slate-100 flex gap-2 justify-end shrink-0 no-print">
            <button class="btn-secondary text-sm" (click)="closeModal()">إغلاق</button>
            <button
              class="btn-primary text-sm"
              data-testid="cashier-print-button"
              [disabled]="!completedInvoice || isReceiptLoading || isPrinting"
              (click)="printReceipt()"
            >
              <svg class="w-4 h-4 ml-1 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 9V2h12v7M6 18H4a2 2 0 01-2-2v-5a2 2 0 012-2h16a2 2 0 012 2v5a2 2 0 01-2 2h-2m-12 0h12v4H6v-4z"></path></svg>
              طباعة الإيصال
            </button>
          </div>
        </div>

      </div>

    </div>
  `,
  styles: [`
    .animate-fade-in-up { animation: fadeInUp 0.3s ease-out forwards; }
    .animate-scale-in { animation: scaleIn 0.2s ease-out forwards; }
    @keyframes fadeInUp { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    @keyframes scaleIn { from { opacity: 0; transform: scale(0.95); } to { opacity: 1; transform: scale(1); } }
  `]
})
export class CashierComponent implements OnInit {
  activeTab: 'products' | 'invoice' = 'invoice'; // Mobile default to invoice
  activeModal: 'customer' | 'discount' | 'suspend' | 'cancel' | 'complete' | 'receipt' | null = null;
  modalData: any = {};
  completedInvoice: InvoiceDetailsResponse | null = null;
  isReceiptLoading = false;
  receiptError: string | null = null;
  isPrinting = false;
  
  state = this.cashierState['stateObj']; // bind to snapshot, we'll sync via subscribe

  constructor(
    public cashierState: CashierStateService,
    private invoicesApi: InvoicesApiService
  ) {}

  ngOnInit(): void {
    this.cashierState.state$.subscribe(s => this.state = s);
    this.cashierState.loadInitialState();
  }

  // --- Products Pane ---
  onProductSelected(productId: number) {
    this.cashierState.addByProduct(productId);
    // Auto switch to invoice tab on mobile when product added
    if (window.innerWidth < 768) {
      this.activeTab = 'invoice';
    }
  }

  // --- Invoice Pane ---
  onUpdateLine(evt: {lineId: number, quantity?: number, overrideLineTotalUsd?: number | null}) {
    this.cashierState.updateLine(evt.lineId, evt.quantity, evt.overrideLineTotalUsd);
  }

  onDeleteLine(lineId: number) {
    this.cashierState.deleteLine(lineId);
  }

  // --- Modals ---
  closeModal() {
    const closingModal = this.activeModal;
    this.activeModal = null;
    if (closingModal === 'receipt') {
      this.completedInvoice = null;
      this.isReceiptLoading = false;
      this.receiptError = null;
      this.isPrinting = false;
    }
  }

  openCustomerModal() {
    this.modalData = { customerName: this.state.cart?.customerName || '' };
    this.activeModal = 'customer';
  }

  saveCustomer() {
    if (this.modalData.customerName) {
      this.cashierState.updateCustomer(this.modalData.customerName);
    }
    this.closeModal();
  }

  deleteCustomer() {
    this.cashierState.deleteCustomer();
    this.closeModal();
  }

  openDiscountModal() {
    this.modalData = { 
      discountType: this.state.cart?.invoiceDiscountType || 'Fixed',
      discountValue: this.state.cart?.invoiceDiscountValue || 0
    };
    this.activeModal = 'discount';
  }

  saveDiscount() {
    this.cashierState.updateDiscount(this.modalData.discountType, this.modalData.discountValue);
    this.closeModal();
  }

  openSuspendModal() {
    this.modalData = { suspensionReason: '' };
    this.activeModal = 'suspend';
  }

  confirmSuspend() {
    if (!this.modalData.suspensionReason) return;
    this.cashierState.suspendCart(this.modalData.suspensionReason).subscribe({
      next: () => this.closeModal(),
      error: () => this.closeModal() // error toast will show via state
    });
  }

  openCancelModal() {
    this.activeModal = 'cancel';
  }

  confirmCancel() {
    this.cashierState.cancelCart().subscribe({
      next: () => this.closeModal(),
      error: () => this.closeModal()
    });
  }

  openCompleteModal() {
    this.activeModal = 'complete';
  }

  confirmComplete() {
    this.cashierState.completeCart().subscribe({
      next: (cart) => {
        if (cart.invoiceId) {
          this.openReceiptAfterComplete(cart.invoiceId);
        } else {
          this.closeModal();
        }
      },
      error: () => this.closeModal()
    });
  }

  private openReceiptAfterComplete(invoiceId: number) {
    this.completedInvoice = null;
    this.receiptError = null;
    this.isReceiptLoading = true;
    this.activeModal = 'receipt';

    this.invoicesApi.getInvoiceDetails(invoiceId).subscribe({
      next: (invoice) => {
        this.completedInvoice = invoice;
        this.isReceiptLoading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.isReceiptLoading = false;
        this.receiptError = err.error?.error === 'UNAUTHORIZED_SCREEN_ACCESS'
          ? 'تم الدفع بنجاح، لكن لا تملك صلاحية عرض تفاصيل الفاتورة للطباعة.'
          : 'تم الدفع بنجاح، لكن تعذر تحميل بيانات الإيصال للطباعة.';
      }
    });
  }

  printReceipt() {
    if (!this.completedInvoice || this.isPrinting) return;
    this.isPrinting = true;
    window.print();
    window.setTimeout(() => this.isPrinting = false, 500);
  }
}
