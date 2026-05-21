import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { SetupApiService, SetupCompleteRequest } from '../../core/services/setup-api.service';
import { SessionService } from '../../core/services/session.service';
import { FormErrorComponent } from '../../shared/components/form-helpers/form-error.component';

function passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) return null;
  if (value.toLowerCase() === 'admin123') {
    return { weakPassword: true };
  }
  return null;
}

function deviceCodeValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) return null;
  if (value.trim().toUpperCase() === 'DEFAULT_DEVICE') {
    return { defaultDeviceCode: true };
  }
  return null;
}

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormErrorComponent],
  template: `
    <div class="min-h-screen bg-slate-100 flex items-center justify-center p-4" dir="rtl">
      <div class="w-full max-w-xl">

        <!-- Header -->
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold text-primary tracking-tight">Bazarflow</h1>
          <p class="text-slate-500 mt-1 text-sm">معالج إعداد النظام الأول</p>
        </div>

        <!-- Setup Card -->
        <div class="bg-white border border-slate-200 rounded-xl shadow-lg p-8">
          
          <!-- Initializing State -->
          <div *ngIf="isInitializing" class="flex flex-col items-center justify-center py-12">
            <div class="w-10 h-10 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
            <p class="text-slate-500 mt-4 text-sm">جاري التحقق من حالة النظام...</p>
          </div>

          <!-- Connection Failed -->
          <div *ngIf="!isInitializing && connectionFailed" class="text-center py-8">
            <div class="mb-4 text-red-600 font-semibold">{{ apiError }}</div>
            <button (click)="retryInit()" class="px-5 py-2 bg-primary text-white rounded-md text-sm font-medium hover:bg-primary-dark transition-colors">
              إعادة المحاولة
            </button>
          </div>

          <!-- Wizard Form -->
          <ng-container *ngIf="!isInitializing && !connectionFailed">
            
            <!-- Stepper Progress Tracker -->
            <div class="flex items-center justify-between mb-8 pb-4 border-b border-slate-100">
              <div *ngFor="let s of [1, 2, 3, 4]" class="flex items-center">
                <div 
                  class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold transition-colors"
                  [class.bg-primary]="currentStep === s"
                  [class.text-white]="currentStep === s"
                  [class.bg-green-500]="currentStep > s"
                  [class.text-white-active]="currentStep > s"
                  [class.bg-slate-100]="currentStep < s"
                  [class.text-slate-400]="currentStep < s"
                >
                  <span *ngIf="currentStep <= s">{{ s }}</span>
                  <span *ngIf="currentStep > s">✓</span>
                </div>
                <span 
                  class="mr-2 text-xs font-semibold hidden sm:inline"
                  [class.text-slate-800]="currentStep === s"
                  [class.text-green-600]="currentStep > s"
                  [class.text-slate-400]="currentStep < s"
                >
                  {{ getStepLabel(s) }}
                </span>
                <!-- Connector Line -->
                <div *ngIf="s < 4" class="w-8 sm:w-12 h-0.5 mx-2 bg-slate-200" [class.bg-green-500]="currentStep > s"></div>
              </div>
            </div>

            <form [formGroup]="setupForm" (ngSubmit)="onSubmit()" autocomplete="off" novalidate>

              <!-- STEP 1: Store Information -->
              <div *ngIf="currentStep === 1">
                <h3 class="text-lg font-bold text-slate-800 mb-4">بيانات المتجر</h3>
                <p class="text-xs text-slate-500 mb-6">يرجى تحديد الاسم التجاري وسعر صرف العملة المحلية الأساسي للبدء.</p>

                <!-- Store Name -->
                <div class="mb-5">
                  <label for="storeName" class="block text-sm font-medium text-slate-700 mb-1">
                    اسم المتجر <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="storeName"
                    type="text"
                    formControlName="storeName"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('storeName')"
                    placeholder="مثال: سوبرماركت الخير"
                  />
                  <app-form-error [show]="isFieldInvalid('storeName')">
                    اسم المتجر مطلوب.
                  </app-form-error>
                </div>

                <!-- Exchange Rate SYP -->
                <div class="mb-6">
                  <label for="exchangeRate" class="block text-sm font-medium text-slate-700 mb-1">
                    سعر الصرف (ل.س مقابل الدولار) <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="exchangeRate"
                    type="number"
                    formControlName="exchangeRate"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('exchangeRate')"
                    placeholder="مثال: 15000"
                  />
                  <app-form-error [show]="!!(isFieldInvalid('exchangeRate') && setupForm.get('exchangeRate')?.hasError('required'))">
                    سعر الصرف مطلوب.
                  </app-form-error>
                  <app-form-error [show]="!!(isFieldInvalid('exchangeRate') && setupForm.get('exchangeRate')?.hasError('min'))">
                    يجب أن يكون سعر الصرف أكبر من الصفر.
                  </app-form-error>
                </div>
              </div>

              <!-- STEP 2: Administrator Account -->
              <div *ngIf="currentStep === 2">
                <h3 class="text-lg font-bold text-slate-800 mb-4">حساب المدير الأول</h3>
                <p class="text-xs text-slate-500 mb-6">سيتم تعيين هذا الحساب كمسؤول أول للنظام مع كامل الصلاحيات.</p>

                <!-- Full Name -->
                <div class="mb-4">
                  <label for="adminFullName" class="block text-sm font-medium text-slate-700 mb-1">
                    الاسم الكامل للمسؤول <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="adminFullName"
                    type="text"
                    formControlName="adminFullName"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('adminFullName')"
                    placeholder="مثال: أحمد المحمد"
                  />
                  <app-form-error [show]="isFieldInvalid('adminFullName')">
                    الاسم الكامل مطلوب.
                  </app-form-error>
                </div>

                <!-- Username -->
                <div class="mb-4">
                  <label for="adminUsername" class="block text-sm font-medium text-slate-700 mb-1">
                    اسم المستخدم <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="adminUsername"
                    type="text"
                    formControlName="adminUsername"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('adminUsername')"
                    placeholder="مثال: admin"
                  />
                  <app-form-error [show]="isFieldInvalid('adminUsername')">
                    اسم المستخدم مطلوب.
                  </app-form-error>
                </div>

                <!-- Password -->
                <div class="mb-4">
                  <label for="adminPassword" class="block text-sm font-medium text-slate-700 mb-1">
                    كلمة المرور <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="adminPassword"
                    type="password"
                    formControlName="adminPassword"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('adminPassword')"
                    placeholder="أدخل كلمة مرور قوية"
                  />
                  <app-form-error [show]="!!(isFieldInvalid('adminPassword') && setupForm.get('adminPassword')?.hasError('required'))">
                    كلمة المرور مطلوبة.
                  </app-form-error>
                  <app-form-error [show]="!!(isFieldInvalid('adminPassword') && setupForm.get('adminPassword')?.hasError('minlength'))">
                    كلمة المرور يجب أن لا تقل عن 6 أحرف.
                  </app-form-error>
                  <app-form-error [show]="!!(isFieldInvalid('adminPassword') && setupForm.get('adminPassword')?.hasError('weakPassword'))">
                    كلمة المرور 'admin123' غير مسموح بها لأسباب أمنية.
                  </app-form-error>
                  <app-form-error [show]="!!(isFieldInvalid('adminPassword') && !setupForm.get('adminPassword')?.hasError('required') && isPasswordSameAsUsername())">
                    لا يمكن أن تكون كلمة المرور مطابقة لاسم المستخدم.
                  </app-form-error>
                </div>

                <!-- Confirm Password -->
                <div class="mb-6">
                  <label for="confirmPassword" class="block text-sm font-medium text-slate-700 mb-1">
                    تأكيد كلمة المرور <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="confirmPassword"
                    type="password"
                    formControlName="confirmPassword"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isConfirmPasswordInvalid()"
                    placeholder="أعد إدخال كلمة المرور"
                  />
                  <app-form-error [show]="isConfirmPasswordInvalid()">
                    كلمتا المرور غير متطابقتين.
                  </app-form-error>
                </div>
              </div>

              <!-- STEP 3: First POS Device -->
              <div *ngIf="currentStep === 3">
                <h3 class="text-lg font-bold text-slate-800 mb-4">جهاز الـ POS الأول</h3>
                <p class="text-xs text-slate-500 mb-6">يرجى ربط هذا الجهاز الافتراضي لتسجيل عمليات البيع الفورية.</p>

                <!-- Device Code -->
                <div class="mb-5">
                  <label for="deviceCode" class="block text-sm font-medium text-slate-700 mb-1">
                    رمز الجهاز <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="deviceCode"
                    type="text"
                    formControlName="deviceCode"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('deviceCode')"
                    placeholder="مثال: DEV-101"
                  />
                  <app-form-error [show]="isFieldInvalid('deviceCode')">
                    رمز الجهاز مطلوب.
                  </app-form-error>
                  <app-form-error [show]="!!(isFieldInvalid('deviceCode') && setupForm.get('deviceCode')?.hasError('defaultDeviceCode'))">
                    لا يمكن استخدام DEFAULT_DEVICE كجهاز مخصص. أدخل رمز جهاز مختلف.
                  </app-form-error>
                </div>

                <!-- Device Name -->
                <div class="mb-6">
                  <label for="deviceName" class="block text-sm font-medium text-slate-700 mb-1">
                    اسم الجهاز الوصفي <span class="text-red-500">*</span>
                  </label>
                  <input
                    id="deviceName"
                    type="text"
                    formControlName="deviceName"
                    class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                    [class.border-red-400]="isFieldInvalid('deviceName')"
                    placeholder="مثال: كاشير الصالة الرئيسية"
                  />
                  <app-form-error [show]="isFieldInvalid('deviceName')">
                    اسم الجهاز مطلوب.
                  </app-form-error>
                </div>
              </div>

              <!-- STEP 4: Review and Submit -->
              <div *ngIf="currentStep === 4">
                <h3 class="text-lg font-bold text-slate-800 mb-4">مراجعة وتأكيد البيانات</h3>
                <p class="text-xs text-slate-500 mb-6">يرجى مراجعة كافة المدخلات بدقة قبل تأكيد عملية الحفظ والتشغيل.</p>

                <!-- Review Table -->
                <div class="bg-slate-50 rounded-lg p-5 border border-slate-100 text-sm space-y-3 mb-6">
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">اسم المتجر:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('storeName')?.value }}</span>
                  </div>
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">سعر الصرف:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('exchangeRate')?.value }} ل.س</span>
                  </div>
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">مدير النظام:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('adminFullName')?.value }}</span>
                  </div>
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">اسم مستخدم المدير:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('adminUsername')?.value }}</span>
                  </div>
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">كلمة المرور:</span>
                    <span class="font-bold text-slate-800">•••••••• (تم إدخال كلمة المرور)</span>
                  </div>
                  <div class="flex justify-between border-b border-slate-200 pb-2">
                    <span class="text-slate-500">رمز الجهاز الجديد:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('deviceCode')?.value }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-slate-500">اسم الجهاز:</span>
                    <span class="font-bold text-slate-800">{{ setupForm.get('deviceName')?.value }}</span>
                  </div>
                </div>
              </div>

              <!-- API Error Banner -->
              <div
                *ngIf="apiError && !connectionFailed"
                class="mb-6 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 text-center"
                role="alert"
              >
                {{ apiError }}
              </div>

              <!-- Button Bar -->
              <div class="flex items-center justify-between mt-8 pt-4 border-t border-slate-100">
                <!-- Previous Button -->
                <button
                  type="button"
                  (click)="prevStep()"
                  class="px-5 py-2 bg-slate-100 text-slate-700 rounded-md text-sm font-medium hover:bg-slate-200 transition-colors"
                  [disabled]="currentStep === 1 || isLoading"
                  [class.opacity-50]="currentStep === 1 || isLoading"
                >
                  السابق
                </button>

                <!-- Next or Complete Button -->
                <button
                  *ngIf="currentStep < 4"
                  type="button"
                  (click)="nextStep()"
                  [disabled]="!canGoNext(currentStep)"
                  class="px-5 py-2 bg-primary text-white rounded-md text-sm font-medium hover:bg-primary-dark transition-colors"
                  [class.opacity-50]="!canGoNext(currentStep)"
                >
                  التالي
                </button>

                <button
                  *ngIf="currentStep === 4"
                  type="submit"
                  [disabled]="isLoading || setupForm.invalid || isPasswordSameAsUsername()"
                  class="px-6 py-2 bg-green-600 text-white rounded-md text-sm font-medium hover:bg-green-700 transition-colors flex items-center gap-2"
                >
                  <svg *ngIf="isLoading" class="animate-spin h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                  </svg>
                  <span>{{ isLoading ? 'جاري إرسال البيانات...' : 'تأكيد وإكمال الإعداد' }}</span>
                </button>
              </div>

            </form>
          </ng-container>

        </div>
      </div>
    </div>
  `
})
export class SetupComponent implements OnInit {
  setupForm!: FormGroup;
  currentStep = 1;
  isInitializing = true;
  connectionFailed = false;
  isLoading = false;
  apiError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private setupApi: SetupApiService,
    private sessionService: SessionService
  ) {}

  ngOnInit(): void {
    this.checkStatus();

    this.setupForm = this.fb.group({
      storeName: ['', Validators.required],
      exchangeRate: [null, [Validators.required, Validators.min(0.0001)]],
      adminFullName: ['', Validators.required],
      adminUsername: ['', Validators.required],
      adminPassword: ['', [Validators.required, Validators.minLength(6), passwordStrengthValidator]],
      confirmPassword: ['', Validators.required],
      deviceCode: ['', [Validators.required, deviceCodeValidator]],
      deviceName: ['', Validators.required]
    });

    // Clear API error if form changes
    this.setupForm.valueChanges.subscribe(() => {
      if (this.apiError) {
        this.apiError = null;
      }
    });
  }

  checkStatus(): void {
    this.isInitializing = true;
    this.connectionFailed = false;
    this.setupApi.getStatus().subscribe({
      next: (res) => {
        this.isInitializing = false;
        if (res.setupCompleted) {
          this.router.navigate(['/login']);
        }
      },
      error: (err) => {
        this.isInitializing = false;
        this.connectionFailed = true;
        this.apiError = 'تعذر التحقق من حالة النظام. قد يكون هناك مشكلة في الاتصال بالخادم.';
      }
    });
  }

  retryInit(): void {
    this.checkStatus();
  }

  getStepLabel(step: number): string {
    switch (step) {
      case 1: return 'بيانات المتجر';
      case 2: return 'حساب المدير';
      case 3: return 'جهاز الـ POS';
      case 4: return 'المراجعة';
      default: return '';
    }
  }

  isFieldInvalid(field: string): boolean {
    const control = this.setupForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  isConfirmPasswordInvalid(): boolean {
    const confirm = this.setupForm.get('confirmPassword');
    const pwd = this.setupForm.get('adminPassword');
    return !!(confirm && (confirm.dirty || confirm.touched) && (confirm.value !== pwd?.value || confirm.invalid));
  }

  isPasswordSameAsUsername(): boolean {
    const username = this.setupForm.get('adminUsername')?.value;
    const password = this.setupForm.get('adminPassword')?.value;
    if (!username || !password) return false;
    return username.trim().toLowerCase() === password.trim().toLowerCase();
  }

  canGoNext(step: number): boolean {
    if (step === 1) {
      return !!(
        this.setupForm.get('storeName')?.valid &&
        this.setupForm.get('exchangeRate')?.valid &&
        this.setupForm.get('exchangeRate')?.value > 0
      );
    }
    if (step === 2) {
      const pwd = this.setupForm.get('adminPassword')?.value || '';
      const cpwd = this.setupForm.get('confirmPassword')?.value || '';
      return !!(
        this.setupForm.get('adminFullName')?.valid &&
        this.setupForm.get('adminUsername')?.valid &&
        this.setupForm.get('adminPassword')?.valid &&
        this.setupForm.get('confirmPassword')?.valid &&
        pwd.length >= 6 &&
        pwd.toLowerCase() !== 'admin123' &&
        !this.isPasswordSameAsUsername() &&
        pwd === cpwd
      );
    }
    if (step === 3) {
      return !!(
        this.setupForm.get('deviceCode')?.valid &&
        this.setupForm.get('deviceName')?.valid
      );
    }
    return true;
  }

  nextStep(): void {
    if (this.currentStep < 4 && this.canGoNext(this.currentStep)) {
      this.currentStep++;
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  onSubmit(): void {
    if (this.setupForm.invalid || this.isLoading || this.isPasswordSameAsUsername()) {
      return;
    }

    this.isLoading = true;
    this.apiError = null;

    const raw = this.setupForm.value;
    const request: SetupCompleteRequest = {
      storeName: raw.storeName.trim(),
      exchangeRate: Number(raw.exchangeRate),
      adminFullName: raw.adminFullName.trim(),
      adminUsername: raw.adminUsername.trim(),
      adminPassword: raw.adminPassword.trim(),
      deviceCode: raw.deviceCode.trim(),
      deviceName: raw.deviceName.trim()
    };

    this.setupApi.complete(request).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          // Save the deviceCode in local storage (bazarflow_device_code) via SessionService
          this.sessionService.setDeviceCode(request.deviceCode);
          // Warm up cache so LoginSetupGuard allows /login without re-checking the API
          this.setupApi.setCompletedCache(true);
          this.router.navigate(['/login']);
        } else {
          this.apiError = res.message || 'حدث خطأ أثناء إعداد النظام.';
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;
        this.apiError = this.mapApiError(err);
      }
    });
  }

  private mapApiError(err: HttpErrorResponse): string {
    const errorCode = err.error?.error as string | undefined;

    if (errorCode === 'SETUP_ALREADY_COMPLETED') {
      return 'لقد تم إعداد النظام بالفعل.';
    }
    if (errorCode === 'SETUP_STATE_AMBIGUOUS') {
      return 'يحتوي النظام على حسابات موظفين مسبقاً، لا يمكن إعادة التهيئة.';
    }
    if (errorCode === 'INVALID_ADMIN_PASSWORD') {
      return 'كلمة المرور غير آمنة أو مطابقة لاسم المستخدم.';
    }
    if (errorCode === 'INVALID_EXCHANGE_RATE') {
      return 'سعر الصرف يجب أن يكون أكبر من الصفر.';
    }
    if (errorCode === 'DEVICE_CODE_ALREADY_EXISTS') {
      return 'رمز الجهاز هذا مستخدم بالفعل في النظام.';
    }
    if (errorCode === 'DEFAULT_DEVICE_NOT_ALLOWED') {
      return 'لا يمكن استخدام DEFAULT_DEVICE كجهاز مخصص. أدخل رمز جهاز مختلف.';
    }
    if (errorCode === 'SETUP_VALIDATION_ERROR') {
      return 'حدث خطأ في التحقق من البيانات المدخلة.';
    }
    if (err.status === 0 || err.status === 500) {
      return 'تعذر الاتصال بالخادم.';
    }
    return 'حدث خطأ أثناء إعداد النظام.';
  }
}
