import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { BlackBoxComponent } from './black-box.component';
import { BlackBoxApiService } from '../../core/services/black-box-api.service';

describe('BlackBoxComponent', () => {
  let component: BlackBoxComponent;
  let fixture: ComponentFixture<BlackBoxComponent>;
  let apiService: jasmine.SpyObj<BlackBoxApiService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('BlackBoxApiService', ['getEvents', 'getEvent']);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FormsModule, BlackBoxComponent],
      providers: [
        { provide: BlackBoxApiService, useValue: spy }
      ]
    }).compileComponents();

    apiService = TestBed.inject(BlackBoxApiService) as jasmine.SpyObj<BlackBoxApiService>;
    apiService.getEvents.and.returnValue(of({
      totalCount: 2,
      page: 1,
      pageSize: 50,
      items: [
        { id: 1, actionType: 'LOGIN', result: 'SUCCESS', hasMetadata: true, metadataTruncated: false, createdAtUtc: '2026-05-29T00:00:00Z' },
        { id: 2, actionType: 'DELETE', result: 'FAILED', hasMetadata: false, metadataTruncated: false, createdAtUtc: '2026-05-29T00:01:00Z' }
      ]
    }));
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BlackBoxComponent);
    component = fixture.componentInstance;
  });

  it('should load component and call getEvents on init', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(apiService.getEvents).toHaveBeenCalledWith({ page: 1, pageSize: 50 });
    expect(component.events.length).toBe(2);
  });

  it('should apply filters and call getEvents with correct parameters', () => {
    fixture.detectChanges();
    component.filterDateFrom = '2026-05-29';
    component.filterResult = 'SUCCESS';
    component.filterEmployeeId = 5;
    component.search();

    expect(component.page).toBe(1);
    expect(apiService.getEvents).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      dateFrom: '2026-05-29',
      result: 'SUCCESS',
      employeeId: 5
    });
  });

  it('should load event details when detail button is clicked', () => {
    apiService.getEvent.and.returnValue(of({
      id: 1,
      actionType: 'LOGIN',
      result: 'SUCCESS',
      hasMetadata: true,
      metadataTruncated: false,
      createdAtUtc: '2026-05-29T00:00:00Z',
      metadataJson: '{"user":"test"}'
    }));

    fixture.detectChanges();
    component.openDetails(1);

    expect(apiService.getEvent).toHaveBeenCalledWith(1);
    expect(component.selectedEvent?.id).toBe(1);
    expect(component.activeModal).toBe('details');
  });

  it('should format and render metadata safely using text interpolation', () => {
    const mockMetadata = '{"user":"test","role":"admin"}';
    apiService.getEvent.and.returnValue(of({
      id: 1,
      actionType: 'LOGIN',
      result: 'SUCCESS',
      hasMetadata: true,
      metadataTruncated: false,
      createdAtUtc: '2026-05-29T00:00:00Z',
      metadataJson: mockMetadata
    }));

    fixture.detectChanges();
    component.openDetails(1);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const preElement = compiled.querySelector('pre');
    expect(preElement).toBeTruthy();
    expect(preElement?.textContent).toContain('"user": "test"');

    const codeElement = compiled.querySelector('code');
    expect(codeElement?.innerHTML).not.toContain('<script>');
  });

  it('should handle 403 error and display forbidden message', () => {
    apiService.getEvents.and.returnValue(throwError(() => ({ status: 403 })));

    fixture.detectChanges();

    expect(component.isForbidden).toBeTrue();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const forbiddenHeading = compiled.querySelector('.bg-red-50 h3');
    expect(forbiddenHeading?.textContent).toContain('عذرًا، لا تملك الصلاحية للوصول إلى الصندوق الأسود.');
  });
});
