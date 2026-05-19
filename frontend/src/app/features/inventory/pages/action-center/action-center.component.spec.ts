import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActionCenterComponent } from './action-center.component';
import { ActionCenterApiService } from '../../services/action-center-api.service';
import { of, throwError } from 'rxjs';
import { ActionCenterResponseDto } from '../../models/action-center.model';
import { Router } from '@angular/router';

describe('ActionCenterComponent', () => {
  let component: ActionCenterComponent;
  let fixture: ComponentFixture<ActionCenterComponent>;
  let apiServiceSpy: jasmine.SpyObj<ActionCenterApiService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockData: ActionCenterResponseDto = {
    summary: {
      outOfStockCount: 1,
      lowStockCount: 0,
      expiredBatchesCount: 0,
      expiringSoonBatchesCount: 0,
      inactiveWithStockCount: 0,
      offerCandidatesCount: 0
    },
    topUrgentActions: [
      { type: 'OUT_OF_STOCK', severity: 'HIGH', productId: 1, productName: 'P1', barcode: '123', message: 'Empty', recommendedAction: 'Restock' }
    ],
    outOfStock: [
      { productId: 1, productName: 'P1', barcode: '123', currentStock: 0 }
    ],
    lowStock: [],
    expired: [],
    expiringSoon: [],
    inactiveWithStock: [],
    offerCandidates: []
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ActionCenterApiService', ['getActionCenterSummary']);
    const router = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [ActionCenterComponent],
      providers: [
        { provide: ActionCenterApiService, useValue: spy },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    apiServiceSpy = TestBed.inject(ActionCenterApiService) as jasmine.SpyObj<ActionCenterApiService>;
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should load data successfully and display summary cards', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    
    fixture.detectChanges(); // ngOnInit

    expect(component.isLoading).toBeFalse();
    expect(component.data).toEqual(mockData);
    
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.text-3xl')?.textContent).toContain('1');
    expect(compiled.textContent).toContain('أهم الإجراءات المطلوبة');
  });

  it('should display error state if loading fails', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(throwError(() => new Error('API Error')));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    
    fixture.detectChanges(); // ngOnInit

    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBeTruthy();
    
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('حدث خطأ أثناء تحميل بيانات');
  });

  it('should display empty state for top urgent actions if array is empty', () => {
    const emptyMock = { ...mockData, topUrgentActions: [] };
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(emptyMock));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    
    fixture.detectChanges(); // ngOnInit

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('كل شيء على ما يرام!');
  });

  it('should show create offer action for OfferCandidate', () => {
    const data: ActionCenterResponseDto = {
      ...mockData,
      summary: { ...mockData.summary, outOfStockCount: 0, offerCandidatesCount: 1 },
      outOfStock: [],
      offerCandidates: [{ productId: 2, productName: 'P2', barcode: '222', currentStock: 8 }]
    };
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(data));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.activeTab = 'offers';
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('إنشاء عرض');
  });

  it('should show create offer action for ExpiringSoon', () => {
    const data: ActionCenterResponseDto = {
      ...mockData,
      summary: { ...mockData.summary, outOfStockCount: 0, expiringSoonBatchesCount: 1 },
      outOfStock: [],
      expiringSoon: [{ productId: 3, productName: 'P3', barcode: '333', currentStock: 4, batchId: 9, expiryDate: '2026-06-01' }]
    };
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(data));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.activeTab = 'expiringSoon';
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('إنشاء عرض');
  });

  it('should not show create offer action for OutOfStock', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.activeTab = 'outOfStock';
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('إنشاء عرض');
  });

  it('should navigate to offers with action and productId when create offer is clicked', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;

    component.createOffer({ productId: 5, productName: 'P5', barcode: '555', currentStock: 2 });

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/offers'], {
      queryParams: { action: 'create', productId: 5, source: 'action-center' }
    });
  });

  it('should navigate to inventory with barcode search', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;

    component.openInventory({ productId: 6, productName: 'P6', barcode: '666', currentStock: 0 });

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/inventory'], {
      queryParams: { search: '666' }
    });
  });

  it('should navigate to products with productId', () => {
    apiServiceSpy.getActionCenterSummary.and.returnValue(of(mockData));
    fixture = TestBed.createComponent(ActionCenterComponent);
    component = fixture.componentInstance;

    component.openProduct({ productId: 7, productName: 'P7', barcode: '777', currentStock: 0 });

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/products'], {
      queryParams: { productId: 7 }
    });
  });
});
