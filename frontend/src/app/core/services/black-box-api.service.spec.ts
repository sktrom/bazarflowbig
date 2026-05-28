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
});
