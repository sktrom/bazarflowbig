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

      <table class="receipt-lines">
        <thead>
          <tr>
            <th class="product-col">المادة</th>
            <th>الكمية</th>
            <th>السعر</th>
            <th>الخصم</th>
            <th>الإجمالي</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let line of sortedLines(invoice.lines)">
            <td class="product-col">
              <div class="product-name">{{ line.productName }}</div>
              <div *ngIf="line.isPriceOverridden || line.offerId" class="line-note">
                {{ line.isPriceOverridden ? 'سعر معدل' : 'عرض #' + line.offerId }}
              </div>
            </td>
            <td class="num">{{ line.quantity | number:'1.0-3' }}</td>
            <td class="num">{{ line.unitPriceUsdOriginal | number:'1.2-2' }} $</td>
            <td class="num">{{ lineDiscount(line) | number:'1.2-2' }} $</td>
            <td class="num">{{ line.lineTotalUsdEffective | number:'1.2-2' }} $</td>
          </tr>
        </tbody>
      </table>

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
      width: min(100%, 420px);
      margin: 0 auto;
      background: #fff;
      color: #111827;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 16px;
      font-size: 12px;
      line-height: 1.6;
    }

    .receipt-header {
      text-align: center;
      border-bottom: 1px dashed #94a3b8;
      padding-bottom: 10px;
      margin-bottom: 12px;
    }

    .store-name {
      font-size: 18px;
      font-weight: 800;
      letter-spacing: 0;
    }

    .receipt-title {
      font-size: 13px;
      font-weight: 700;
      color: #334155;
    }

    .receipt-tag {
      display: inline-block;
      margin-top: 6px;
      padding: 2px 8px;
      border: 1px solid #cbd5e1;
      border-radius: 999px;
      color: #475569;
      font-size: 11px;
    }

    .receipt-meta,
    .receipt-totals {
      display: grid;
      gap: 6px;
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
      color: #64748b;
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
      width: 100%;
      border-collapse: collapse;
      margin: 14px 0;
      table-layout: fixed;
    }

    .receipt-lines th,
    .receipt-lines td {
      border-bottom: 1px solid #e5e7eb;
      padding: 6px 4px;
      vertical-align: top;
    }

    .receipt-lines th {
      color: #475569;
      font-size: 11px;
      font-weight: 700;
    }

    .product-col {
      width: 34%;
      text-align: right;
    }

    .product-name {
      overflow-wrap: anywhere;
      font-weight: 700;
    }

    .line-note {
      color: #64748b;
      font-size: 10px;
      font-weight: 500;
    }

    .grand-total {
      border-top: 1px dashed #94a3b8;
      padding-top: 6px;
      margin-top: 2px;
      font-size: 13px;
    }

    .receipt-footer {
      margin-top: 14px;
      padding-top: 10px;
      border-top: 1px dashed #94a3b8;
      text-align: center;
      color: #475569;
      font-weight: 600;
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
