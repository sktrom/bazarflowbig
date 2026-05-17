import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ProductsComponent } from './products.component';
import { ProductsStateService } from './services/products-state.service';
import { ProductsApiService, ProductListResponse, CategoryListResponse, ProductDetailResponse, BatchListResponse, BatchItem } from './services/products-api.service';

describe('ProductsComponent & ProductsState', () => {
  let fixture: ComponentFixture<ProductsComponent>;
  let component: ProductsComponent;
  let apiSpy: jasmine.SpyObj<ProductsApiService>;

  const mockCategories: CategoryListResponse = { items: [{ id: 1, name: 'Cat1', isActive: true }] };
  const mockProducts: ProductListResponse = {
    items: [
      { id: 1, name: 'Prod A', barcode: '111', categoryId: 1, categoryName: 'Cat1', priceUsd: 10, isActive: true },
      { id: 2, name: 'Prod B', barcode: '222', categoryId: 2, categoryName: 'Cat2', priceUsd: 20, isActive: false }
    ]
  };

  const mockDetails: ProductDetailResponse = {
    id: 1, name: 'Prod A', barcode: '111', categoryId: 1, baseUnit: 'pcs', priceUsd: 10,
    hasCarton: false, hasExpiry: true, isActive: true, createdAt: '', updatedAt: ''
  };

  const mockBatches: BatchListResponse = {
    items: [
      { id: 101, productId: 1, quantityOriginal: 10, quantityRemaining: 10, unitCostUsd: 5, expiryDate: '2025-12-31', createdAt: '', isDepleted: false }
    ]
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ProductsApiService', [
      'getProducts', 'getCategoriesLookup', 'getProduct', 'createProduct', 'updateProduct', 'deleteProduct',
      'getBatches', 'createBatch', 'exportProducts'
    ]);

    apiSpy.getCategoriesLookup.and.returnValue(of(mockCategories));
    apiSpy.getProducts.and.returnValue(of(mockProducts));

    await TestBed.configureTestingModule({
      imports: [ProductsComponent, HttpClientTestingModule],
      providers: [
        { provide: ProductsApiService, useValue: apiSpy },
        ProductsStateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // calls ngOnInit -> loadInitialData
  });

  it('products list loads correctly', () => {
    expect(apiSpy.getProducts).toHaveBeenCalled();
    expect(apiSpy.getCategoriesLookup).toHaveBeenCalled();
    expect(component.state.products.length).toBe(2);
    expect(component.filteredProducts.length).toBe(2);
  });

  it('search filters current view', () => {
    component.searchTerm = 'Prod A';
    component.applyFilters();
    expect(component.filteredProducts.length).toBe(1);
    expect(component.filteredProducts[0].name).toBe('Prod A');
  });

  it('category filter works', () => {
    component.filterCategory = 1;
    component.applyFilters();
    expect(component.filteredProducts.length).toBe(1);
    expect(component.filteredProducts[0].categoryId).toBe(1);
  });

  it('add product opens modal', () => {
    component.openCreateModal();
    expect(component.activeModal).toBe('form');
    expect(component.editId).toBeNull();
  });

  it('edit product opens populated modal', () => {
    apiSpy.getProduct.and.returnValue(of(mockDetails));
    component.openEditModal(mockProducts.items[0]);
    expect(apiSpy.getProduct).toHaveBeenCalledWith(1);
    expect(component.activeModal).toBe('form');
    expect(component.editId).toBe(1);
    expect(component.formData.name).toBe('Prod A');
  });

  it('duplicate barcode error handled clearly', () => {
    const err = new HttpErrorResponse({ status: 409, error: { error: 'BARCODE_ALREADY_EXISTS' } });
    apiSpy.createProduct.and.returnValue(throwError(() => err));
    
    component.openCreateModal();
    component.formData = { name: 'X', barcode: '111', categoryId: 1, baseUnit: 'x', priceUsd: 1, hasCarton: false, hasExpiry: false };
    component.saveProduct();
    
    expect(component.state.error).toContain('الباركود موجود مسبقاً');
    expect(component.barcodeError).toBeTrue();
  });

  it('details modal loads batches table', () => {
    apiSpy.getProduct.and.returnValue(of(mockDetails));
    apiSpy.getBatches.and.returnValue(of(mockBatches));
    
    component.openDetailsModal(1);
    
    expect(apiSpy.getProduct).toHaveBeenCalledWith(1);
    expect(apiSpy.getBatches).toHaveBeenCalledWith(1);
    expect(component.activeModal).toBe('details');
    expect(component.batchesData.length).toBe(1);
  });

  it('add batch with expiry enforced for expiry products', () => {
    // detailsData has hasExpiry = true
    component.detailsData = mockDetails; 
    component.openAddBatchModal();
    
    component.batchForm = { quantity: 10, unitCostUsd: 5, expiryDate: null };
    expect(component.isValidBatchForm()).toBeFalse(); // Invalid without expiry
    
    component.batchForm.expiryDate = '2025-01-01';
    expect(component.isValidBatchForm()).toBeTrue(); // Valid with expiry
  });

  it('add batch omits expiry for non-expiry products', () => {
    // Modify detailsData to have hasExpiry = false
    component.detailsData = { ...mockDetails, hasExpiry: false };
    component.openAddBatchModal();
    
    component.batchForm = { quantity: 10, unitCostUsd: 5, expiryDate: '2025-01-01' }; // user somehow entered it
    
    const mockBatchItem: BatchItem = { id: 1, productId: 1, quantityOriginal: 10, quantityRemaining: 10, unitCostUsd: 5, createdAt: '', isDepleted: false };
    apiSpy.createBatch.and.returnValue(of(mockBatchItem));
    
    component.saveBatch();
    
    // Ensure the payload sent to API does NOT have expiryDate
    expect(apiSpy.createBatch).toHaveBeenCalledWith(1, jasmine.objectContaining({
      quantity: 10,
      unitCostUsd: 5
    }));
    // Note: jasmine.objectContaining verifies expiryDate is NOT present if we don't include it in expected object
    const callArgs = apiSpy.createBatch.calls.mostRecent().args[1];
    expect((callArgs as any).expiryDate).toBeUndefined();
  });

  it('delete/disable action requires confirmation', () => {
    component.openDeleteModal(mockProducts.items[0]);
    expect(component.activeModal).toBe('delete');
    expect(component.deleteCandidate?.id).toBe(1);
    
    apiSpy.deleteProduct.and.returnValue(of({}));
    component.confirmDelete();
    expect(apiSpy.deleteProduct).toHaveBeenCalledWith(1);
  });

  it('export entry action triggers expected flow hook', () => {
    const mockBlob = new Blob(['test'], { type: 'text/csv' });
    apiSpy.exportProducts.and.returnValue(of(mockBlob));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
    spyOn(window.URL, 'revokeObjectURL');
    
    component.exportProducts();
    expect(apiSpy.exportProducts).toHaveBeenCalled();
    expect(window.URL.createObjectURL).toHaveBeenCalled();
  });
});
