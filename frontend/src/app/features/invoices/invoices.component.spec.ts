import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { InvoicesComponent } from './invoices.component';
import { InvoicesApiService, InvoiceDetailsResponse } from './services/invoices-api.service';
import { InvoicesStateService } from './services/invoices-state.service';
import { of } from 'rxjs';
import { HttpResponse, HttpHeaders } from '@angular/common/http';
import { RouterTestingModule } from '@angular/router/testing';
import { By } from '@angular/platform-browser';
import { BlackBoxRecorderService } from '../../core/services/black-box-recorder.service';

describe('InvoicesComponent Export', () => {
  let component: InvoicesComponent;
  let fixture: ComponentFixture<InvoicesComponent>;
  let apiSpy: jasmine.SpyObj<InvoicesApiService>;
  let stateSpy: jasmine.SpyObj<InvoicesStateService>;
  let blackBoxSpy: jasmine.SpyObj<BlackBoxRecorderService>;

  const mockDetails: InvoiceDetailsResponse = {
    invoiceId: 10,
    invoiceNumber: 'INV-10',
    status: 'Completed',
    customerName: 'Ahmad',
    originalEmployeeId: 1,
    employeeName: 'Cashier One',
    subtotalUsd: 12,
    totalUsd: 10,
    exchangeRateSypSnapshot: 10000,
    totalSyp: 100000,
    hasManualPriceEdit: false,
    hasAdjustmentRequest: false,
    createdAt: '2026-05-18T10:00:00Z',
    completedAt: '2026-05-18T10:05:00Z',
    lines: [
      {
        lineId: 1,
        productId: 1,
        productName: 'Prod A',
        quantity: 1,
        unitPriceUsdOriginal: 12,
        lineTotalUsdOriginal: 12,
        lineTotalUsdEffective: 12,
        isPriceOverridden: false,
        sortOrder: 1
      }
    ]
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('InvoicesApiService', ['exportInvoices', 'getInvoices', 'getInvoiceDetails']);
    blackBoxSpy = jasmine.createSpyObj('BlackBoxRecorderService', ['recordSuccess', 'recordFailure']);
    stateSpy = jasmine.createSpyObj('InvoicesStateService', ['loadInvoices', 'setError'], {
      state$: of({ items: [], totalCount: 0, page: 1, pageSize: 20, isLoading: false, error: null })
    });

    await TestBed.configureTestingModule({
      imports: [InvoicesComponent, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: InvoicesApiService, useValue: apiSpy },
        { provide: InvoicesStateService, useValue: stateSpy },
        { provide: BlackBoxRecorderService, useValue: blackBoxSpy }
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

  it('prints the loaded invoice details receipt', () => {
    const printSpy = spyOn(window, 'print').and.stub();
    apiSpy.getInvoiceDetails.and.returnValue(of(mockDetails));

    component.viewDetails(10);
    fixture.detectChanges();

    const printButton = fixture.debugElement.query(By.css('[data-testid="invoice-print-button"]'));
    expect(printButton).toBeTruthy();

    printButton.triggerEventHandler('click', new MouseEvent('click'));

    expect(apiSpy.getInvoiceDetails).toHaveBeenCalledWith(10);
    expect(printSpy).toHaveBeenCalled();
  });
});
