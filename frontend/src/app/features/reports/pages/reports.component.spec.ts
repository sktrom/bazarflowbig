import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReportsComponent } from './reports.component';
import { ReportsApiService } from '../services/reports-api.service';
import { of, throwError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

describe('ReportsComponent', () => {
  let component: ReportsComponent;
  let fixture: ComponentFixture<ReportsComponent>;
  let apiSpy: jasmine.SpyObj<ReportsApiService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ReportsApiService', [
      'getSalesInvoices', 'getSalesItems', 'getSalesCharts',
      'getProductsSummary', 'getProductsMovements', 'getProductsCharts',
      'getEmployeesSummary', 'getEmployeesActivity', 'getEmployeesCharts',
      'getInventorySummary', 'getInventoryBatches', 'getInventoryCharts',
      'getProfitSales', 'getProfitProducts', 'getInventoryValuation',
      'getExpirySummary', 'getExpiryBatches', 'getExpiryCharts'
    ]);

    await TestBed.configureTestingModule({
      imports: [ReportsComponent, FormsModule],
      providers: [
        { provide: ReportsApiService, useValue: spy }
      ]
    }).compileComponents();

    apiSpy = TestBed.inject(ReportsApiService) as jasmine.SpyObj<ReportsApiService>;
  });

  beforeEach(() => {
    // Setup default responses for Sales (the default tab)
    apiSpy.getSalesInvoices.and.returnValue(of({ items: [] }));
    apiSpy.getSalesItems.and.returnValue(of({ items: [] }));
    apiSpy.getSalesCharts.and.returnValue(of({ items: [] }));

    fixture = TestBed.createComponent(ReportsComponent);
    component = fixture.componentInstance;
  });

  it('should load Sales tab on init', () => {
    fixture.detectChanges();
    expect(component.activeTab).toBe('Sales');
    expect(apiSpy.getSalesInvoices).toHaveBeenCalled();
    expect(apiSpy.getSalesItems).toHaveBeenCalled();
    expect(apiSpy.getSalesCharts).toHaveBeenCalled();
    expect(component.isLoading).toBeFalse();
  });

  it('should call api with filters on refresh button', () => {
    fixture.detectChanges(); // initial load
    
    component.filters.dateFrom = '2023-01-01';
    component.refreshData(); // explicit refresh
    
    expect(apiSpy.getSalesInvoices).toHaveBeenCalledWith('2023-01-01', null as any, null as any);
  });

  it('should handle API errors gracefully', () => {
    const err = new HttpErrorResponse({ status: 500 });
    apiSpy.getSalesInvoices.and.returnValue(throwError(() => err));
    
    fixture.detectChanges();
    
    expect(component.globalError).toContain('حدث خطأ أثناء تحميل البيانات');
    expect(component.isLoading).toBeFalse();
  });

  it('should load another tab data when switching and destroy old chart', () => {
    apiSpy.getExpirySummary.and.returnValue(of({ items: [] }));
    apiSpy.getExpiryBatches.and.returnValue(of({ items: [] }));
    apiSpy.getExpiryCharts.and.returnValue(of({ items: [] }));

    fixture.detectChanges(); // load sales
    
    // Fake a chart instance
    const destroySpy = jasmine.createSpy('destroy');
    component.chartInstance = { destroy: destroySpy } as any;
    
    // Set some filters to test retention
    component.filters.dateFrom = '2023-01-01';
    component.filters.dateTo = '2023-12-31';
    component.filters.productId = 5;

    component.switchTab('Expiry');
    
    expect(destroySpy).toHaveBeenCalled();
    expect(component.activeTab).toBe('Expiry');
    expect(apiSpy.getExpirySummary).toHaveBeenCalled();
    
    // Dates should be retained, but specific filters should be reset
    expect(component.filters.dateFrom).toBe('2023-01-01');
    expect(component.filters.dateTo).toBe('2023-12-31');
    expect(component.filters.productId).toBeNull();
  });

  it('should display empty state message if no data', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('لا توجد بيانات');
    expect(el.textContent).toContain('لا توجد بيانات كافية للرسم البياني');
  });

  it('should clear chart timeout on destroy', () => {
    spyOn(window, 'clearTimeout');
    component.chartTimeoutId = 12345;
    component.ngOnDestroy();
    expect(window.clearTimeout).toHaveBeenCalledWith(12345);
    expect(component.chartTimeoutId).toBeNull();
  });

  it('should load profit tab data', () => {
    apiSpy.getProfitSales.and.returnValue(of({ items: [] }));
    apiSpy.getProfitProducts.and.returnValue(of({ items: [] }));
    apiSpy.getInventoryValuation.and.returnValue(of({ items: [] }));

    fixture.detectChanges();
    component.switchTab('Profit');

    expect(component.activeTab).toBe('Profit');
    expect(apiSpy.getProfitSales).toHaveBeenCalled();
    expect(apiSpy.getProfitProducts).toHaveBeenCalled();
    expect(apiSpy.getInventoryValuation).toHaveBeenCalled();
  });

  it('should render missing cost warning and margin in profit tab', () => {
    apiSpy.getProfitSales.and.returnValue(of({ items: [{
      invoiceId: 1,
      invoiceNumber: 'INV-1',
      createdAt: '2026-05-20T00:00:00Z',
      revenueUsd: 10,
      knownCostUsd: 6,
      profitUsd: 4,
      marginPercent: 40,
      hasMissingCost: true,
      isProfitComplete: false,
      missingCostQuantity: 1
    }] }));
    apiSpy.getProfitProducts.and.returnValue(of({ items: [{
      productId: 1,
      productName: 'Milk',
      quantitySold: 2,
      revenueUsd: 10,
      knownCostUsd: 6,
      profitUsd: 4,
      marginPercent: 40,
      hasMissingCost: true,
      isProfitComplete: false,
      missingCostQuantity: 1
    }] }));
    apiSpy.getInventoryValuation.and.returnValue(of({ items: [{
      productId: 1,
      productName: 'Milk',
      categoryName: 'Dairy',
      totalQuantityAvailable: 3,
      knownCostQuantity: 2,
      missingCostQuantity: 1,
      knownStockValueUsd: 6,
      hasMissingCost: true,
      isValuationComplete: false
    }] }));

    fixture.detectChanges();
    component.switchTab('Profit');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('بعض النتائج غير محسوبة بالكامل');
    expect(text).toContain('40.0%');
    expect(text).toContain('Milk');
  });

});
