import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OffersComponent } from './offers.component';
import { OffersApiService } from '../services/offers-api.service';
import { of, throwError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse, HttpResponse, HttpHeaders } from '@angular/common/http';

describe('OffersComponent', () => {
  let component: OffersComponent;
  let fixture: ComponentFixture<OffersComponent>;
  let apiSpy: jasmine.SpyObj<OffersApiService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('OffersApiService', ['getAll', 'create', 'update', 'cancel', 'delete', 'exportOffers', 'productsLookup']);

    await TestBed.configureTestingModule({
      imports: [OffersComponent, FormsModule],
      providers: [
        { provide: OffersApiService, useValue: spy }
      ]
    }).compileComponents();

    apiSpy = TestBed.inject(OffersApiService) as jasmine.SpyObj<OffersApiService>;
  });

  beforeEach(() => {
    apiSpy.getAll.and.returnValue(of({ items: [] }));
    apiSpy.productsLookup.and.returnValue(of({ items: [] }));
    fixture = TestBed.createComponent(OffersComponent);
    component = fixture.componentInstance;
  });

  it('should load list on init', () => {
    fixture.detectChanges();
    expect(apiSpy.getAll).toHaveBeenCalled();
    expect(component.isLoading).toBeFalse();
  });

  it('should display empty state if items length is 0', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('لا توجد عروض مطابقة');
  });

  it('should open create modal and save', () => {
    fixture.detectChanges();
    component.openCreateModal();
    expect(component.activeModal).toBe('form');
    expect(component.editId).toBeNull();

    component.formData = { productId: 1, discountType: 'Amount', discountValue: 10 };
    apiSpy.create.and.returnValue(of({ id: 1, productId: 1, productName: 'Test', discountType: 'Amount', discountValue: 10, isActive: true, createdAt: '', updatedAt: '' }));
    
    component.saveOffer();
    
    expect(apiSpy.create).toHaveBeenCalledWith({ productId: 1, discountType: 'Amount', discountValue: 10 });
    expect(component.activeModal).toBeNull();
  });

  it('should handle PRODUCT_NOT_FOUND error on save', () => {
    fixture.detectChanges();
    component.openCreateModal();
    component.formData = { productId: 99, discountType: 'Amount', discountValue: 10 };
    
    const errorResponse = new HttpErrorResponse({ error: { error: 'PRODUCT_NOT_FOUND' }, status: 400 });
    apiSpy.create.and.returnValue(throwError(() => errorResponse));

    component.saveOffer();

    expect(component.formError).toBe('المنتج غير موجود، تأكد من اختيار المنتج الصحيح');
    expect(component.activeModal).toBe('form'); // should not close
  });

  it('should show global error on CANNOT_DELETE_USED_OFFER', () => {
    fixture.detectChanges();
    component.openDeleteModal({ id: 1, productId: 1, productName: 'P', discountType: 'A', discountValue: 5, isActive: true });
    
    const errorResponse = new HttpErrorResponse({ error: { error: 'CANNOT_DELETE_USED_OFFER' }, status: 409 });
    apiSpy.delete.and.returnValue(throwError(() => errorResponse));

    component.confirmDelete();

    expect(component.globalError).toBe('لا يمكن حذف هذا العرض لأنه مستخدم أو قديم، يمكن إلغاؤه فقط');
    expect(component.activeModal).toBeNull(); // modal closes, toast shows
  });

  it('should export offers with filters', () => {
    fixture.detectChanges();
    component.filterStatus = 'active';
    
    const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const mockResponse = new HttpResponse({ body: mockBlob, headers: new HttpHeaders({'Content-Disposition': 'attachment; filename="offers_export.xlsx"'}) });
    
    apiSpy.exportOffers.and.returnValue(of(mockResponse));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
    spyOn(window.URL, 'revokeObjectURL');

    component.exportOffers();

    expect(apiSpy.exportOffers).toHaveBeenCalledWith(jasmine.objectContaining({ format: 'excel', isActive: true }));
    expect(window.URL.createObjectURL).toHaveBeenCalled();
  });

  it('should search products and allow selecting a product in create mode', () => {
    fixture.detectChanges();
    component.openCreateModal();
    
    expect(apiSpy.productsLookup).toHaveBeenCalledWith('');
    
    const mockProducts = [
      { productId: 10, name: 'شوكولاتة', barcode: '12345', priceUsd: 2.5 }
    ];
    apiSpy.productsLookup.and.returnValue(of({ items: mockProducts }));
    
    component.onProductSearch('شوكو');
    component.ngOnInit(); // trigger search subscription or wait
    
    // Simulate direct call since rxjs debounce is async
    component['runProductSearch']('شوكو');
    expect(component.productLookupResults.length).toBe(1);
    expect(component.productLookupResults[0].name).toBe('شوكولاتة');
    
    component.selectProduct(mockProducts[0]);
    expect(component.selectedProduct).toEqual(mockProducts[0]);
    expect(component.formData.productId).toBe(10);
    expect(component.productLookupResults.length).toBe(0);
    
    component.clearSelectedProduct();
    expect(component.selectedProduct).toBeNull();
    expect(component.formData.productId).toBeNull();
  });
});
