import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginSetupGuard } from './login-setup.guard';
import { SetupApiService } from '../services/setup-api.service';

describe('LoginSetupGuard', () => {
  let guard: LoginSetupGuard;
  let setupApiSpy: jasmine.SpyObj<SetupApiService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    setupApiSpy = jasmine.createSpyObj('SetupApiService', ['getStatus']);
    routerSpy = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        LoginSetupGuard,
        { provide: SetupApiService, useValue: setupApiSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(LoginSetupGuard);
  });

  it('should redirect to /setup if setupCompleted is false', (done) => {
    const mockUrlTree = {} as UrlTree;
    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: false }));
    routerSpy.parseUrl.and.returnValue(mockUrlTree);

    guard.canActivate().subscribe((result) => {
      expect(result).toBe(mockUrlTree);
      expect(routerSpy.parseUrl).toHaveBeenCalledWith('/setup');
      done();
    });
  });

  it('should allow access to /login if setupCompleted is true', (done) => {
    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: true }));

    guard.canActivate().subscribe((result) => {
      expect(result).toBeTrue();
      done();
    });
  });

  it('should allow access to /login if API fails', (done) => {
    setupApiSpy.getStatus.and.returnValue(throwError(() => new Error('API Error')));

    guard.canActivate().subscribe((result) => {
      expect(result).toBeTrue();
      done();
    });
  });
});
