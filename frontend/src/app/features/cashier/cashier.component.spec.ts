import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { CashierComponent } from './cashier.component';
import { CashierStateService } from './services/cashier-state.service';
import { CashierApiService, CartResponse, ProductDto } from './services/cashier-api.service';
import { InvoicesApiService, InvoiceDetailsResponse } from '../invoices/services/invoices-api.service';
import { By } from '@angular/platform-browser';

describe('CashierComponent & CashierState', () => {
  let fixture: ComponentFixture<CashierComponent>;
  let component: CashierComponent;
  let apiSpy: jasmine.SpyObj<CashierApiService>;
  let invoicesApiSpy: jasmine.SpyObj<InvoicesApiService>;
  let stateService: CashierStateService;

  const mockProducts: ProductDto[] = [
    { id: 1, name: 'Prod A', barcode: '111', priceUsd: 10, categoryId: 1, categoryName: 'Cat1', isActive: true }
  ];

  const emptyCart: CartResponse = { status: 'Working', subtotalUsd: 0, totalUsd: 0, lines: [] };
  
  const activeCart: CartResponse = {
    status: 'Working',
    subtotalUsd: 10,
    totalUsd: 10,
    lines: [
      { lineId: 101, productId: 1, productName: 'Prod A', quantity: 1, unitPriceUsdOriginal: 10, lineTotalUsdOriginal: 10, lineTotalUsdEffective: 10, isPriceOverridden: false }
    ]
  };

  const receiptDetails: InvoiceDetailsResponse = {
    invoiceId: 77,
    invoiceNumber: 'INV-77',
    status: 'Completed',
    customerName: 'Ahmad',
    originalEmployeeId: 1,
    employeeName: 'Cashier One',
    subtotalUsd: 10,
    totalUsd: 10,
    exchangeRateSypSnapshot: 10000,
    totalSyp: 100000,
    hasManualPriceEdit: false,
    hasAdjustmentRequest: false,
    createdAt: '2026-05-18T10:00:00Z',
    completedAt: '2026-05-18T10:01:00Z',
    lines: [
      { lineId: 101, productId: 1, productName: 'Prod A', quantity: 1, unitPriceUsdOriginal: 10, lineTotalUsdOriginal: 10, lineTotalUsdEffective: 10, isPriceOverridden: false, sortOrder: 1 }
    ]
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('CashierApiService', [
      'getCashierProducts', 'getCurrentCart', 'addByProduct', 'updateLine', 'deleteLine',
      'suspendCart', 'completeCart', 'cancelCart', 'addByBarcode'
    ]);
    invoicesApiSpy = jasmine.createSpyObj('InvoicesApiService', ['getInvoiceDetails']);

    apiSpy.getCashierProducts.and.returnValue(of(mockProducts));
    apiSpy.getCurrentCart.and.returnValue(of(activeCart));

    await TestBed.configureTestingModule({
      imports: [CashierComponent, HttpClientTestingModule],
      providers: [
        { provide: CashierApiService, useValue: apiSpy },
        { provide: InvoicesApiService, useValue: invoicesApiSpy },
        CashierStateService
      ]
    }).compileComponents();

    stateService = TestBed.inject(CashierStateService);
    fixture = TestBed.createComponent(CashierComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // calls ngOnInit -> loadInitialState
  });

  // --- Initial Load ---
  it('products pane loads cashier products source', () => {
    expect(apiSpy.getCashierProducts).toHaveBeenCalled();
    expect(component.state.products).toEqual(mockProducts);
  });

  it('should focus barcode input on init', async () => {
    await new Promise(resolve => setTimeout(resolve, 0));
    const input = fixture.debugElement.query(By.css('[data-testid="barcode-input"]')).nativeElement as HTMLInputElement;
    expect(document.activeElement).toBe(input);
  });

  it('should add product immediately on Enter', () => {
    apiSpy.addByBarcode.and.returnValue(of(activeCart));
    component.barcodeInput = ' 111 ';
    fixture.detectChanges();

    const input = fixture.debugElement.query(By.css('[data-testid="barcode-input"]'));
    input.triggerEventHandler('keyup.enter', new KeyboardEvent('keyup', { key: 'Enter' }));

    expect(apiSpy.addByBarcode).toHaveBeenCalledWith('111');
  });

  it('should not require clicking Add after scan', () => {
    apiSpy.addByBarcode.and.returnValue(of(activeCart));
    component.barcodeInput = '111';
    fixture.detectChanges();
    const addButton = fixture.debugElement.query(By.css('[data-testid="barcode-add-button"]')).nativeElement as HTMLButtonElement;
    const clickSpy = spyOn(addButton, 'click').and.callThrough();

    const input = fixture.debugElement.query(By.css('[data-testid="barcode-input"]'));
    input.triggerEventHandler('keyup.enter', new KeyboardEvent('keyup', { key: 'Enter' }));

    expect(apiSpy.addByBarcode).toHaveBeenCalledWith('111');
    expect(clickSpy).not.toHaveBeenCalled();
  });

  it('should not submit empty barcode', () => {
    component.barcodeInput = '   ';
    component.submitBarcode();

    expect(apiSpy.addByBarcode).not.toHaveBeenCalled();
  });

  it('should clear input after success', () => {
    apiSpy.addByBarcode.and.returnValue(of(activeCart));
    component.barcodeInput = '111';

    component.submitBarcode();

    expect(component.barcodeInput).toBe('');
    expect(component.barcodeSuccessMessage).toBe('تمت إضافة المنتج');
  });

  it('should restore focus after success', fakeAsync(() => {
    apiSpy.addByBarcode.and.returnValue(of(activeCart));
    component.barcodeInput = '111';
    const input = fixture.debugElement.query(By.css('[data-testid="barcode-input"]')).nativeElement as HTMLInputElement;

    component.submitBarcode();
    tick(0);

    expect(document.activeElement).toBe(input);
    tick(1500);
  }));

  it('should show PRODUCT_NOT_FOUND message', fakeAsync(() => {
    const err = new HttpErrorResponse({ status: 404, error: { error: 'PRODUCT_NOT_FOUND' } });
    apiSpy.addByBarcode.and.returnValue(throwError(() => err));
    component.barcodeInput = 'BAD-CODE';
    const input = fixture.debugElement.query(By.css('[data-testid="barcode-input"]')).nativeElement as HTMLInputElement;

    component.submitBarcode();
    tick(0);

    expect(component.barcodeInput).toBe('');
    expect(component.barcodeProcessing).toBeFalse();
    expect(component.state.error).toBe('لم يتم العثور على منتج بهذا الباركود');
    expect(document.activeElement).toBe(input);
  }));

  // --- Add Product ---
  it('should keep product grid click flow working', () => {
    apiSpy.addByProduct.and.returnValue(of(activeCart));
    component.onProductSelected(1);
    expect(apiSpy.addByProduct).toHaveBeenCalledWith(1);
  });

  // --- Invoice Pane Tests (via Component or State directly) ---
  it('double click enters inline edit and blur saves edit', () => {
    // Assuming UI event binds to onUpdateLine correctly (simulated here)
    apiSpy.updateLine.and.returnValue(of(activeCart));
    component.onUpdateLine({ lineId: 101, quantity: 2 });
    expect(apiSpy.updateLine).toHaveBeenCalledWith(101, 2, undefined);
  });

  it('quantity 0 blocked (handled in invoice-pane component logic)', () => {
    // invoice-pane component handles quantity 0 locally by returning early.
    // If we test state service:
    // This is essentially validating the UI logic in invoice-pane, let's test it by making sure state doesn't get called.
    const paneInstance = fixture.debugElement.query(By.css('app-invoice-pane')).componentInstance;
    spyOn(paneInstance.updateLine, 'emit');
    paneInstance.editingLineId = 101;
    paneInstance.editData = { quantity: 0, overrideTotal: null };
    paneInstance.onBlurLine(activeCart.lines[0]);
    expect(paneInstance.updateLine.emit).not.toHaveBeenCalled();
  });

  it('override removed when quantity changes', () => {
    const paneInstance = fixture.debugElement.query(By.css('app-invoice-pane')).componentInstance;
    spyOn(paneInstance.updateLine, 'emit');
    const overriddenLine = { ...activeCart.lines[0], isPriceOverridden: true, quantity: 1, lineTotalUsdEffective: 15 };
    paneInstance.editingLineId = 101;
    paneInstance.editData = { quantity: 2, overrideTotal: 15 }; // Changed quantity, kept override total
    paneInstance.onBlurLine(overriddenLine);
    // Because qty changed, override should be removed (null)
    expect(paneInstance.updateLine.emit).toHaveBeenCalledWith({ lineId: 101, quantity: 2, overrideLineTotalUsd: null });
  });

  it('direct line delete works', () => {
    apiSpy.deleteLine.and.returnValue(of(emptyCart));
    component.onDeleteLine(101);
    expect(apiSpy.deleteLine).toHaveBeenCalledWith(101);
  });

  it('deleting last line resets empty state', () => {
    const err404 = new HttpErrorResponse({ status: 404, error: { error: 'NO_WORKING_CART_EXISTS' } });
    apiSpy.deleteLine.and.returnValue(throwError(() => err404));
    component.onDeleteLine(101); // Deleting the only line
    expect(component.state.cart?.lines.length).toBe(0);
  });

  // --- Modals ---
  it('suspend modal uses suspensionReason', () => {
    apiSpy.suspendCart.and.returnValue(of(emptyCart));
    component.openSuspendModal();
    component.modalData.suspensionReason = 'Hold';
    component.confirmSuspend();
    expect(apiSpy.suspendCart).toHaveBeenCalledWith('Hold');
  });

  it('CUSTOMER_NAME_REQUIRED handled clearly', () => {
    const err = new HttpErrorResponse({ status: 400, error: { error: 'CUSTOMER_NAME_REQUIRED' } });
    apiSpy.suspendCart.and.returnValue(throwError(() => err));
    component.openSuspendModal();
    component.modalData.suspensionReason = 'Hold';
    component.confirmSuspend();
    expect(component.state.error).toContain('اسم العميل مطلوب');
  });

  it('complete confirmation works', () => {
    apiSpy.completeCart.and.returnValue(of(emptyCart));
    component.openCompleteModal();
    component.confirmComplete();
    expect(apiSpy.completeCart).toHaveBeenCalled();
    expect(component.state.cart?.lines.length).toBe(0); // empty/new cart
    expect(component.activeModal).toBeNull();
    expect(invoicesApiSpy.getInvoiceDetails).not.toHaveBeenCalled();
  });

  it('completion with invoiceId fetches receipt details and opens print preview', () => {
    const completedCart: CartResponse = { ...emptyCart, invoiceId: 77, status: 'Completed' };
    apiSpy.completeCart.and.returnValue(of(completedCart));
    invoicesApiSpy.getInvoiceDetails.and.returnValue(of(receiptDetails));

    component.openCompleteModal();
    component.confirmComplete();

    expect(invoicesApiSpy.getInvoiceDetails).toHaveBeenCalledWith(77);
    expect(component.activeModal).toBe('receipt');
    expect(component.completedInvoice?.invoiceId).toBe(77);
    expect(component.isReceiptLoading).toBeFalse();
  });

  it('receipt details fetch failure keeps completed sale state and shows non-blocking error', () => {
    const completedCart: CartResponse = { ...emptyCart, invoiceId: 77, status: 'Completed' };
    const err403 = new HttpErrorResponse({ status: 403, error: { error: 'UNAUTHORIZED_SCREEN_ACCESS' } });
    apiSpy.completeCart.and.returnValue(of(completedCart));
    invoicesApiSpy.getInvoiceDetails.and.returnValue(throwError(() => err403));

    component.openCompleteModal();
    component.confirmComplete();

    expect(component.state.cart?.lines.length).toBe(0);
    expect(component.activeModal).toBe('receipt');
    expect(component.completedInvoice).toBeNull();
    expect(component.receiptError).toContain('صلاحية');
  });

  it('cancel confirmation works', () => {
    apiSpy.cancelCart.and.returnValue(of(emptyCart));
    component.openCancelModal();
    component.confirmCancel();
    expect(apiSpy.cancelCart).toHaveBeenCalled();
  });

  // --- Layout basic behavior ---
  it('desktop/mobile layout behavior basic coverage (activeTab)', () => {
    expect(component.activeTab).toBe('invoice');
    component.activeTab = 'products';
    fixture.detectChanges();
    // left pane translates based on activeTab on mobile
    const leftPane = fixture.debugElement.query(By.css('div.w-full.md\\:w-\\[60\\%\\]'));
    expect(leftPane.classes['-translate-x-full']).toBeFalsy();
  });
});
