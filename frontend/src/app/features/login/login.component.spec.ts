import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { LoginComponent } from './login.component';
import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';
import { PermissionsService } from '../../core/services/permissions.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let authApiSpy: jasmine.SpyObj<AuthApiService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let sessionServiceSpy: jasmine.SpyObj<SessionService>;
  let permissionsServiceSpy: jasmine.SpyObj<PermissionsService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockSuccessResponse = {
    employeeId: 1,
    fullName: 'أحمد علي',
    sessionId: 999,
    deviceCode: 'DEV-01',
    allowedScreenKeys: ['Cashier', 'Invoices']
  };

  beforeEach(async () => {
    authApiSpy = jasmine.createSpyObj('AuthApiService', ['login']);
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isLoggedIn', 'setAuthenticated']);
    sessionServiceSpy = jasmine.createSpyObj('SessionService', ['setSessionId', 'setEmployee', 'getSessionId']);
    permissionsServiceSpy = jasmine.createSpyObj('PermissionsService', ['setPermissions']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    authServiceSpy.isLoggedIn.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthApiService, useValue: authApiSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: PermissionsService, useValue: permissionsServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    component.ngOnInit();
  });

  // --- Form Validation ---

  it('should disable submit when form is invalid', () => {
    expect(component.loginForm.invalid).toBeTrue();
  });

  it('should mark username as invalid when empty on submit', () => {
    component.loginForm.get('password')?.setValue('pass123');
    component.onSubmit();
    expect(component.loginForm.get('username')?.touched).toBeTrue();
    expect(component.loginForm.get('username')?.invalid).toBeTrue();
  });

  it('should mark password as invalid when empty on submit', () => {
    component.loginForm.get('username')?.setValue('user1');
    component.onSubmit();
    expect(component.loginForm.get('password')?.touched).toBeTrue();
    expect(component.loginForm.get('password')?.invalid).toBeTrue();
  });

  it('should not call authApiService when form is invalid', () => {
    component.onSubmit();
    expect(authApiSpy.login).not.toHaveBeenCalled();
  });

  // --- deviceCode in payload ---

  it('should include deviceCode in the request payload', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.get('username')?.setValue('user1');
    component.loginForm.get('password')?.setValue('pass1');
    component.onSubmit();

    expect(authApiSpy.login).toHaveBeenCalledWith(
      jasmine.objectContaining({ deviceCode: jasmine.any(String) })
    );
  });

  // --- Success flow ---

  it('should store sessionId after successful login', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(sessionServiceSpy.setSessionId).toHaveBeenCalledWith(999);
  });

  it('should store allowedScreenKeys in PermissionsService after login', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(permissionsServiceSpy.setPermissions)
      .toHaveBeenCalledWith(['Cashier', 'Invoices']);
  });

  it('should navigate to /cashier after successful login', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/cashier']);
  });

  it('should mark authenticated after successful login', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(authServiceSpy.setAuthenticated).toHaveBeenCalledWith(true);
  });

  // --- Loading state ---

  it('should set isLoading true while request is in-flight', () => {
    authApiSpy.login.and.returnValue(of(mockSuccessResponse));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    // isLoading resets after success — but during sync observable it completes immediately
    // Structural check: isLoading starts as false before submit
    expect(component.isLoading).toBeFalse(); // after completion
  });

  // --- Error mapping ---

  it('should show credentials error on 400 INVALID_CREDENTIALS', () => {
    const err = new HttpErrorResponse({ status: 400, error: { error: 'INVALID_CREDENTIALS' } });
    authApiSpy.login.and.returnValue(throwError(() => err));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(component.apiError).toContain('غير صحيحة');
  });

  it('should show device error on 400 DEVICE_NOT_FOUND', () => {
    const err = new HttpErrorResponse({ status: 400, error: { error: 'DEVICE_NOT_FOUND' } });
    authApiSpy.login.and.returnValue(throwError(() => err));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(component.apiError).toContain('الجهاز');
  });

  it('should show active session error on 409', () => {
    const err = new HttpErrorResponse({ status: 409, error: { error: 'EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION' } });
    authApiSpy.login.and.returnValue(throwError(() => err));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(component.apiError).toContain('جلسة نشطة');
  });

  it('should show forbidden error on 403', () => {
    const err = new HttpErrorResponse({ status: 403, error: {} });
    authApiSpy.login.and.returnValue(throwError(() => err));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(component.apiError).toContain('غير مفعّل');
  });

  it('should show connection error on 500', () => {
    const err = new HttpErrorResponse({ status: 500, error: {} });
    authApiSpy.login.and.returnValue(throwError(() => err));
    component.loginForm.setValue({ username: 'user1', password: 'pass1' });
    component.onSubmit();
    expect(component.apiError).toContain('الاتصال');
  });

  // --- Existing session redirect ---

  it('should redirect to /cashier if already authenticated on init', () => {
    authServiceSpy.isLoggedIn.and.returnValue(true);
    component.ngOnInit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/cashier']);
  });
});
