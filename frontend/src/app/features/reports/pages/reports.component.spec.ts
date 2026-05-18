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
    
    expect(apiSpy.getSalesInvoices).toHaveBeenCalledWith('2023-01-01', null, null);
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
    component.chartInstance = { destroy: jasmine.createSpy('destroy') } as any;

    component.switchTab('Expiry');
    
    expect(component.chartInstance?.destroy).toHaveBeenCalled();
    expect(component.activeTab).toBe('Expiry');
    expect(apiSpy.getExpirySummary).toHaveBeenCalled();
  });

  it('should display empty state message if no data', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('لا توجد بيانات');
    expect(el.textContent).toContain('لا توجد بيانات كافية للرسم البياني');
  });

});
