import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { BlackBoxRecorderService } from '../../core/services/black-box-recorder.service';
import { FormErrorComponent } from '../../shared/components/form-helpers/form-error.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormErrorComponent],
  template: `
    <div class="min-h-screen bg-slate-100 flex items-center justify-center p-4">
      <div class="w-full max-w-md">

        <!-- App Title -->
        <div class="text-center mb-8 flex flex-col items-center justify-center">
          <img src="assets/brand/bazarflow-icon.png" alt="BazarFlow Logo" class="h-16 w-16 mb-2 object-contain" />
          <h1 class="text-3xl font-bold text-primary tracking-tight">Bazarflow</h1>
          <p class="text-slate-500 mt-1 text-sm">نظام إدارة نقطة البيع</p>
        </div>

        <!-- Login Card -->
        <div class="card p-8">
          <h2 class="text-xl font-bold text-slate-800 mb-6 text-center">تسجيل الدخول</h2>

          <!-- Session Expired Warning Banner -->
          <div
            *ngIf="sessionExpiredMessage"
            id="session-expired-alert"
            class="mb-5 p-3 bg-amber-50 border border-amber-200 rounded-md text-sm text-amber-800 text-center font-medium leading-relaxed"
            role="alert"
          >
            {{ sessionExpiredMessage }}
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" autocomplete="off" novalidate>

            <!-- Username Field -->
            <div class="mb-5">
              <label for="username" class="block text-sm font-medium text-slate-700 mb-1">
                اسم المستخدم
              </label>
              <input
                id="username"
                type="text"
                formControlName="username"
                class="input-field"
                [class.border-red-400]="isFieldInvalid('username')"
                [attr.disabled]="isLoading ? true : null"
                placeholder="أدخل اسم المستخدم"
              />
              <app-form-error [show]="isFieldInvalid('username')">
                اسم المستخدم مطلوب
              </app-form-error>
            </div>

            <!-- Password Field -->
            <div class="mb-6">
              <label for="password" class="block text-sm font-medium text-slate-700 mb-1">
                كلمة المرور
              </label>
              <input
                id="password"
                type="password"
                formControlName="password"
                class="input-field"
                [class.border-red-400]="isFieldInvalid('password')"
                [attr.disabled]="isLoading ? true : null"
                placeholder="أدخل كلمة المرور"
              />
              <app-form-error [show]="isFieldInvalid('password')">
                كلمة المرور مطلوبة
              </app-form-error>
            </div>

            <!-- API Error Message -->
            <div
              *ngIf="apiError"
              class="mb-5 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 text-center"
              role="alert"
            >
              <div>{{ apiError }}</div>
            </div>

            <!-- Submit Button -->
            <button
              id="login-submit-btn"
              type="submit"
              class="btn-primary w-full flex items-center justify-center gap-2"
              [disabled]="isLoading || loginForm.invalid"
            >
              <svg *ngIf="isLoading" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span>{{ isLoading ? 'جاري الدخول...' : 'تسجيل الدخول' }}</span>
            </button>

          </form>

          <!-- Default Device Warning Banner -->
          <div
            *ngIf="deviceCode === 'DEFAULT_DEVICE'"
            id="default-device-warning"
            class="mt-5 p-3 bg-amber-50 border border-amber-200 rounded-md text-xs text-amber-800 text-center leading-relaxed"
            role="alert"
          >
            أنت تستخدم الجهاز الافتراضي العام. يوصى بإنشاء جهاز مخصص من الإعدادات لضمان دقة التقارير والأمان.
          </div>

          <!-- Device Info -->
          <div class="mt-6 pt-4 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
            <div>
              <span>الجهاز الحالي: </span>
              <span class="font-semibold text-slate-700">{{ deviceCode }}</span>
              <span *ngIf="deviceCode === 'DEFAULT_DEVICE'"> (افتراضي)</span>
            </div>
            <button
              type="button"
              (click)="openDeviceModal()"
              class="text-primary hover:underline font-semibold"
            >
              تغيير الجهاز
            </button>
          </div>
        </div>

        <!-- Developer Attribution -->
        <div class="text-center mt-6 text-xs text-slate-400 font-medium">
          <div>المهندس: أياز مراد</div>
          <div class="mt-0.5 tracking-wide">Engineer / Developer: Ayaz Murad</div>
        </div>

      </div>
    </div>

    <!-- Device Selection Modal -->
    <div
      *ngIf="showDeviceModal"
      class="fixed inset-0 bg-slate-900/50 flex items-center justify-center p-4 z-50"
      role="dialog"
      aria-modal="true"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-sm w-full p-6 relative">
        <h3 class="text-lg font-bold text-slate-800 mb-4">تغيير رمز الجهاز</h3>
        <p class="text-xs text-slate-500 mb-4 leading-relaxed">
          أدخل رمز الجهاز الجديد الذي ترغب في ربطه بجلسة العمل الحالية.
        </p>
        
        <div class="mb-6">
          <label for="deviceCodeInput" class="block text-xs font-semibold text-slate-600 mb-2">
            رمز الجهاز
          </label>
          <input
            id="deviceCodeInput"
            #deviceInput
            type="text"
            [value]="deviceCode === 'DEFAULT_DEVICE' ? '' : deviceCode"
            class="input-field w-full"
            placeholder="مثال: DEV-102"
            autocomplete="off"
          />
        </div>

        <div class="flex items-center justify-end gap-2">
          <button
            type="button"
            (click)="showDeviceModal = false"
            class="btn-secondary"
          >
            إلغاء
          </button>
          <button
            type="button"
            (click)="saveDeviceCode(deviceInput.value)"
            class="btn-primary"
          >
            حفظ الرمز
          </button>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  apiError: string | null = null;
  showDeviceModal = false;
  lastErrorCode: string | null = null;
  sessionExpiredMessage: string | null = null;

  get deviceCode(): string {
    return this.sessionService.getDeviceCode() || 'DEFAULT_DEVICE';
  }

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authApiService: AuthApiService,
    private authService: AuthService,
    private sessionService: SessionService,
    private permissionsService: PermissionsService,
    private blackBox: BlackBoxRecorderService
  ) {}

  ngOnInit(): void {
    // If already authenticated, redirect to /cashier immediately
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/cashier']);
      return;
    }

    this.loginForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(1)]],
      password: ['', [Validators.required, Validators.minLength(1)]]
    });

    // Clear API error and session expired message on any change
    this.loginForm.valueChanges.subscribe(() => {
      if (this.apiError) {
        this.apiError = null;
        this.lastErrorCode = null;
      }
      if (this.sessionExpiredMessage) {
        this.sessionExpiredMessage = null;
      }
    });

    // Read query parameters to check if session expired
    this.route.queryParams.subscribe(params => {
      if (params['reason'] === 'session-expired') {
        this.sessionExpiredMessage = 'انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى';

        // Clear query parameters from URL without adding to browser history
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { reason: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });
      }
    });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.loginForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onSubmit(): void {
    // Mark all touched to trigger validation messages
    this.loginForm.markAllAsTouched();

    if (this.loginForm.invalid || this.isLoading) return;

    this.isLoading = true;
    this.apiError = null;
    this.sessionExpiredMessage = null;

    const { username, password } = this.loginForm.value;

    this.authApiService.login({
      username: username.trim(),
      password: password.trim(),
      deviceCode: this.deviceCode
    }).subscribe({
      next: (response) => {
        // 1. Store session ID (numeric from backend)
        this.sessionService.setSessionId(response.sessionId);
        this.sessionService.setSessionToken(response.sessionToken);

        // 2. Store employee basics
        this.sessionService.setEmployee({
          employeeId: response.employeeId,
          fullName: response.fullName,
          deviceCode: response.deviceCode
        });

        // 3. Set permissions from Login Response directly (no separate GET /permissions call)
        this.permissionsService.setPermissions(response.allowedScreenKeys);

        // 4. Mark authenticated
        this.authService.setAuthenticated(true);

        this.blackBox.recordSuccess('LOGIN_SUCCESS', {
          pageName: 'Login',
          entityType: 'Employee',
          entityId: response.employeeId,
          metadata: {
            username: username.trim(),
            deviceCode: response.deviceCode
          }
        });

        // 5. Reset loading state
        this.isLoading = false;

        // 6. Navigate to Cashier (cashier-first system)
        this.router.navigate(['/cashier']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;
        this.lastErrorCode = err.error?.error as string | null;
        this.apiError = this.mapApiError(err);

        if (this.lastErrorCode === 'SETUP_REQUIRED') {
          this.router.navigate(['/setup']);
        }

        if (this.lastErrorCode === 'DEFAULT_DEVICE_NOT_ALLOWED') {
          this.showDeviceModal = true;
        }
      }
    });
  }

  openDeviceModal(): void {
    this.showDeviceModal = true;
  }

  saveDeviceCode(code: string): void {
    const trimmed = code.trim();
    if (trimmed) {
      this.sessionService.setDeviceCode(trimmed);
    }
    this.showDeviceModal = false;
  }

  private mapApiError(err: HttpErrorResponse): string {
    const errorCode = err.error?.error as string | undefined;

    if (err.status === 429 || errorCode === 'LOGIN_THROTTLED') {
      return 'تم تجاوز عدد المحاولات. حاول بعد قليل.';
    }

    if (err.status === 401 || errorCode === 'LOGIN_FAILED') {
      return 'تعذر تسجيل الدخول. تحقق من البيانات أو الجهاز.';
    }

    if (err.status === 409 && errorCode === 'EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION') {
      return 'هذا الحساب لديه جلسة نشطة بالفعل. يرجى تسجيل الخروج أولاً';
    }

    if (errorCode === 'SETUP_REQUIRED') {
      return 'النظام يحتاج إلى الإعداد الأول.';
    }

    if (errorCode === 'DEFAULT_DEVICE_NOT_ALLOWED') {
      return 'لا يمكن استخدام الجهاز الافتراضي بعد إعداد النظام. يرجى اختيار جهاز مخصص.';
    }

    if (err.status === 403) {
      return 'هذا الحساب غير مفعّل أو ممنوع من الوصول';
    }

    if (err.status === 500 || err.status === 0) {
      return 'حدث خطأ في الاتصال بالخادم، يرجى المحاولة لاحقاً';
    }

    return 'حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى';
  }
}
