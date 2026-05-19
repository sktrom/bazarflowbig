import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SupplierListItem } from '../../suppliers/models/supplier.model';
import { SuppliersApiService } from '../../suppliers/services/suppliers-api.service';
import {
  PurchaseInvoiceDetailResponse,
  PurchaseInvoiceListItem,
  PurchaseProductLookupItem
} from '../models/purchase-invoice.model';
import { PurchaseInvoicesApiService } from '../services/purchase-invoices-api.service';
import { PurchasesComponent } from './purchases.component';

describe('PurchasesComponent', () => {
  let component: PurchasesComponent;
  let fixture: ComponentFixture<PurchasesComponent>;
  let apiSpy: jasmine.SpyObj<PurchaseInvoicesApiService>;
  let suppliersApiSpy: jasmine.SpyObj<SuppliersApiService>;

  const invoices: PurchaseInvoiceListItem[] = [
    {
      id: 1,
      invoiceNumber: 'PI-20260519-000001',
      supplierId: 10,
      supplierName: 'Alpha Supplier',
      status: 'Draft',
      externalInvoiceNumber: 'EXT-1',
      subtotalUsd: 12,
      totalUsd: 12,
      createdAt: '2026-05-19T10:00:00Z',
      updatedAt: '2026-05-19T10:00:00Z'
    },
    {
      id: 2,
      invoiceNumber: 'PI-20260518-000001',
      supplierId: 11,
      supplierName: 'Beta Supplier',
      status: 'Completed',
      externalInvoiceNumber: null,
      subtotalUsd: 5,
      totalUsd: 5,
      createdAt: '2026-05-18T10:00:00Z',
      updatedAt: '2026-05-18T10:00:00Z'
    }
  ];

  const suppliers: SupplierListItem[] = [
    { id: 10, name: 'Alpha Supplier', phone: null, email: null, isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' },
    { id: 11, name: 'Beta Supplier', phone: null, email: null, isActive: false, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' }
  ];

  const detail: PurchaseInvoiceDetailResponse = {
    ...invoices[0],
    createdByEmployeeId: 4,
    createdByEmployeeName: 'Cashier One',
    notes: 'Header notes',
    lines: [
      {
        id: 100,
        productId: 200,
        productName: 'Milk',
        barcode: '123',
        quantity: 2,
        unitCostUsd: 3,
        lineTotalUsd: 6,
        expiryDate: '2026-12-31T00:00:00Z',
        notes: null,
        sortOrder: 1
      }
    ]
  };

  const lookupProduct: PurchaseProductLookupItem = {
    productId: 200,
    name: 'Milk',
    barcode: '123',
    priceUsd: 4,
    hasExpiry: true,
    baseUnit: 'pcs'
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('PurchaseInvoicesApiService', [
      'getAll',
      'getById',
      'create',
      'update',
      'delete',
      'addLine',
      'updateLine',
      'deleteLine',
      'productsLookup'
    ]);
    suppliersApiSpy = jasmine.createSpyObj('SuppliersApiService', ['getSuppliers']);

    apiSpy.getAll.and.returnValue(of({ items: invoices }));
    apiSpy.getById.and.returnValue(of(detail));
    apiSpy.productsLookup.and.returnValue(of({ items: [] }));
    suppliersApiSpy.getSuppliers.and.returnValue(of({ items: suppliers }));

    await TestBed.configureTestingModule({
      imports: [PurchasesComponent],
      providers: [
        { provide: PurchaseInvoicesApiService, useValue: apiSpy },
        { provide: SuppliersApiService, useValue: suppliersApiSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PurchasesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load purchase invoices on init', () => {
    expect(apiSpy.getAll).toHaveBeenCalled();
    expect(component.invoices.length).toBe(2);
    expect(component.filteredInvoices.length).toBe(2);
  });

  it('should render purchase invoice list', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('PI-20260519-000001');
    expect(text).toContain('Alpha Supplier');
  });

  it('should filter locally by supplier, status, date, and search', () => {
    component.filterSupplierId = 10;
    component.applyFilters();
    expect(component.filteredInvoices.length).toBe(1);
    expect(component.filteredInvoices[0].id).toBe(1);

    component.filterSupplierId = null;
    component.filterStatus = 'Completed';
    component.applyFilters();
    expect(component.filteredInvoices.length).toBe(1);
    expect(component.filteredInvoices[0].id).toBe(2);

    component.filterStatus = null;
    component.dateFrom = '2026-05-19';
    component.applyFilters();
    expect(component.filteredInvoices.length).toBe(1);
    expect(component.filteredInvoices[0].id).toBe(1);

    component.dateFrom = '';
    component.searchTerm = 'ext-1';
    component.applyFilters();
    expect(component.filteredInvoices.length).toBe(1);
    expect(component.filteredInvoices[0].id).toBe(1);
  });

  it('should create draft purchase invoice', () => {
    apiSpy.create.and.returnValue(of(detail));
    apiSpy.getAll.calls.reset();
    component.openCreateModal();
    component.formData = { supplierId: 10, externalInvoiceNumber: ' EXT-1 ', notes: ' Notes ' };

    component.saveInvoice();

    expect(apiSpy.create).toHaveBeenCalledWith({
      supplierId: 10,
      externalInvoiceNumber: 'EXT-1',
      notes: 'Notes'
    });
    expect(component.activeModal).toBe('details');
    expect(component.selectedInvoice?.id).toBe(1);
    expect(apiSpy.getAll).toHaveBeenCalled();
  });

  it('should edit draft purchase invoice', () => {
    apiSpy.getById.and.returnValue(of(detail));
    apiSpy.update.and.returnValue(of({ ...detail, notes: 'Updated' }));
    component.openEditModal(invoices[0]);
    component.formData.notes = 'Updated';

    component.saveInvoice();

    expect(apiSpy.getById).toHaveBeenCalledWith(1);
    expect(apiSpy.update).toHaveBeenCalledWith(1, jasmine.objectContaining({ notes: 'Updated' }));
  });

  it('should delete draft purchase invoice', () => {
    apiSpy.delete.and.returnValue(of({ success: true, action: 'DELETED', message: 'Deleted' }));
    apiSpy.getAll.calls.reset();

    component.openDeleteModal(invoices[0]);
    component.confirmDelete();

    expect(apiSpy.delete).toHaveBeenCalledWith(1);
    expect(component.toast).toBe('تم حذف فاتورة الشراء');
    expect(apiSpy.getAll).toHaveBeenCalled();
  });

  it('should open details and render lines and totals', () => {
    apiSpy.getById.and.returnValue(of(detail));

    component.openDetails(invoices[0]);
    fixture.detectChanges();

    expect(component.selectedInvoice?.lines.length).toBe(1);
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Milk');
    expect(text).toContain('Total USD');
  });

  it('should lookup products and render results', fakeAsync(() => {
    apiSpy.productsLookup.and.returnValue(of({ items: [lookupProduct] }));
    component.selectedInvoice = detail;
    component.openAddLineModal();
    component.productSearch = 'milk';

    component.onProductSearchChange('milk');
    tick(300);
    fixture.detectChanges();

    expect(apiSpy.productsLookup).toHaveBeenCalledWith('milk');
    expect(component.lookupItems.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Milk');
  }));

  it('should add line and refresh invoice detail state from backend response', () => {
    apiSpy.addLine.and.returnValue(of(detail));
    apiSpy.getAll.calls.reset();
    component.selectedInvoice = detail;
    component.openAddLineModal();
    component.selectProduct(lookupProduct);
    component.lineForm = { productId: 200, quantity: 2, unitCostUsd: 3, expiryDate: '2026-12-31', notes: null };

    component.saveLine();

    expect(apiSpy.addLine).toHaveBeenCalledWith(1, jasmine.objectContaining({
      productId: 200,
      quantity: 2,
      unitCostUsd: 3,
      expiryDate: '2026-12-31'
    }));
    expect(component.activeModal).toBe('details');
    expect(apiSpy.getAll).toHaveBeenCalled();
  });

  it('should edit line and refresh detail state', () => {
    apiSpy.updateLine.and.returnValue(of(detail));
    component.selectedInvoice = detail;
    component.openEditLineModal(detail.lines[0]);
    component.lineForm.quantity = 4;

    component.saveLine();

    expect(apiSpy.updateLine).toHaveBeenCalledWith(1, 100, jasmine.objectContaining({
      quantity: 4,
      unitCostUsd: 3
    }));
  });

  it('should delete line and refresh detail state', () => {
    apiSpy.deleteLine.and.returnValue(of({ success: true, message: 'Deleted' }));
    apiSpy.getById.and.returnValue(of({ ...detail, lines: [] }));
    component.selectedInvoice = detail;

    component.deleteLine(detail.lines[0]);

    expect(apiSpy.deleteLine).toHaveBeenCalledWith(1, 100);
    expect(apiSpy.getById).toHaveBeenCalledWith(1);
    expect(component.selectedInvoice?.lines.length).toBe(0);
  });

  it('should block invalid line quantity, unit cost, and missing expiry', () => {
    component.selectedInvoice = detail;
    component.openAddLineModal();
    component.selectProduct(lookupProduct);

    component.lineForm = { productId: 200, quantity: 0, unitCostUsd: 1, expiryDate: '2026-12-31', notes: null };
    component.saveLine();
    expect(component.lineErr).toBe('الكمية يجب أن تكون أكبر من صفر');

    component.lineForm = { productId: 200, quantity: 1, unitCostUsd: -1, expiryDate: '2026-12-31', notes: null };
    component.saveLine();
    expect(component.lineErr).toBe('تكلفة الوحدة غير صالحة');

    component.lineForm = { productId: 200, quantity: 1, unitCostUsd: 1, expiryDate: null, notes: null };
    component.saveLine();
    expect(component.lineErr).toBe('تاريخ الصلاحية مطلوب لهذا المنتج');
    expect(apiSpy.addLine).not.toHaveBeenCalled();
  });

  it('should render non-draft invoices as read-only', () => {
    component.selectedInvoice = { ...detail, status: 'Completed' };
    component.activeModal = 'details';
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('هذه الفاتورة غير قابلة للتعديل');
    expect(text).not.toContain('إضافة خط');
  });

  it('should map backend error codes to Arabic messages', () => {
    const error = new HttpErrorResponse({ status: 400, error: { error: 'EXPIRY_DATE_REQUIRED' } });
    apiSpy.addLine.and.returnValue(throwError(() => error));
    component.selectedInvoice = detail;
    component.openAddLineModal();
    component.selectProduct(lookupProduct);
    component.lineForm = { productId: 200, quantity: 1, unitCostUsd: 1, expiryDate: '2026-12-31', notes: null };

    component.saveLine();

    expect(component.lineErr).toBe('تاريخ الصلاحية مطلوب لهذا المنتج');
  });
});
