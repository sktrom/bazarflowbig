import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { InvoicesComponent } from './invoices.component';
import { InvoicesApiService } from './services/invoices-api.service';
import { InvoicesStateService } from './services/invoices-state.service';
import { of } from 'rxjs';
import { HttpResponse, HttpHeaders } from '@angular/common/http';
import { RouterTestingModule } from '@angular/router/testing';

describe('InvoicesComponent Export', () => {
  let component: InvoicesComponent;
  let fixture: ComponentFixture<InvoicesComponent>;
  let apiSpy: jasmine.SpyObj<InvoicesApiService>;
  let stateSpy: jasmine.SpyObj<InvoicesStateService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('InvoicesApiService', ['exportInvoices', 'getInvoices']);
    stateSpy = jasmine.createSpyObj('InvoicesStateService', ['loadInvoices', 'setError'], {
      state$: of({ items: [], totalCount: 0, page: 1, pageSize: 20, isLoading: false, error: null })
    });

    await TestBed.configureTestingModule({
      imports: [InvoicesComponent, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: InvoicesApiService, useValue: apiSpy },
        { provide: InvoicesStateService, useValue: stateSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoicesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should call exportInvoices with current filters and trigger download', () => {
    component.filters = {
      customerName: 'Ahmad',
      status: 'Completed',
      dateFrom: '2026-05-01',
      dateTo: '',
      employeeId: null,
      adjustmentRequestStatus: '',
      manualPriceEdited: true
    };

    const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const mockResponse = new HttpResponse({ 
      body: mockBlob, 
      headers: new HttpHeaders({'Content-Disposition': 'attachment; filename="invoices_export.xlsx"'}) 
    });
    
    apiSpy.exportInvoices.and.returnValue(of(mockResponse));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
    spyOn(window.URL, 'revokeObjectURL');
    
    component.exportInvoices();
    
    expect(apiSpy.exportInvoices).toHaveBeenCalledWith(jasmine.objectContaining({
      format: 'excel',
      customerName: 'Ahmad',
      status: 'Completed',
      dateFrom: '2026-05-01',
      manualPriceEdited: true
    }));
    expect(window.URL.createObjectURL).toHaveBeenCalledWith(mockBlob);
  });
});
