import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReceiptPrintComponent, ReceiptPrintInvoice } from './receipt-print.component';

describe('ReceiptPrintComponent', () => {
  let fixture: ComponentFixture<ReceiptPrintComponent>;
  let component: ReceiptPrintComponent;

  const invoice: ReceiptPrintInvoice = {
    invoiceId: 12,
    invoiceNumber: 'INV-00012',
    status: 'Completed',
    customerName: 'Ahmad',
    originalEmployeeId: 3,
    employeeName: 'Cashier One',
    invoiceDiscountType: 'Amount',
    invoiceDiscountValue: 3.5,
    subtotalUsd: 18.5,
    totalUsd: 15,
    exchangeRateSypSnapshot: 10000,
    totalSyp: 150000,
    createdAt: '2026-05-18T10:00:00Z',
    completedAt: '2026-05-18T10:05:00Z',
    lines: [
      {
        lineId: 2,
        productId: 20,
        productName: 'Long Product Name',
        quantity: 2,
        unitPriceUsdOriginal: 10,
        lineTotalUsdOriginal: 20,
        lineTotalUsdEffective: 18.5,
        isPriceOverridden: false,
        sortOrder: 2
      },
      {
        lineId: 1,
        productId: 10,
        productName: 'First Product',
        quantity: 1,
        unitPriceUsdOriginal: 5,
        lineTotalUsdOriginal: 5,
        lineTotalUsdEffective: 5,
        isPriceOverridden: true,
        sortOrder: 1
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReceiptPrintComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ReceiptPrintComponent);
    component = fixture.componentInstance;
  });

  it('renders receipt metadata, lines, totals, and derived discounts', () => {
    component.invoice = invoice;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;

    expect(text).toContain('INV-00012');
    expect(text).toContain('Ahmad');
    expect(text).toContain('Cashier One');
    expect(text).toContain('First Product');
    expect(text).toContain('Long Product Name');
    expect(text).toContain('1.50 $');
    expect(text).toContain('3.50 $');
    expect(text).toContain('15.00 $');
    expect(text).toContain('10,000');
    expect(text).toMatch(/150,000\s+ل\.س/);
    expect(text.indexOf('First Product')).toBeLessThan(text.indexOf('Long Product Name'));
  });

  it('renders fallbacks when customer, employee, SYP total, and exchange rate are unavailable', () => {
    component.invoice = {
      ...invoice,
      customerName: undefined,
      employeeName: undefined,
      originalEmployeeId: 7,
      exchangeRateSypSnapshot: null,
      totalSyp: null,
      lines: []
    };
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    const missingValueCount = (text.match(/غير متوفر/g) || []).length;

    expect(text).toContain('زبون نقدي');
    expect(text).toContain('#7');
    expect(missingValueCount).toBeGreaterThanOrEqual(2);
  });
});
