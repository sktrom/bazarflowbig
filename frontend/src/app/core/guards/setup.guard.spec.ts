import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SetupGuard } from './setup.guard';
import { SetupApiService } from '../services/setup-api.service';

describe('SetupGuard', () => {
  let guard: SetupGuard;
  let setupApiSpy: jasmine.SpyObj<SetupApiService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    setupApiSpy = jasmine.createSpyObj('SetupApiService', ['getStatus']);
    routerSpy = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        SetupGuard,
        { provide: SetupApiService, useValue: setupApiSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(SetupGuard);
  });

  it('should allow access to /setup if setupCompleted is false', (done) => {
    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: false }));

    guard.canActivate().subscribe((result) => {
      expect(result).toBeTrue();
      done();
    });
  });

  it('should redirect to /login if setupCompleted is true', (done) => {
    const mockUrlTree = {} as UrlTree;
    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: true }));
    routerSpy.parseUrl.and.returnValue(mockUrlTree);

    guard.canActivate().subscribe((result) => {
      expect(result).toBe(mockUrlTree);
      expect(routerSpy.parseUrl).toHaveBeenCalledWith('/login');
      done();
    });
  });

  it('should allow access to /setup if API fails', (done) => {
    setupApiSpy.getStatus.and.returnValue(throwError(() => new Error('API Error')));

    guard.canActivate().subscribe((result) => {
      expect(result).toBeTrue();
      done();
    });
  });
});
