import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { SetupComponent } from './setup.component';
import { SetupApiService } from '../../core/services/setup-api.service';
import { SessionService } from '../../core/services/session.service';

describe('SetupComponent', () => {
  let component: SetupComponent;
  let fixture: ComponentFixture<SetupComponent>;
  let setupApiSpy: jasmine.SpyObj<SetupApiService>;
  let sessionServiceSpy: jasmine.SpyObj<SessionService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    setupApiSpy = jasmine.createSpyObj('SetupApiService', ['getStatus', 'complete', 'setCompletedCache']);
    sessionServiceSpy = jasmine.createSpyObj('SessionService', ['setDeviceCode']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: false }));

    await TestBed.configureTestingModule({
      imports: [SetupComponent, ReactiveFormsModule],
      providers: [
        { provide: SetupApiService, useValue: setupApiSpy },
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SetupComponent);
    component = fixture.componentInstance;
  });

  it('should create and render initial state', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(component.currentStep).toBe(1);
    expect(component.isInitializing).toBeFalse();
  });

  it('should redirect to /login if setup is already completed on init', () => {
    setupApiSpy.getStatus.and.returnValue(of({ setupCompleted: true }));
    fixture.detectChanges();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should show connection failure message if getStatus fails on init', () => {
    setupApiSpy.getStatus.and.returnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();

    expect(component.connectionFailed).toBeTrue();
    expect(component.apiError).toContain('تعذر التحقق من حالة النظام');
  });

  // --- Step Navigation & Validations ---

  it('should validate Step 1 inputs and block next step if invalid', () => {
    fixture.detectChanges();
    
    // Initial state is invalid
    expect(component.canGoNext(1)).toBeFalse();
    
    // Fill invalid values
    component.setupForm.get('storeName')?.setValue('');
    component.setupForm.get('exchangeRate')?.setValue(-500);
    expect(component.canGoNext(1)).toBeFalse();

    // Fill valid values
    component.setupForm.get('storeName')?.setValue('BazarFlow Test');
    component.setupForm.get('exchangeRate')?.setValue(15000);
    expect(component.canGoNext(1)).toBeTrue();

    // Try going next
    component.nextStep();
    expect(component.currentStep).toBe(2);
  });

  it('should validate Step 2 admin inputs and block weak password', () => {
    fixture.detectChanges();
    component.currentStep = 2;

    // Fill standard required inputs
    component.setupForm.get('adminFullName')?.setValue('أحمد علي');
    component.setupForm.get('adminUsername')?.setValue('admin');
    
    // Weak password 'admin123'
    component.setupForm.get('adminPassword')?.setValue('admin123');
    component.setupForm.get('confirmPassword')?.setValue('admin123');
    expect(component.canGoNext(2)).toBeFalse();

    // Password equals username
    component.setupForm.get('adminPassword')?.setValue('admin');
    component.setupForm.get('confirmPassword')?.setValue('admin');
    expect(component.canGoNext(2)).toBeFalse();

    // Too short password
    component.setupForm.get('adminPassword')?.setValue('123');
    component.setupForm.get('confirmPassword')?.setValue('123');
    expect(component.canGoNext(2)).toBeFalse();

    // Valid password
    component.setupForm.get('adminPassword')?.setValue('securePassWord123');
    component.setupForm.get('confirmPassword')?.setValue('securePassWord123');
    expect(component.canGoNext(2)).toBeTrue();

    component.nextStep();
    expect(component.currentStep).toBe(3);
  });

  it('should validate Step 3 device inputs and allow proceeding to step 4', () => {
    fixture.detectChanges();
    component.currentStep = 3;

    expect(component.canGoNext(3)).toBeFalse();

    component.setupForm.get('deviceCode')?.setValue('DEV-001');
    component.setupForm.get('deviceName')?.setValue('Main Cashier');
    expect(component.canGoNext(3)).toBeTrue();

    component.nextStep();
    expect(component.currentStep).toBe(4);
  });

  it('should reject DEFAULT_DEVICE on Step 3 before submit', () => {
    fixture.detectChanges();
    component.currentStep = 3;

    component.setupForm.get('deviceCode')?.setValue('DEFAULT_DEVICE');
    component.setupForm.get('deviceName')?.setValue('Main Cashier');

    expect(component.setupForm.get('deviceCode')?.hasError('defaultDeviceCode')).toBeTrue();
    expect(component.canGoNext(3)).toBeFalse();
    expect(setupApiSpy.complete).not.toHaveBeenCalled();
  });

  // --- Submit and Redirect ---

  it('should submit correct form data and redirect to /login on success', () => {
    setupApiSpy.complete.and.returnValue(of({ success: true, message: 'Setup completed successfully.' }));
    
    fixture.detectChanges();
    
    // Fill all form values
    component.setupForm.setValue({
      storeName: '  BazarFlow Store  ',
      exchangeRate: 15000.5,
      adminFullName: '  Admin Name  ',
      adminUsername: '  adminuser  ',
      adminPassword: '  securePass123  ',
      confirmPassword: '  securePass123  ',
      deviceCode: '  DEV-101  ',
      deviceName: '  Main Desk  '
    });

    component.currentStep = 4;
    component.onSubmit();

    expect(setupApiSpy.complete).toHaveBeenCalledWith({
      storeName: 'BazarFlow Store',
      exchangeRate: 15000.5,
      adminFullName: 'Admin Name',
      adminUsername: 'adminuser',
      adminPassword: 'securePass123',
      deviceCode: 'DEV-101',
      deviceName: 'Main Desk'
    });

    expect(sessionServiceSpy.setDeviceCode).toHaveBeenCalledWith('DEV-101');
    expect(setupApiSpy.setCompletedCache).toHaveBeenCalledWith(true);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  // --- Error Mapping ---

  it('should map API errors to user-friendly Arabic messages', () => {
    fixture.detectChanges();
    component.currentStep = 4;
    
    component.setupForm.setValue({
      storeName: 'BazarFlow Store',
      exchangeRate: 15000,
      adminFullName: 'Admin Name',
      adminUsername: 'adminuser',
      adminPassword: 'securePass123',
      confirmPassword: 'securePass123',
      deviceCode: 'DEV-101',
      deviceName: 'Main Desk'
    });

    const errors: Record<string, string> = {
      'SETUP_ALREADY_COMPLETED': 'لقد تم إعداد النظام بالفعل.',
      'SETUP_STATE_AMBIGUOUS': 'يحتوي النظام على حسابات موظفين مسبقاً، لا يمكن إعادة التهيئة.',
      'INVALID_ADMIN_PASSWORD': 'كلمة المرور غير آمنة أو مطابقة لاسم المستخدم.',
      'INVALID_EXCHANGE_RATE': 'سعر الصرف يجب أن يكون أكبر من الصفر.',
      'DEVICE_CODE_ALREADY_EXISTS': 'رمز الجهاز هذا مستخدم بالفعل في النظام.',
      'DEFAULT_DEVICE_NOT_ALLOWED': 'لا يمكن استخدام DEFAULT_DEVICE كجهاز مخصص. أدخل رمز جهاز مختلف.',
      'SETUP_VALIDATION_ERROR': 'حدث خطأ في التحقق من البيانات المدخلة.'
    };

    Object.keys(errors).forEach(errCode => {
      const errorResponse = new HttpErrorResponse({
        error: { error: errCode },
        status: 400
      });
      setupApiSpy.complete.and.returnValue(throwError(() => errorResponse));

      component.onSubmit();
      expect(component.apiError).toBe(errors[errCode]);
    });
  });

  it('should display developer attribution text', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('أياز مراد');
    expect(compiled.textContent).toContain('Ayaz Murad');
  });
});
