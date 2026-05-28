import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { BlackBoxApiService } from './black-box-api.service';

describe('BlackBoxApiService', () => {
  let service: BlackBoxApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(BlackBoxApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('posts event to black box endpoint', () => {
    service.createEvent({
      route: '/cashier',
      pageName: 'Cashier',
      actionType: 'COMPLETE_INVOICE',
      result: 'SUCCESS'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/api/black-box/events`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.actionType).toBe('COMPLETE_INVOICE');
    req.flush({ success: true, id: 1 });
  });

  it('gets events from black box endpoint with parameters', () => {
    const mockResponse = {
      totalCount: 1,
      page: 1,
      pageSize: 50,
      items: [{ id: 123, actionType: 'VIEW', result: 'SUCCESS', hasMetadata: false, metadataTruncated: false, createdAtUtc: '2026-05-29T00:00:00Z' }]
    };

    service.getEvents({ page: 1, pageSize: 50, result: 'SUCCESS' }).subscribe(res => {
      expect(res.totalCount).toBe(1);
      expect(res.items.length).toBe(1);
      expect(res.items[0].id).toBe(123);
    });

    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/api/black-box/events` && request.params.get('result') === 'SUCCESS');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('gets single event details from black box endpoint', () => {
    const mockDetail = {
      id: 123,
      actionType: 'VIEW',
      result: 'SUCCESS',
      hasMetadata: true,
      metadataTruncated: false,
      createdAtUtc: '2026-05-29T00:00:00Z',
      metadataJson: '{"key":"val"}',
      ipAddress: '127.0.0.1',
      userAgent: 'Mozilla/5.0'
    };

    service.getEvent(123).subscribe(res => {
      expect(res.id).toBe(123);
      expect(res.metadataJson).toBe('{"key":"val"}');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/black-box/events/123`);
    expect(req.request.method).toBe('GET');
    req.flush(mockDetail);
  });
});
