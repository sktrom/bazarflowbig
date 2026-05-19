import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { SuppliersComponent } from './suppliers.component';
import { SuppliersApiService } from '../services/suppliers-api.service';
import { SupplierDetailResponse, SupplierListItem } from '../models/supplier.model';

describe('SuppliersComponent', () => {
  let component: SuppliersComponent;
  let fixture: ComponentFixture<SuppliersComponent>;
  let apiSpy: jasmine.SpyObj<SuppliersApiService>;

  const suppliers: SupplierListItem[] = [
    { id: 1, name: 'Alpha Supplier', phone: '111', email: 'alpha@test.com', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-02T00:00:00Z' },
    { id: 2, name: 'Beta Supplier', phone: '222', email: null, isActive: false, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-03T00:00:00Z' }
  ];

  const supplierDetail: SupplierDetailResponse = {
    id: 1,
    name: 'Alpha Supplier',
    phone: '111',
    email: 'alpha@test.com',
    address: 'Address',
    notes: 'Notes',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z'
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('SuppliersApiService', [
      'getSuppliers',
      'getSupplier',
      'createSupplier',
      'updateSupplier',
      'deleteSupplier'
    ]);

    apiSpy.getSuppliers.and.returnValue(of({ items: suppliers }));

    await TestBed.configureTestingModule({
      imports: [SuppliersComponent],
      providers: [{ provide: SuppliersApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(SuppliersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load suppliers on init', () => {
    expect(apiSpy.getSuppliers).toHaveBeenCalled();
    expect(component.suppliers.length).toBe(2);
    expect(component.filteredSuppliers.length).toBe(2);
  });

  it('should render suppliers table', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Alpha Supplier');
    expect(text).toContain('Beta Supplier');
  });

  it('should show empty state when there are no suppliers', () => {
    apiSpy.getSuppliers.and.returnValue(of({ items: [] }));
    component.loadSuppliers();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('لا توجد موردون');
  });

  it('should filter suppliers by search and status', () => {
    component.searchTerm = 'alpha';
    component.applyFilters();
    expect(component.filteredSuppliers.length).toBe(1);
    expect(component.filteredSuppliers[0].id).toBe(1);

    component.searchTerm = '';
    component.filterStatus = 'inactive';
    component.applyFilters();
    expect(component.filteredSuppliers.length).toBe(1);
    expect(component.filteredSuppliers[0].id).toBe(2);
  });

  it('should create supplier from modal', () => {
    apiSpy.createSupplier.and.returnValue(of(supplierDetail));
    apiSpy.getSuppliers.calls.reset();
    component.openCreateModal();
    component.formData = { name: ' New Supplier ', phone: ' 333 ', email: null, address: null, notes: null, isActive: true };

    component.saveSupplier();

    expect(apiSpy.createSupplier).toHaveBeenCalledWith(jasmine.objectContaining({
      name: 'New Supplier',
      phone: '333'
    }));
    expect(component.activeModal).toBeNull();
    expect(apiSpy.getSuppliers).toHaveBeenCalled();
  });

  it('should open edit modal by loading supplier details', () => {
    apiSpy.getSupplier.and.returnValue(of(supplierDetail));

    component.openEditModal(suppliers[0]);

    expect(apiSpy.getSupplier).toHaveBeenCalledWith(1);
    expect(component.activeModal).toBe('form');
    expect(component.editId).toBe(1);
    expect(component.formData.address).toBe('Address');
  });

  it('should update supplier from edit modal', () => {
    apiSpy.updateSupplier.and.returnValue(of({ ...supplierDetail, name: 'Updated Supplier' }));
    apiSpy.getSuppliers.calls.reset();
    component.editId = 1;
    component.activeModal = 'form';
    component.formData = { name: 'Updated Supplier', phone: null, email: null, address: null, notes: null, isActive: false };

    component.saveSupplier();

    expect(apiSpy.updateSupplier).toHaveBeenCalledWith(1, jasmine.objectContaining({
      name: 'Updated Supplier',
      isActive: false
    }));
    expect(apiSpy.getSuppliers).toHaveBeenCalled();
  });

  it('should require supplier name before saving', () => {
    component.openCreateModal();
    component.formData.name = '   ';

    component.saveSupplier();

    expect(component.formErr).toBe('اسم المورد مطلوب');
    expect(apiSpy.createSupplier).not.toHaveBeenCalled();
  });

  it('should map duplicate name error clearly', () => {
    const error = new HttpErrorResponse({ status: 409, error: { error: 'SUPPLIER_NAME_ALREADY_EXISTS' } });
    apiSpy.createSupplier.and.returnValue(throwError(() => error));
    component.openCreateModal();
    component.formData.name = 'Existing';

    component.saveSupplier();

    expect(component.formErr).toBe('اسم المورد موجود مسبقًا');
    expect(component.activeModal).toBe('form');
  });

  it('should delete supplier after confirmation', () => {
    apiSpy.deleteSupplier.and.returnValue(of({ success: true, action: 'DELETED', message: 'Deleted' }));
    apiSpy.getSuppliers.calls.reset();
    component.openDeleteModal(suppliers[0]);

    component.confirmDelete();

    expect(apiSpy.deleteSupplier).toHaveBeenCalledWith(1);
    expect(component.toast).toBe('تم حذف المورد');
    expect(apiSpy.getSuppliers).toHaveBeenCalled();
  });

  it('should show deactivated message when delete deactivates used supplier', () => {
    apiSpy.deleteSupplier.and.returnValue(of({ success: true, action: 'DEACTIVATED', message: 'Deactivated' }));
    component.openDeleteModal(suppliers[0]);

    component.confirmDelete();

    expect(component.toast).toBe('تم تعطيل المورد لأنه مستخدم');
  });
});
