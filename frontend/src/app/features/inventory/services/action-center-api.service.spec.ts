import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ActionCenterApiService } from './action-center-api.service';
import { environment } from '../../../../environments/environment';

describe('ActionCenterApiService', () => {
  let service: ActionCenterApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(ActionCenterApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch action center summary', () => {
    const mockResponse = {
      summary: {
        outOfStockCount: 1,
        lowStockCount: 2,
        expiredBatchesCount: 3,
        expiringSoonBatchesCount: 4,
        inactiveWithStockCount: 5,
        offerCandidatesCount: 6
      },
      topUrgentActions: [],
      outOfStock: [],
      lowStock: [],
      expiringSoon: [],
      expired: [],
      inactiveWithStock: [],
      offerCandidates: []
    };

    service.getActionCenterSummary().subscribe((res) => {
      expect(res).toEqual(mockResponse as any);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/inventory/action-center`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
