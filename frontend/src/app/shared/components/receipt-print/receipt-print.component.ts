import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export interface ReceiptPrintLine {
  lineId: number;
  productId: number;
  productName: string;
  offerId?: number;
  quantity: number;
  unitPriceUsdOriginal: number;
  lineTotalUsdOriginal: number;
  lineTotalUsdEffective: number;
  isPriceOverridden: boolean;
  sortOrder: number;
}

export interface ReceiptPrintInvoice {
  invoiceId: number;
  invoiceNumber: string;
  status: string;
  customerName?: string;
  originalEmployeeId: number;
  employeeName?: string;
  invoiceDiscountType?: string;
  invoiceDiscountValue?: number;
  subtotalUsd: number;
  totalUsd: number;
  exchangeRateSypSnapshot?: number | null;
  totalSyp?: number | null;
  createdAt: string;
  completedAt?: string;
  lines: ReceiptPrintLine[];
}

@Component({
  selector: 'app-receipt-print',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section *ngIf="invoice" class="receipt-print-root" dir="rtl" aria-label="إيصال بيع">
      <header class="receipt-header">
        <div class="store-name">BazarFlow</div>
        <div class="receipt-title">إيصال بيع</div>
        <div *ngIf="reprint" class="receipt-tag">نسخة إعادة طباعة</div>
      </header>

      <div class="receipt-meta">
        <div>
          <span>رقم الفاتورة</span>
          <strong>{{ invoice.invoiceNumber }}</strong>
        </div>
        <div>
          <span>التاريخ</span>
          <strong>{{ receiptDate(invoice) | date:'yyyy-MM-dd HH:mm' }}</strong>
        </div>
        <div>
          <span>الموظف</span>
          <strong>{{ invoice.employeeName || ('#' + invoice.originalEmployeeId) }}</strong>
        </div>
        <div>
          <span>الزبون</span>
          <strong>{{ invoice.customerName || 'زبون نقدي' }}</strong>
        </div>
      </div>

      <div class="receipt-lines" aria-label="المنتجات">
        <div class="lines-heading">
          <span>المنتجات</span>
          <span>الإجمالي</span>
        </div>
        <div *ngFor="let line of sortedLines(invoice.lines)" class="receipt-line">
          <div class="product-name">{{ line.productName }}</div>
          <div class="line-equation">
            <span class="num">{{ line.quantity | number:'1.0-3' }}</span>
            <span>×</span>
            <span class="num">{{ line.unitPriceUsdOriginal | number:'1.2-2' }} $</span>
            <span>=</span>
            <strong class="num">{{ line.lineTotalUsdEffective | number:'1.2-2' }} $</strong>
          </div>
          <div *ngIf="lineDiscount(line) > 0 || line.isPriceOverridden || line.offerId" class="line-note">
            <span *ngIf="lineDiscount(line) > 0">خصم السطر: {{ lineDiscount(line) | number:'1.2-2' }} $</span>
            <span *ngIf="line.isPriceOverridden">سعر معدل</span>
            <span *ngIf="line.offerId">عرض #{{ line.offerId }}</span>
          </div>
        </div>
      </div>

      <div class="receipt-totals">
        <div>
          <span>المجموع الفرعي</span>
          <strong>{{ invoice.subtotalUsd | number:'1.2-2' }} $</strong>
        </div>
        <div>
          <span>خصم الفاتورة{{ invoice.invoiceDiscountType ? ' (' + discountLabel(invoice.invoiceDiscountType) + ')' : '' }}</span>
          <strong>{{ invoiceDiscountAmount(invoice) | number:'1.2-2' }} $</strong>
        </div>
        <div class="grand-total">
          <span>الإجمالي بالدولار</span>
          <strong>{{ invoice.totalUsd | number:'1.2-2' }} $</strong>
        </div>
        <div>
          <span>سعر الصرف</span>
          <strong>
            <ng-container *ngIf="invoice.exchangeRateSypSnapshot != null; else missingRate">
              {{ invoice.exchangeRateSypSnapshot | number:'1.0-4' }}
            </ng-container>
          </strong>
        </div>
        <div class="grand-total">
          <span>الإجمالي بالليرة</span>
          <strong>
            <ng-container *ngIf="invoice.totalSyp != null; else missingSyp">
              {{ invoice.totalSyp | number:'1.0-0' }} ل.س
            </ng-container>
          </strong>
        </div>
      </div>

      <ng-template #missingRate>غير متوفر</ng-template>
      <ng-template #missingSyp>غير متوفر</ng-template>

      <footer class="receipt-footer">
        <div>شكراً لتسوقكم معنا</div>
        <div>احتفظ بالإيصال للمراجعة</div>
      </footer>
    </section>
  `,
  styles: [`
    :host {
      display: block;
    }

    .receipt-print-root {
      width: 80mm;
      max-width: 100%;
      margin: 0 auto;
      background: #fff;
      color: #000;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 10px;
      font-size: 11px;
      line-height: 1.45;
      font-family: Arial, Tahoma, sans-serif;
    }

    .receipt-header {
      text-align: center;
      border-bottom: 1px dashed #000;
      padding-bottom: 8px;
      margin-bottom: 8px;
    }

    .store-name {
      font-size: 17px;
      font-weight: 800;
      letter-spacing: 0;
    }

    .receipt-title {
      font-size: 12px;
      font-weight: 700;
      color: #111;
    }

    .receipt-tag {
      display: inline-block;
      margin-top: 6px;
      padding: 2px 8px;
      border: 1px solid #cbd5e1;
      border-radius: 999px;
      color: #111;
      font-size: 10px;
    }

    .receipt-meta,
    .receipt-totals {
      display: grid;
      gap: 4px;
    }

    .receipt-meta > div,
    .receipt-totals > div {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      align-items: baseline;
    }

    .receipt-meta span,
    .receipt-totals span {
      color: #111;
      white-space: nowrap;
    }

    .receipt-meta strong,
    .receipt-totals strong,
    .num {
      direction: ltr;
      unicode-bidi: plaintext;
      text-align: left;
    }

    .receipt-lines {
      margin: 10px 0;
      border-top: 1px dashed #000;
      border-bottom: 1px dashed #000;
    }

    .lines-heading,
    .receipt-line {
      border-bottom: 1px solid #e5e7eb;
    }

    .lines-heading {
      display: flex;
      justify-content: space-between;
      padding: 5px 0;
      color: #111;
      font-size: 10px;
      font-weight: 700;
    }

    .receipt-line {
      padding: 6px 0;
    }

    .receipt-line:last-child {
      border-bottom: 0;
    }

    .product-name {
      overflow-wrap: anywhere;
      font-weight: 700;
      margin-bottom: 2px;
    }

    .line-equation {
      display: flex;
      align-items: baseline;
      justify-content: flex-end;
      gap: 4px;
      direction: ltr;
      unicode-bidi: plaintext;
      color: #111;
    }

    .line-note {
      color: #334155;
      font-size: 10px;
      font-weight: 500;
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-top: 2px;
    }

    .grand-total {
      border-top: 1px dashed #000;
      padding-top: 6px;
      margin-top: 2px;
      font-size: 13px;
    }

    .receipt-footer {
      margin-top: 10px;
      padding-top: 8px;
      border-top: 1px dashed #000;
      text-align: center;
      color: #111;
      font-weight: 600;
      font-size: 10px;
    }
  `]
})
export class ReceiptPrintComponent {
  @Input() invoice: ReceiptPrintInvoice | null = null;
  @Input() reprint = false;

  sortedLines(lines: ReceiptPrintLine[]): ReceiptPrintLine[] {
    return [...lines].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  receiptDate(invoice: ReceiptPrintInvoice): string {
    return invoice.completedAt || invoice.createdAt;
  }

  lineDiscount(line: ReceiptPrintLine): number {
    return Math.max(0, line.lineTotalUsdOriginal - line.lineTotalUsdEffective);
  }

  invoiceDiscountAmount(invoice: ReceiptPrintInvoice): number {
    return Math.max(0, invoice.subtotalUsd - invoice.totalUsd);
  }

  discountLabel(type: string): string {
    switch (type) {
      case 'Percent':
        return 'نسبة';
      case 'Amount':
        return 'مبلغ';
      default:
        return type;
    }
  }
}
