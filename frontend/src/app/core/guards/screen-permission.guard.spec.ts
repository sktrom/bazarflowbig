import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { ScreenPermissionGuard } from './screen-permission.guard';
import { PermissionsService } from '../services/permissions.service';

describe('ScreenPermissionGuard', () => {
  let guard: ScreenPermissionGuard;
  let permissionsService: jasmine.SpyObj<PermissionsService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const permSpy = jasmine.createSpyObj('PermissionsService', ['hasPermission']);
    const routerSpy = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        ScreenPermissionGuard,
        { provide: PermissionsService, useValue: permSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(ScreenPermissionGuard);
    permissionsService = TestBed.inject(PermissionsService) as jasmine.SpyObj<PermissionsService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should return true if user has required screen permission', () => {
    const route = new ActivatedRouteSnapshot();
    route.data = { screenKey: 'Reports' };
    permissionsService.hasPermission.withArgs('Reports').and.returnValue(true);

    expect(guard.canActivate(route)).toBeTrue();
  });

  it('should redirect to /unauthorized if user lacks permission', () => {
    const route = new ActivatedRouteSnapshot();
    route.data = { screenKey: 'Reports' };
    permissionsService.hasPermission.withArgs('Reports').and.returnValue(false);
    
    const mockUrlTree = {} as any;
    router.parseUrl.and.returnValue(mockUrlTree);
    
    expect(guard.canActivate(route)).toBe(mockUrlTree);
    expect(router.parseUrl).toHaveBeenCalledWith('/unauthorized');
  });
});
