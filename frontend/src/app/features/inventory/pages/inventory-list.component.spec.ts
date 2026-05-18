import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { InventoryListComponent } from './inventory-list.component';
import { InventoryApiService } from '../services/inventory-api.service';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse, HttpResponse, HttpHeaders } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

describe('InventoryListComponent', () => {
  let component: InventoryListComponent;
  let fixture: ComponentFixture<InventoryListComponent>;
  let inventoryApiSpy: jasmine.SpyObj<InventoryApiService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('InventoryApiService', ['getInventoryList', 'getInventoryDetails', 'exportInventory']);

    await TestBed.configureTestingModule({
      imports: [InventoryListComponent, FormsModule],
      providers: [
        { provide: InventoryApiService, useValue: spy }
      ]
    }).compileComponents();

    inventoryApiSpy = TestBed.inject(InventoryApiService) as jasmine.SpyObj<InventoryApiService>;
  });

  beforeEach(() => {
    inventoryApiSpy.getInventoryList.and.returnValue(of({
      totalCount: 0,
      page: 1,
      pageSize: 20,
      items: []
    }));

    fixture = TestBed.createComponent(InventoryListComponent);
    component = fixture.componentInstance;
  });

  it('should load list on init', () => {
    fixture.detectChanges(); // calls ngOnInit -> loadInventory
    expect(inventoryApiSpy.getInventoryList).toHaveBeenCalled();
    expect(component.items.length).toBe(0);
    expect(component.isLoading).toBeFalse();
  });

  it('should display empty state if items length is 0', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('لا توجد منتجات تطابق معايير البحث');
  });

  it('should load details when openDetails is called', () => {
    inventoryApiSpy.getInventoryDetails.and.returnValue(of({
      productId: 1,
      productName: 'Test Product',
      barcode: '123',
      categoryId: 1,
      categoryName: 'Cat',
      baseUnit: 'Unit',
      priceUsd: 10,
      hasCarton: false,
      hasExpiry: false,
      isActive: true,
      totalQuantityAvailable: 100,
      stockStatus: 'InStock',
      batches: []
    }));

    fixture.detectChanges();
    component.openDetails(1);

    expect(inventoryApiSpy.getInventoryDetails).toHaveBeenCalledWith(1);
    expect(component.selectedProductId).toBe(1);
    expect(component.productDetails?.productName).toBe('Test Product');
    expect(component.detailsLoading).toBeFalse();
  });

  it('should handle error when details fetch fails with 404', () => {
    inventoryApiSpy.getInventoryDetails.and.returnValue(throwError(() => ({ status: 404 })));

    fixture.detectChanges();
    component.openDetails(999);

    expect(component.detailsError).toContain('غير موجود');
    expect(component.detailsLoading).toBeFalse();
  });

  it('should update search term and trigger reload', fakeAsync(() => {
    fixture.detectChanges();
    inventoryApiSpy.getInventoryList.calls.reset();

    component.onSearchChange('new term');
    tick(500); // wait for debounceTime(400)

    expect(inventoryApiSpy.getInventoryList).toHaveBeenCalled();
    expect(component.page).toBe(1);
  }));

  it('should export inventory with filters', () => {
    fixture.detectChanges();
    component.searchQuery = 'test';
    component.filterHasStock = true;
    
    const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const mockResponse = new HttpResponse({ body: mockBlob, headers: new HttpHeaders({'Content-Disposition': 'attachment; filename="inventory_export.xlsx"'}) });
    
    inventoryApiSpy.exportInventory.and.returnValue(of(mockResponse));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
    spyOn(window.URL, 'revokeObjectURL');

    component.exportInventory();

    expect(inventoryApiSpy.exportInventory).toHaveBeenCalledWith(jasmine.objectContaining({ format: 'excel', search: 'test', hasStock: true }));
    expect(window.URL.createObjectURL).toHaveBeenCalled();
  });
});
