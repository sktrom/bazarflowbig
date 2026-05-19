import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActionCenterComponent } from './action-center.component';
import { ActionCenterApiService } from '../../services/action-center-api.service';
import { of, throwError } from 'rxjs';
import { ActionCenterResponseDto } from '../../models/action-center.model';

describe('ActionCenterComponent', () => {
  let component: ActionCenterComponent;
  let fixture: ComponentFixture<ActionCenterComponent>;
  let apiServiceSpy: jasmine.SpyObj<ActionCenterApiService>;

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

    await TestBed.configureTestingModule({
      imports: [ActionCenterComponent],
      providers: [
        { provide: ActionCenterApiService, useValue: spy }
      ]
    }).compileComponents();

    apiServiceSpy = TestBed.inject(ActionCenterApiService) as jasmine.SpyObj<ActionCenterApiService>;
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
});
