import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { FormErrorComponent } from '../../shared/components/form-helpers/form-error.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormErrorComponent],
  template: `
    <div class="min-h-screen bg-slate-100 flex items-center justify-center p-4">
      <div class="w-full max-w-md">

        <!-- App Title -->
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold text-primary tracking-tight">Bazarflow</h1>
          <p class="text-slate-500 mt-1 text-sm">نظام إدارة نقطة البيع</p>
        </div>

        <!-- Login Card -->
        <div class="card p-8">
          <h2 class="text-xl font-bold text-slate-800 mb-6 text-center">تسجيل الدخول</h2>

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
              {{ apiError }}
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
        </div>

      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  apiError: string | null = null;

  // deviceCode is read from stored session/device config, not user input
  private readonly deviceCode: string = 'DEFAULT_DEVICE';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authApiService: AuthApiService,
    private authService: AuthService,
    private sessionService: SessionService,
    private permissionsService: PermissionsService
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

    // Clear API error on any change
    this.loginForm.valueChanges.subscribe(() => {
      if (this.apiError) this.apiError = null;
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

    const { username, password } = this.loginForm.value;

    this.authApiService.login({
      username: username.trim(),
      password: password.trim(),
      deviceCode: this.deviceCode
    }).subscribe({
      next: (response) => {
        // 1. Store session ID (numeric from backend)
        this.sessionService.setSessionId(response.sessionId);

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

        // 5. Reset loading state
        this.isLoading = false;

        // 6. Navigate to Cashier (cashier-first system)
        this.router.navigate(['/cashier']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;
        this.apiError = this.mapApiError(err);
      }
    });
  }

  private mapApiError(err: HttpErrorResponse): string {
    const errorCode = err.error?.error as string | undefined;

    if (err.status === 400) {
      if (errorCode === 'INVALID_CREDENTIALS' || errorCode === 'EMPLOYEE_NOT_FOUND') {
        return 'اسم المستخدم أو كلمة المرور غير صحيحة';
      }
      if (errorCode === 'DEVICE_NOT_FOUND') {
        return 'الجهاز غير معرّف في النظام';
      }
      return 'اسم المستخدم أو كلمة المرور غير صحيحة';
    }

    if (err.status === 409 && errorCode === 'EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION') {
      return 'هذا الحساب لديه جلسة نشطة بالفعل. يرجى تسجيل الخروج أولاً';
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
