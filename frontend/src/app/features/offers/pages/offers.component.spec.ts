import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OffersComponent } from './offers.component';
import { OffersApiService } from '../services/offers-api.service';
import { of, throwError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

describe('OffersComponent', () => {
  let component: OffersComponent;
  let fixture: ComponentFixture<OffersComponent>;
  let apiSpy: jasmine.SpyObj<OffersApiService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('OffersApiService', ['getAll', 'create', 'update', 'cancel', 'delete']);

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

    expect(component.formError).toBe('المنتج غير موجود، تأكد من رقم المنتج');
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
});
