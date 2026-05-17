import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartResponse, CartLineDto } from '../services/cashier-api.service';

@Component({
  selector: 'app-invoice-pane',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="h-full flex flex-col bg-white border-l border-slate-200">
      
      <!-- Customer Block -->
      <div class="p-4 border-b border-slate-200 shrink-0 bg-slate-50 flex justify-between items-center cursor-pointer hover:bg-slate-100 transition-colors" (click)="customerClicked.emit()">
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path></svg>
          <span class="font-medium text-slate-700 text-sm">
            {{ cart?.customerName || 'عميل نقدي' }}
          </span>
        </div>
        <div class="text-xs text-primary font-medium">تغيير</div>
      </div>

      <!-- Lines Table (Scrollable) -->
      <div class="flex-1 overflow-y-auto">
        <table class="w-full text-sm text-right">
          <thead class="text-xs text-slate-500 bg-slate-100 sticky top-0 z-10">
            <tr>
              <th class="px-3 py-2 font-medium">المنتج</th>
              <th class="px-3 py-2 font-medium w-20 text-center">الكمية</th>
              <th class="px-3 py-2 font-medium w-24 text-left">السعر</th>
              <th class="px-3 py-2 font-medium w-24 text-left">الإجمالي</th>
              <th class="px-2 py-2 w-10"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngIf="!cart?.lines?.length">
              <td colspan="5" class="text-center py-8 text-slate-400">الفاتورة فارغة. أضف منتجات للبدء.</td>
            </tr>
            <ng-container *ngFor="let line of cart?.lines">
              <tr 
                class="border-b border-slate-100 hover:bg-slate-50 transition-colors"
                (dblclick)="onDblClickLine(line)"
              >
                <!-- Product Name -->
                <td class="px-3 py-3">
                  <div class="font-medium text-slate-800 line-clamp-2">{{ line.productName }}</div>
                  <div *ngIf="line.offerId" class="text-xs text-green-600 mt-0.5">يشمله عرض ترويجي</div>
                  <div *ngIf="line.isPriceOverridden" class="text-xs text-orange-500 mt-0.5">تسعير يدوي</div>
                </td>
                
                <!-- Quantity (Inline Edit) -->
                <td class="px-3 py-3 text-center">
                  <ng-container *ngIf="editingLineId !== line.lineId; else qtyEdit">
                    <span class="font-semibold">{{ line.quantity }}</span>
                  </ng-container>
                  <ng-template #qtyEdit>
                    <input 
                      type="number" 
                      [(ngModel)]="editData.quantity" 
                      class="w-16 text-center border border-primary rounded px-1 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                      min="1"
                      (blur)="onBlurLine(line)"
                      (keyup.enter)="onBlurLine(line)"
                      #qtyInput
                    >
                  </ng-template>
                </td>

                <!-- Price -->
                <td class="px-3 py-3 text-left whitespace-nowrap">
                  <span class="text-slate-600">{{ line.unitPriceUsdOriginal | currency:'USD' }}</span>
                </td>

                <!-- Total (Inline Edit Override) -->
                <td class="px-3 py-3 text-left whitespace-nowrap">
                  <ng-container *ngIf="editingLineId !== line.lineId; else totalEdit">
                    <div class="font-semibold" [class.text-primary]="!line.isPriceOverridden" [class.text-orange-600]="line.isPriceOverridden">
                      {{ line.lineTotalUsdEffective | currency:'USD' }}
                    </div>
                    <div *ngIf="line.lineTotalUsdEffective !== line.lineTotalUsdOriginal" class="text-xs text-slate-400 line-through">
                      {{ line.lineTotalUsdOriginal | currency:'USD' }}
                    </div>
                  </ng-container>
                  <ng-template #totalEdit>
                    <input 
                      type="number" 
                      [(ngModel)]="editData.overrideTotal" 
                      class="w-20 text-left border border-primary rounded px-1 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                      (blur)="onBlurLine(line)"
                      (keyup.enter)="onBlurLine(line)"
                    >
                  </ng-template>
                </td>

                <!-- Delete -->
                <td class="px-2 py-3 text-center">
                  <button (click)="deleteLine.emit(line.lineId)" class="text-red-400 hover:text-red-600 p-1" title="حذف السطر">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                  </button>
                </td>
              </tr>
            </ng-container>
          </tbody>
        </table>
      </div>

      <!-- Summary Block (Fixed) -->
      <div class="p-4 border-t border-slate-200 bg-slate-50 shrink-0">
        
        <div class="flex justify-between text-sm text-slate-600 mb-2">
          <span>المجموع الفرعي:</span>
          <span>{{ (cart?.subtotalUsd || 0) | currency:'USD' }}</span>
        </div>
        
        <div 
          class="flex justify-between text-sm mb-3 cursor-pointer group"
          [class.text-green-600]="(cart?.invoiceDiscountValue || 0) > 0"
          [class.text-slate-600]="!cart?.invoiceDiscountValue"
          (click)="discountClicked.emit()"
        >
          <span class="border-b border-dashed border-slate-300 group-hover:border-primary transition-colors">
            الخصم <span *ngIf="cart?.invoiceDiscountType === 'Percentage'">({{ cart?.invoiceDiscountValue }}%)</span>:
          </span>
          <span>
            <ng-container *ngIf="cart?.invoiceDiscountType === 'Percentage'">-</ng-container>
            <ng-container *ngIf="cart?.invoiceDiscountType === 'Fixed'">{{ cart?.invoiceDiscountValue | currency:'USD' }}</ng-container>
            <ng-container *ngIf="!cart?.invoiceDiscountValue">0.00</ng-container>
          </span>
        </div>

        <div class="flex justify-between text-lg font-bold text-slate-800 mb-4 pt-2 border-t border-slate-200">
          <span>الإجمالي النهائي:</span>
          <span class="text-primary">{{ (cart?.totalUsd || 0) | currency:'USD' }}</span>
        </div>

        <!-- Action Buttons -->
        <div class="grid grid-cols-3 gap-2">
          <button 
            class="btn-secondary text-sm py-2"
            [disabled]="!cart?.lines?.length || isLoading"
            (click)="suspendClicked.emit()"
          >
            تعليق
          </button>
          <button 
            class="btn-danger text-sm py-2 bg-red-100 text-red-700 hover:bg-red-200 border-none"
            [disabled]="!cart?.lines?.length || isLoading"
            (click)="cancelClicked.emit()"
          >
            إلغاء
          </button>
          <button 
            class="btn-primary text-sm py-2"
            [disabled]="!cart?.lines?.length || isLoading"
            (click)="completeClicked.emit()"
          >
            إتمام الدفع
          </button>
        </div>
      </div>
      
    </div>
  `
})
export class InvoicePaneComponent {
  @Input() cart: CartResponse | null = null;
  @Input() isLoading = false;

  @Output() updateLine = new EventEmitter<{lineId: number, quantity?: number, overrideLineTotalUsd?: number | null}>();
  @Output() deleteLine = new EventEmitter<number>();
  @Output() customerClicked = new EventEmitter<void>();
  @Output() discountClicked = new EventEmitter<void>();
  @Output() suspendClicked = new EventEmitter<void>();
  @Output() cancelClicked = new EventEmitter<void>();
  @Output() completeClicked = new EventEmitter<void>();

  // Inline editing state
  editingLineId: number | null = null;
  editData = { quantity: 1, overrideTotal: null as number | null };

  onDblClickLine(line: CartLineDto) {
    this.editingLineId = line.lineId;
    this.editData = {
      quantity: line.quantity,
      overrideTotal: line.isPriceOverridden ? line.lineTotalUsdEffective : null
    };
    // Note: setTimeout logic to focus input could be added here if needed via ViewChild
  }

  onBlurLine(originalLine: CartLineDto) {
    if (this.editingLineId !== originalLine.lineId) return;

    const newQty = this.editData.quantity;
    let newOverride = this.editData.overrideTotal;

    // Validation: Block quantity 0
    if (newQty <= 0) {
      this.editingLineId = null; // revert without saving
      return; 
    }

    // Logic: If quantity changes after manual override, remove override
    if (originalLine.isPriceOverridden && newQty !== originalLine.quantity) {
      newOverride = null;
    }

    // Check if actually changed
    const qtyChanged = newQty !== originalLine.quantity;
    const overrideChanged = newOverride !== (originalLine.isPriceOverridden ? originalLine.lineTotalUsdEffective : null);

    if (qtyChanged || overrideChanged) {
      this.updateLine.emit({
        lineId: originalLine.lineId,
        quantity: qtyChanged ? newQty : undefined,
        overrideLineTotalUsd: newOverride !== undefined ? newOverride : undefined
      });
    }

    this.editingLineId = null;
  }
}
