import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CashierStateService } from './services/cashier-state.service';
import { ProductsPaneComponent } from './components/products-pane.component';
import { InvoicePaneComponent } from './components/invoice-pane.component';

@Component({
  selector: 'app-cashier',
  standalone: true,
  imports: [CommonModule, FormsModule, ProductsPaneComponent, InvoicePaneComponent],
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
  activeModal: 'customer' | 'discount' | 'suspend' | 'cancel' | 'complete' | null = null;
  modalData: any = {};
  
  state = this.cashierState['stateObj']; // bind to snapshot, we'll sync via subscribe

  constructor(public cashierState: CashierStateService) {}

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
    this.activeModal = null;
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
      next: () => this.closeModal(),
      error: () => this.closeModal()
    });
  }
}
