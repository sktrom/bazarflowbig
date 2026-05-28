import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BlackBoxApiService } from './black-box-api.service';
import { BlackBoxRecorderService } from './black-box-recorder.service';

describe('BlackBoxRecorderService', () => {
  let service: BlackBoxRecorderService;
  let apiSpy: jasmine.SpyObj<BlackBoxApiService>;

  beforeEach(() => {
    apiSpy = jasmine.createSpyObj('BlackBoxApiService', ['createEvent']);
    apiSpy.createEvent.and.returnValue(of({ success: true, id: 1 }));

    TestBed.configureTestingModule({
      providers: [
        BlackBoxRecorderService,
        { provide: BlackBoxApiService, useValue: apiSpy },
        { provide: Router, useValue: { url: '/cashier?x=1' } }
      ]
    });

    service = TestBed.inject(BlackBoxRecorderService);
  });

  it('sends sanitized metadata', () => {
    service.recordSuccess('COMPLETE_INVOICE', {
      entityType: 'Invoice',
      entityId: 10,
      metadata: {
        totalUsd: 12,
        password: 'secret',
        nested: {
          sessionToken: 'token',
          safe: true
        }
      }
    });

    expect(apiSpy.createEvent).toHaveBeenCalledWith(jasmine.objectContaining({
      route: '/cashier?x=1',
      pageName: 'cashier',
      actionType: 'COMPLETE_INVOICE',
      entityType: 'Invoice',
      entityId: '10',
      result: 'SUCCESS',
      metadata: {
        totalUsd: 12,
        nested: {
          safe: true
        }
      }
    }));
  });

  it('strips sensitive keys case-insensitively', () => {
    const sanitized = service.sanitizeMetadata({
      apiKey: 'a',
      Authorization: 'b',
      connectionString: 'c',
      publicValue: 'ok'
    });

    expect(sanitized).toEqual({ publicValue: 'ok' });
  });

  it('does not throw when recorder API fails', () => {
    apiSpy.createEvent.and.returnValue(throwError(() => new Error('network')));

    expect(() => service.recordFailure('CREATE_PRODUCT', { metadata: { safe: true } })).not.toThrow();
  });
});
