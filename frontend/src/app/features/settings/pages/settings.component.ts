import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SessionService } from '../../../core/services/session.service';
import { BlackBoxRecorderService } from '../../../core/services/black-box-recorder.service';
import { SettingsApiService } from '../services/settings-api.service';
import { EmployeeListItem, PermissionEntry, CategoryItem, PublicSettingsResponse, CreateBackupResponse, AuditLogListItem, AuditLogDetailResponse, AuditLogStatusResponse, PosDeviceListItem, PosDeviceDetailsResponse, ActiveSessionResponse } from '../models/settings.model';

type TabType = 'employees' | 'categories' | 'store' | 'backup' | 'audit' | 'devices' | 'sessions';
type ModalType = 'empCreate' | 'empEdit' | 'empDelete' | 'empReset' | 'catCreate' | 'catEdit' | 'catDelete' | 'auditDetail' | 'deviceCreate' | 'deviceEdit' | 'deviceDelete' | 'sessionConfirmClose' | null;

const ALL_SCREENS = ['Sales','Products','Invoices','Offers','Reports','Inventory','Settings','Purchases'];

const ERR: Record<string,string> = {
  USERNAME_ALREADY_EXISTS: 'اسم المستخدم موجود مسبقًا',
  EMPLOYEE_NOT_FOUND: 'الموظف غير موجود',
  CANNOT_DELETE_SELF: 'لا يمكنك حذف حسابك الحالي',
  CATEGORY_NAME_ALREADY_EXISTS: 'اسم التصنيف موجود مسبقًا',
  CATEGORY_NOT_FOUND: 'التصنيف غير موجود',
  INVALID_PASSWORD: 'كلمة المرور يجب أن تكون 6 أحرف على الأقل',
  BACKUP_PATH_NOT_ACCESSIBLE: 'مسار النسخ الاحتياطي غير قابل للكتابة',
  BACKUP_SQL_FAILED: 'فشل تنفيذ النسخ الاحتياطي في SQL Server',
  BACKUP_DIRECTORY_NOT_CONFIGURED: 'مسار النسخ الاحتياطي غير مهيأ',
  DEVICE_CODE_REQUIRED: 'رمز الجهاز مطلوب',
  DEVICE_NAME_REQUIRED: 'اسم الجهاز مطلوب',
  DEVICE_CODE_ALREADY_EXISTS: 'رمز الجهاز هذا مسجل مسبقًا',
  DEVICE_NOT_FOUND: 'الجهاز غير موجود',
  CANNOT_DISABLE_LAST_ACTIVE_DEVICE: 'لا يمكن تعطيل آخر جهاز نشط',
  LAST_ACTIVE_DEVICE: 'لا يمكن تعطيل آخر جهاز نشط',
  CANNOT_DELETE_DEFAULT_DEVICE: 'لا يمكن حذف جهاز النظام الافتراضي',
  SESSION_NOT_FOUND: 'الجلسة غير موجودة',
  SESSION_NOT_ACTIVE: 'الجلسة ليست نشطة',
  NO_ACTIVE_SESSION: 'لا توجد جلسة نشطة',
};

function mapErr(e: HttpErrorResponse): string {
  return ERR[e?.error?.error] ?? 'حدث خطأ أثناء تنفيذ العملية';
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
<div class="h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
  <!-- Header + Tabs -->
  <div class="bg-white border-b border-slate-200 px-6 pt-6 pb-0 shrink-0">
    <h1 class="text-2xl font-bold text-slate-800 mb-4">الإعدادات</h1>
    <div class="flex gap-1">
      <button *ngFor="let t of tabs" (click)="switchTab(t.id)"
        class="px-5 py-2 text-sm font-medium rounded-t-lg transition-colors"
        [class.bg-primary]="activeTab===t.id" [class.text-white]="activeTab===t.id"
        [class.bg-slate-100]="activeTab!==t.id" [class.text-slate-600]="activeTab!==t.id">
        {{t.label}}
      </button>
    </div>
  </div>

  <!-- Content -->
  <div class="flex-1 overflow-auto p-6">
    <!-- Loading -->
    <div *ngIf="loading" class="flex items-center justify-center py-16">
      <div class="w-10 h-10 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
    </div>

    <ng-container *ngIf="!loading">
      <!-- EMPLOYEES TAB -->
      <div *ngIf="activeTab==='employees'">
        <div class="flex justify-between items-center mb-4">
          <p class="text-sm text-slate-500">إدارة الموظفين وصلاحياتهم</p>
          <button (click)="openEmpCreate()" class="px-4 py-2 bg-primary text-white rounded-md text-sm font-medium">+ إضافة موظف</button>
        </div>
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table class="w-full text-sm text-right">
            <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs">
              <tr>
                <th class="px-4 py-3">الاسم</th><th class="px-4 py-3">اسم المستخدم</th>
                <th class="px-4 py-3">الهاتف</th><th class="px-4 py-3 text-center">الحالة</th>
                <th class="px-4 py-3 text-center">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="!employees.length"><td colspan="5" class="py-8 text-center text-slate-400">لا يوجد موظفون مسجلون</td></tr>
              <tr *ngFor="let e of employees" class="hover:bg-slate-50">
                <td class="px-4 py-3 font-medium">{{e.fullName}}</td>
                <td class="px-4 py-3 text-slate-500">{{e.username}}</td>
                <td class="px-4 py-3 text-slate-500">{{e.phone||'—'}}</td>
                <td class="px-4 py-3 text-center">
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium" [class.bg-green-100]="e.isActive" [class.text-green-800]="e.isActive" [class.bg-slate-100]="!e.isActive" [class.text-slate-600]="!e.isActive">{{e.isActive?'نشط':'معطّل'}}</span>
                </td>
                <td class="px-4 py-3 text-center">
                  <div class="flex justify-center gap-2">
                    <button (click)="openEmpEdit(e)" class="text-slate-400 hover:text-primary" title="تعديل">✏️</button>
                    <button (click)="openEmpReset(e)" class="text-slate-400 hover:text-blue-500" title="إعادة كلمة المرور">🔑</button>
                    <button (click)="openEmpDelete(e)" class="text-slate-400 hover:text-red-600" title="حذف">🗑️</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- CATEGORIES TAB -->
      <div *ngIf="activeTab==='categories'">
        <div class="flex justify-between items-center mb-4">
          <p class="text-sm text-slate-500">إدارة تصنيفات المنتجات</p>
          <button (click)="openCatCreate()" class="px-4 py-2 bg-primary text-white rounded-md text-sm font-medium">+ إضافة تصنيف</button>
        </div>
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table class="w-full text-sm text-right">
            <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs">
              <tr><th class="px-4 py-3">الاسم</th><th class="px-4 py-3 text-center">الحالة</th><th class="px-4 py-3 text-center">إجراءات</th></tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="!categories.length"><td colspan="3" class="py-8 text-center text-slate-400">لا توجد تصنيفات</td></tr>
              <tr *ngFor="let c of categories" class="hover:bg-slate-50">
                <td class="px-4 py-3 font-medium">{{c.name}}</td>
                <td class="px-4 py-3 text-center">
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium" [class.bg-green-100]="c.isActive" [class.text-green-800]="c.isActive" [class.bg-slate-100]="!c.isActive" [class.text-slate-600]="!c.isActive">{{c.isActive?'نشط':'معطّل'}}</span>
                </td>
                <td class="px-4 py-3 text-center">
                  <div class="flex justify-center gap-2">
                    <button (click)="openCatEdit(c)" class="text-slate-400 hover:text-primary">✏️</button>
                    <button (click)="openCatDelete(c)" class="text-slate-400 hover:text-red-600">🗑️</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- STORE TAB -->
      <div *ngIf="activeTab==='store'" class="space-y-6">
        <div class="max-w-md bg-white rounded-xl border border-slate-200 shadow-sm p-6 space-y-4">
          <h2 class="font-bold text-slate-700">معلومات المتجر</h2>
          <div><span class="text-xs text-slate-500 block mb-1">اسم المتجر</span><p class="font-medium text-slate-800">{{storeSettings?.storeName||'—'}}</p></div>
          <div><span class="text-xs text-slate-500 block mb-1">سعر الصرف</span><p class="font-medium text-slate-800">1 USD = {{storeSettings?.exchangeRate||0}} SYP</p></div>
          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 text-amber-800 text-sm">⚠️ تعديل اسم المتجر وسعر الصرف غير متاح في هذه النسخة.</div>
        </div>

        <div class="max-w-md bg-white rounded-xl border border-slate-200 shadow-sm p-6 space-y-4 flex flex-col items-center text-center">
          <img src="assets/brand/bazarflow-icon.png" alt="BazarFlow Logo" class="h-16 w-16 mb-2 object-contain" />
          <h2 class="font-bold text-slate-800 text-lg">BazarFlow</h2>
          <div class="w-full border-t border-slate-100 my-2"></div>
          <div class="text-sm text-slate-600 space-y-1">
            <div class="font-medium">المهندس: أياز مراد</div>
            <div class="font-sans text-xs text-slate-500">Engineer / Developer: Ayaz Murad</div>
          </div>
        </div>
      </div>

      <!-- BACKUP TAB -->
      <div *ngIf="activeTab==='backup'">
        <div class="max-w-2xl bg-white rounded-xl border border-slate-200 shadow-sm p-6 space-y-5">
          <div>
            <h2 class="font-bold text-slate-800 mb-1">النسخ الاحتياطي</h2>
            <p class="text-sm text-slate-500">إنشاء نسخة احتياطية محلية من قاعدة بيانات SQL Server.</p>
          </div>

          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 text-amber-800 text-sm">
            النسخة الاحتياطية تحتوي بيانات حساسة مثل الفواتير والموظفين والصلاحيات. احفظ الملف في مكان آمن ولا تشاركه إلا مع شخص مخول.
          </div>

          <div class="flex flex-wrap items-center gap-3">
            <button (click)="createBackup()" [disabled]="backupLoading"
              class="px-4 py-2 bg-primary text-white rounded-md text-sm font-medium disabled:opacity-50">
              {{ backupLoading ? 'جاري إنشاء النسخة...' : 'إنشاء نسخة احتياطية' }}
            </button>
            <span class="text-xs text-slate-500">لا يوجد استرجاع أو تنزيل مباشر في هذه النسخة.</span>
          </div>

          <div *ngIf="backupError" class="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm">
            {{ backupError }}
          </div>

          <div *ngIf="backupResult" class="border border-green-200 bg-green-50 rounded-lg p-4 text-sm text-green-900 space-y-2">
            <div class="font-bold">تم إنشاء النسخة الاحتياطية بنجاح</div>
            <div><span class="text-green-700">اسم الملف:</span> {{ backupResult.fileName }}</div>
            <div><span class="text-green-700">وقت الإنشاء:</span> {{ backupResult.createdAt | date:'medium' }}</div>
            <div><span class="text-green-700">الحجم:</span> {{ formatBytes(backupResult.sizeBytes) }}</div>
            <div><span class="text-green-700">المجلد:</span> {{ backupResult.backupDirectory }}</div>
          </div>
        </div>
      </div>

      <!-- AUDIT LOG TAB -->
      <div *ngIf="activeTab==='audit'" class="space-y-4">
        <!-- Status and Retention Card -->
        <div *ngIf="auditStatus" class="bg-white rounded-xl border border-slate-200 shadow-sm p-4 space-y-4">
          <div class="flex items-center justify-between pb-2 border-b border-slate-100">
            <h3 class="font-bold text-slate-800 text-sm">حالة وتخزين سجلات النشاط</h3>
            <span class="px-2 py-0.5 text-[10px] font-semibold bg-blue-50 text-blue-600 rounded-full border border-blue-100">
              سياسة الاحتفاظ بالبيانات
            </span>
          </div>

          <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-4">
            <!-- Total Count -->
            <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
              <div class="text-[10px] font-medium text-slate-400">إجمالي سجلات النشاط</div>
              <div class="text-lg font-bold text-slate-800 mt-1">{{ auditStatus.totalCount | number }}</div>
            </div>

            <!-- Oldest Record -->
            <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
              <div class="text-[10px] font-medium text-slate-400">تاريخ أقدم سجل</div>
              <div class="text-xs font-bold text-slate-700 mt-2">
                {{ auditStatus.oldestCreatedAt ? (auditStatus.oldestCreatedAt | date:'yyyy-MM-dd HH:mm') : '—' }}
              </div>
            </div>

            <!-- Newest Record -->
            <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
              <div class="text-[10px] font-medium text-slate-400">تاريخ أحدث سجل</div>
              <div class="text-xs font-bold text-slate-700 mt-2">
                {{ auditStatus.newestCreatedAt ? (auditStatus.newestCreatedAt | date:'yyyy-MM-dd HH:mm') : '—' }}
              </div>
            </div>

            <!-- Large JSON Count -->
            <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
              <div class="text-[10px] font-medium text-slate-400">سجلات ذات حجم كبير (JSON)</div>
              <div class="text-lg font-bold text-slate-800 mt-1">{{ auditStatus.approximateLargeJsonCount | number }}</div>
            </div>

            <!-- Retention & Cleanup -->
            <div class="bg-slate-50 rounded-lg p-3 border border-slate-100 col-span-2 md:col-span-1">
              <div class="text-[10px] font-medium text-slate-400">فترة الاحتفاظ الموصى بها</div>
              <div class="text-xs font-bold text-slate-700 mt-2 flex flex-col gap-0.5">
                <span>{{ auditStatus.recommendedRetentionDays }} يومًا</span>
                <span class="text-[10px] text-slate-500 font-normal">
                  التنظيف التلقائي: <strong class="text-red-500">غير مفعّل</strong>
                </span>
              </div>
            </div>
          </div>

          <!-- Warning Message -->
          <div class="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-lg p-3 text-xs text-amber-800">
            <svg class="w-5.5 h-5.5 text-amber-600 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div class="leading-relaxed">
              <strong class="font-bold">تنبيه أمان البيانات:</strong>
              التنظيف التلقائي غير مفعّل في هذه النسخة. لا تحذف السجلات إلا بعد عمل Backup كامل للنظام.
            </div>
          </div>
        </div>

        <!-- Filters Card -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-4 space-y-3">
          <div class="flex items-center justify-between pb-2 border-b border-slate-100">
            <h3 class="font-bold text-slate-800 text-sm">تصفية سجل النشاط</h3>
            <button (click)="resetAuditFilters()" class="text-xs text-primary hover:underline font-medium">مسح الفلاتر</button>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-3 text-xs">
            <div>
              <label class="block text-slate-500 mb-1">العملية</label>
              <input type="text" [(ngModel)]="filterAction" placeholder="مثال: CREATE" class="w-full px-3 py-1.5 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
            </div>
            <div>
              <label class="block text-slate-500 mb-1">نوع الكيان</label>
              <input type="text" [(ngModel)]="filterEntityType" placeholder="مثال: Product" class="w-full px-3 py-1.5 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
            </div>
            <div>
              <label class="block text-slate-500 mb-1">رقم الموظف</label>
              <input type="number" [(ngModel)]="filterEmployeeId" placeholder="مثال: 1" class="w-full px-3 py-1.5 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
            </div>
            <div>
              <label class="block text-slate-500 mb-1">من تاريخ</label>
              <input type="date" [(ngModel)]="filterDateFrom" class="w-full px-3 py-1.5 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
            </div>
            <div>
              <label class="block text-slate-500 mb-1">إلى تاريخ</label>
              <input type="date" [(ngModel)]="filterDateTo" class="w-full px-3 py-1.5 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
            </div>
          </div>
          <div class="flex justify-end pt-1">
            <button (click)="loadAuditLogs()" class="px-4 py-1.5 bg-primary text-white rounded-md text-xs font-medium hover:bg-primary-dark transition duration-200">تحديث</button>
          </div>
        </div>

        <!-- Table Card -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-right border-collapse text-xs">
              <thead>
                <tr class="bg-slate-50 text-slate-500 border-b border-slate-200">
                  <th class="p-3 font-semibold">التاريخ</th>
                  <th class="p-3 font-semibold">الموظف</th>
                  <th class="p-3 font-semibold">العملية</th>
                  <th class="p-3 font-semibold">الكيان</th>
                  <th class="p-3 font-semibold">اسم/رقم الكيان</th>
                  <th class="p-3 font-semibold text-left">التفاصيل</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-slate-700">
                <tr *ngFor="let item of auditLogs" class="hover:bg-slate-50 transition duration-150">
                  <td class="p-3 whitespace-nowrap">{{ item.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                  <td class="p-3 font-medium">{{ item.employeeName || '—' }} (رقم: {{ item.employeeId || '—' }})</td>
                  <td class="p-3">
                    <span class="px-2 py-0.5 rounded-full font-medium"
                      [class.bg-green-50]="item.action === 'CREATE'" [class.text-green-700]="item.action === 'CREATE'"
                      [class.bg-blue-50]="item.action === 'UPDATE'" [class.text-blue-700]="item.action === 'UPDATE'"
                      [class.bg-red-50]="item.action === 'DELETE'" [class.text-red-700]="item.action === 'DELETE'">
                      {{ translateAction(item.action) }}
                    </span>
                  </td>
                  <td class="p-3">{{ item.entityType }}</td>
                  <td class="p-3 font-mono text-slate-600">{{ item.entityDisplayName || item.entityId || '—' }}</td>
                  <td class="p-3 text-left">
                    <button (click)="openAuditDetail(item.id)" class="px-2.5 py-1 text-primary hover:bg-slate-100 rounded transition duration-200">تفاصيل</button>
                  </td>
                </tr>
                <tr *ngIf="auditLogs.length === 0">
                  <td colspan="6" class="p-6 text-center text-slate-400">لا توجد سجلات مطابقة للفلاتر المحددة.</td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination Bar -->
          <div class="px-4 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
            <div>
              <span>عرض الصفحة {{ auditPage }} (إجمالي السجلات: {{ auditTotalCount }})</span>
            </div>
            <div class="flex items-center gap-2">
              <button (click)="changeAuditPage(-1)" [disabled]="auditPage <= 1" class="px-2.5 py-1 border border-slate-200 bg-white rounded shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">السابق</button>
              <button (click)="changeAuditPage(1)" [disabled]="auditPage * auditPageSize >= auditTotalCount" class="px-2.5 py-1 border border-slate-200 bg-white rounded shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">التالي</button>
            </div>
          </div>
        </div>
      </div>

      <!-- DEVICES TAB -->
      <div *ngIf="activeTab==='devices'">
        <div class="flex justify-between items-center mb-4">
          <p class="text-sm text-slate-500">إدارة الأجهزة ونقاط البيع المصرح لها بالدخول</p>
          <button (click)="openDeviceCreate()" class="px-4 py-2 bg-primary text-white rounded-md text-sm font-medium">+ إضافة جهاز</button>
        </div>
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table class="w-full text-sm text-right">
            <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs">
              <tr>
                <th class="px-4 py-3">اسم الجهاز</th>
                <th class="px-4 py-3">رمز الجهاز</th>
                <th class="px-4 py-3 text-center">الحالة</th>
                <th class="px-4 py-3">آخر تسجيل دخول</th>
                <th class="px-4 py-3">ملاحظات</th>
                <th class="px-4 py-3 text-center">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="!devices.length"><td colspan="6" class="py-8 text-center text-slate-400">لا توجد أجهزة مسجلة</td></tr>
              <tr *ngFor="let d of devices" class="hover:bg-slate-50">
                <td class="px-4 py-3 font-medium">
                  {{d.deviceName}}
                  <span *ngIf="d.deviceCode==='DEFAULT_DEVICE'" class="mr-2 px-2 py-0.5 bg-blue-50 text-blue-700 border border-blue-100 rounded text-[10px] font-semibold">جهاز النظام الافتراضي</span>
                </td>
                <td class="px-4 py-3 font-mono text-slate-500">
                  <div class="flex items-center gap-2">
                    <span>{{d.deviceCode}}</span>
                    <button (click)="copyDeviceCode(d.deviceCode)" class="text-slate-400 hover:text-primary" title="نسخ الرمز">📋</button>
                  </div>
                </td>
                <td class="px-4 py-3 text-center">
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium" [class.bg-green-100]="d.isActive" [class.text-green-800]="d.isActive" [class.bg-slate-100]="!d.isActive" [class.text-slate-600]="!d.isActive">{{d.isActive?'نشط':'معطّل'}}</span>
                </td>
                <td class="px-4 py-3 text-slate-500">{{(d.lastLoginAt | date:'yyyy-MM-dd HH:mm:ss') || '—'}}</td>
                <td class="px-4 py-3 text-slate-500">{{d.notes||'—'}}</td>
                <td class="px-4 py-3 text-center">
                  <div class="flex justify-center gap-2">
                    <button (click)="openDeviceEdit(d)" class="text-slate-400 hover:text-primary" title="تعديل">✏️</button>
                    <button (click)="toggleDeviceActive(d)" class="text-slate-400 hover:text-blue-500" [title]="d.isActive?'تعطيل':'تفعيل'">
                      {{d.isActive?'🚫':'✅'}}
                    </button>
                    <button (click)="openDeviceDelete(d)" class="text-slate-400 hover:text-red-600" title="حذف" [disabled]="d.deviceCode==='DEFAULT_DEVICE'">🗑️</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- SESSIONS TAB -->
      <div *ngIf="activeTab==='sessions'">
        <div class="flex justify-between items-center mb-4">
          <p class="text-sm text-slate-500">إدارة وإغلاق جلسات الموظفين النشطة في النظام</p>
          <button (click)="loadActiveSessions()" class="px-4 py-2 bg-primary text-white rounded-md text-sm font-medium">🔄 تحديث</button>
        </div>
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table class="w-full text-sm text-right">
            <thead class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs">
              <tr>
                <th class="px-4 py-3">الموظف</th>
                <th class="px-4 py-3">اسم المستخدم</th>
                <th class="px-4 py-3">الجهاز</th>
                <th class="px-4 py-3 text-center">وقت البدء</th>
                <th class="px-4 py-3 text-center">آخر نشاط</th>
                <th class="px-4 py-3 text-center">انتهاء الصلاحية</th>
                <th class="px-4 py-3 text-center">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="!activeSessions.length"><td colspan="7" class="py-8 text-center text-slate-400">لا توجد جلسات نشطة حالياً</td></tr>
              <tr *ngFor="let s of activeSessions" class="hover:bg-slate-50" [class.bg-blue-50]="isCurrentSession(s.sessionId)">
                <td class="px-4 py-3 font-medium">
                  {{s.employeeName}}
                  <span *ngIf="isCurrentSession(s.sessionId)" class="mr-2 px-2 py-0.5 bg-blue-100 text-blue-800 rounded-full text-xs font-semibold">جلستك الحالية</span>
                </td>
                <td class="px-4 py-3 text-slate-500">{{s.username}}</td>
                <td class="px-4 py-3 text-slate-500">
                  {{s.deviceName}} <span class="text-xs text-slate-400">({{s.deviceCode}})</span>
                </td>
                <td class="px-4 py-3 text-center text-slate-500">{{s.startedAt | date:'yyyy-MM-dd HH:mm:ss'}}</td>
                <td class="px-4 py-3 text-center text-slate-500">{{s.lastSeenAt ? (s.lastSeenAt | date:'yyyy-MM-dd HH:mm:ss') : '—'}}</td>
                <td class="px-4 py-3 text-center text-slate-500">{{s.expiresAt ? (s.expiresAt | date:'yyyy-MM-dd HH:mm:ss') : '—'}}</td>
                <td class="px-4 py-3 text-center">
                  <button (click)="openSessionCloseConfirm(s)" class="px-3 py-1 bg-red-50 hover:bg-red-100 text-red-600 border border-red-200 rounded-md text-xs font-medium transition duration-150">إغلاق الجلسة</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </ng-container>
  </div>

  <!-- MODALS -->
  <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-50 flex items-center justify-center p-4">
    
    <!-- Employee Create/Edit -->
    <div *ngIf="activeModal==='empCreate'||activeModal==='empEdit'" class="bg-white rounded-xl shadow-xl w-full max-w-lg flex flex-col max-h-[90vh]">
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800">{{activeModal==='empCreate'?'إضافة موظف':'تعديل موظف'}}</div>
      <div class="p-5 overflow-y-auto flex-1 space-y-3 text-sm">
        <div *ngIf="formErr" class="bg-red-50 text-red-600 p-3 rounded-md">{{formErr}}</div>
        <div><label class="block text-slate-600 mb-1">الاسم الكامل *</label><input type="text" [(ngModel)]="empForm.fullName" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary"></div>
        <div *ngIf="activeModal==='empCreate'"><label class="block text-slate-600 mb-1">اسم المستخدم *</label><input type="text" [(ngModel)]="empForm.username" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary"></div>
        <div><label class="block text-slate-600 mb-1">الهاتف</label><input type="text" [(ngModel)]="empForm.phone" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary"></div>
        <div *ngIf="activeModal==='empCreate'"><label class="block text-slate-600 mb-1">كلمة المرور *</label><input type="password" [(ngModel)]="empForm.password" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary"></div>
        <div *ngIf="activeModal==='empEdit'" class="flex items-center gap-2"><input type="checkbox" id="empActive" [(ngModel)]="empForm.isActive" class="rounded"><label for="empActive" class="text-slate-700">الموظف نشط</label></div>
        <div class="border-t border-slate-100 pt-3">
          <p class="font-medium text-slate-700 mb-2">الصلاحيات</p>
          <div class="grid grid-cols-2 gap-2">
            <label *ngFor="let s of allScreens" class="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" [checked]="getPermission(s)" (change)="togglePermission(s)" class="rounded border-slate-300">
              <span class="text-sm text-slate-700">{{s}}</span>
            </label>
          </div>
        </div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="saveEmployee()" [disabled]="saving" class="px-4 py-2 bg-primary text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري الحفظ...':'حفظ'}}</button>
      </div>
    </div>

    <!-- Employee Reset Password -->
    <div *ngIf="activeModal==='empReset'" class="bg-white rounded-xl shadow-xl w-full max-w-sm flex flex-col">
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800">إعادة تعيين كلمة المرور</div>
      <div class="p-5 space-y-3 text-sm">
        <div *ngIf="formErr" class="bg-red-50 text-red-600 p-3 rounded-md">{{formErr}}</div>
        <p class="text-slate-600">الموظف: <strong>{{actionEmp?.fullName}}</strong></p>
        <div><label class="block text-slate-600 mb-1">كلمة المرور الجديدة *</label><input type="password" [(ngModel)]="resetPwd" class="w-full px-3 py-2 border border-slate-300 rounded-md"></div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="confirmResetPassword()" [disabled]="saving||!resetPwd" class="px-4 py-2 bg-blue-600 text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري...':'تأكيد'}}</button>
      </div>
    </div>

    <!-- Employee Delete -->
    <div *ngIf="activeModal==='empDelete'" class="bg-white rounded-xl shadow-xl w-full max-w-sm flex flex-col">
      <div class="px-5 py-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد حذف الموظف</div>
      <div class="p-5 text-sm text-slate-700">هل أنت متأكد من حذف الموظف "{{actionEmp?.fullName}}"؟ قد يتم تعطيله بدلاً من الحذف.</div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="confirmDeleteEmployee()" [disabled]="saving" class="px-4 py-2 bg-red-600 text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري...':'حذف'}}</button>
      </div>
    </div>

    <!-- Category Create/Edit -->
    <div *ngIf="activeModal==='catCreate'||activeModal==='catEdit'" class="bg-white rounded-xl shadow-xl w-full max-w-sm flex flex-col">
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800">{{activeModal==='catCreate'?'إضافة تصنيف':'تعديل تصنيف'}}</div>
      <div class="p-5 space-y-3 text-sm">
        <div *ngIf="formErr" class="bg-red-50 text-red-600 p-3 rounded-md">{{formErr}}</div>
        <div><label class="block text-slate-600 mb-1">الاسم *</label><input type="text" [(ngModel)]="catForm.name" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary"></div>
        <div *ngIf="activeModal==='catEdit'" class="flex items-center gap-2"><input type="checkbox" id="catActive" [(ngModel)]="catForm.isActive" class="rounded"><label for="catActive" class="text-slate-700">التصنيف نشط</label></div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="saveCategory()" [disabled]="saving||!catForm.name" class="px-4 py-2 bg-primary text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري الحفظ...':'حفظ'}}</button>
      </div>
    </div>

    <!-- Category Delete -->
    <div *ngIf="activeModal==='catDelete'" class="bg-white rounded-xl shadow-xl w-full max-w-sm flex flex-col">
      <div class="px-5 py-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد حذف التصنيف</div>
      <div class="p-5 text-sm text-slate-700">هل أنت متأكد من حذف التصنيف "{{actionCat?.name}}"؟ قد يتم تعطيله بدلاً من الحذف.</div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="confirmDeleteCategory()" [disabled]="saving" class="px-4 py-2 bg-red-600 text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري...':'حذف'}}</button>
      </div>
    </div>

    <!-- Audit Detail Modal -->
    <div *ngIf="activeModal==='auditDetail'" class="bg-white rounded-xl shadow-xl w-full max-w-3xl flex flex-col max-h-[90vh]">
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800 flex justify-between items-center">
        <span>تفاصيل سجل النشاط #{{selectedAuditLog?.id}}</span>
        <button (click)="closeModal()" class="text-slate-400 hover:text-slate-600 text-lg">✕</button>
      </div>
      <div class="p-5 overflow-y-auto flex-1 space-y-4 text-xs">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 bg-slate-50 p-3 rounded-lg border border-slate-200 text-slate-700">
          <div><span class="text-slate-500 block mb-0.5">العملية</span><span class="font-bold text-slate-800">{{selectedAuditLog ? translateAction(selectedAuditLog.action) : '—'}}</span></div>
          <div><span class="text-slate-500 block mb-0.5">نوع الكيان</span><span class="font-medium text-slate-800">{{selectedAuditLog?.entityType || '—'}}</span></div>
          <div><span class="text-slate-500 block mb-0.5">اسم/رقم الكيان</span><span class="font-mono text-slate-850">{{selectedAuditLog?.entityDisplayName || selectedAuditLog?.entityId || '—'}}</span></div>
          <div><span class="text-slate-500 block mb-0.5">الموظف</span><span class="font-medium text-slate-800">{{selectedAuditLog?.employeeName || '—'}} (رقم: {{selectedAuditLog?.employeeId || '—'}})</span></div>
          <div><span class="text-slate-500 block mb-0.5">رقم الجلسة</span><span class="font-mono text-slate-800">{{selectedAuditLog?.sessionId || '—'}}</span></div>
          <div><span class="text-slate-500 block mb-0.5">التاريخ والوقت</span><span class="text-slate-800">{{selectedAuditLog?.createdAt | date:'yyyy-MM-dd HH:mm:ss'}}</span></div>
          <div><span class="text-slate-500 block mb-0.5">عنوان IP</span><span class="font-mono text-slate-800">{{selectedAuditLog?.ipAddress || '—'}}</span></div>
          <div class="md:col-span-2"><span class="text-slate-500 block mb-0.5">متصفح المستخدم (User Agent)</span><span class="font-mono text-slate-600">{{selectedAuditLog?.userAgent || '—'}}</span></div>
        </div>

        <div class="space-y-3">
          <div *ngIf="selectedAuditLog?.hasBefore">
            <h4 class="font-bold text-slate-700 mb-1">البيانات قبل التعديل:</h4>
            <pre class="bg-slate-950 text-emerald-400 p-3 rounded-md overflow-x-auto font-mono text-left" style="direction: ltr; unicode-bidi: embed;">{{ prettyJson(selectedAuditLog?.beforeJson) }}</pre>
          </div>
          <div *ngIf="selectedAuditLog?.hasAfter">
            <h4 class="font-bold text-slate-700 mb-1">البيانات بعد التعديل:</h4>
            <pre class="bg-slate-950 text-emerald-400 p-3 rounded-md overflow-x-auto font-mono text-left" style="direction: ltr; unicode-bidi: embed;">{{ prettyJson(selectedAuditLog?.afterJson) }}</pre>
          </div>
          <div *ngIf="selectedAuditLog?.hasMetadata">
            <h4 class="font-bold text-slate-700 mb-1">بيانات إضافية:</h4>
            <pre class="bg-slate-950 text-emerald-400 p-3 rounded-md overflow-x-auto font-mono text-left" style="direction: ltr; unicode-bidi: embed;">{{ prettyJson(selectedAuditLog?.metadataJson) }}</pre>
          </div>
        </div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end">
        <button (click)="closeModal()" class="px-4 py-2 bg-primary text-white rounded-md text-sm">إغلاق</button>
      </div>
    </div>

    <!-- Device Create/Edit Modal -->
    <div *ngIf="activeModal==='deviceCreate'||activeModal==='deviceEdit'" class="bg-white rounded-xl shadow-xl w-full max-w-md flex flex-col">
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800">{{activeModal==='deviceCreate'?'إضافة جهاز':'تعديل جهاز'}}</div>
      <div class="p-5 space-y-3 text-sm">
        <div *ngIf="formErr" class="bg-red-50 text-red-600 p-3 rounded-md">{{formErr}}</div>
        <div>
          <label class="block text-slate-600 mb-1">اسم الجهاز *</label>
          <input type="text" [(ngModel)]="deviceForm.deviceName" placeholder="مثال: كاشير 1" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
        </div>
        <div *ngIf="activeModal==='deviceCreate'">
          <label class="block text-slate-600 mb-1">رمز الجهاز (Code) *</label>
          <input type="text" [(ngModel)]="deviceForm.deviceCode" placeholder="مثال: POS-01" class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary">
          <p class="text-xs text-slate-400 mt-1">يجب أن يكون فريداً وغير قابل للتغيير لاحقاً.</p>
        </div>
        <div>
          <label class="block text-slate-600 mb-1">ملاحظات / موقع الجهاز</label>
          <textarea [(ngModel)]="deviceForm.notes" placeholder="موقع الجهاز أو أي تفاصيل إضافية..." class="w-full px-3 py-2 border border-slate-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary" rows="3"></textarea>
        </div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="saveDevice()" [disabled]="saving||!deviceForm.deviceName" class="px-4 py-2 bg-primary text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري الحفظ...':'حفظ'}}</button>
      </div>
    </div>

    <!-- Device Delete Modal -->
    <div *ngIf="activeModal==='deviceDelete'" class="bg-white rounded-xl shadow-xl w-full max-w-sm flex flex-col">
      <div class="px-5 py-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد حذف الجهاز</div>
      <div class="p-5 text-sm text-slate-700">هل أنت متأكد من حذف الجهاز "{{actionDevice?.deviceName}}"؟ قد يتم تعطيله بدلاً من الحذف إذا كان يمتلك جلسات بيع سابقة.</div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="confirmDeleteDevice()" [disabled]="saving" class="px-4 py-2 bg-red-600 text-white rounded-md text-sm disabled:opacity-50">{{saving?'جاري...':'حذف'}}</button>
      </div>
    </div>

    <!-- Session Confirm Close Modal -->
    <div *ngIf="activeModal==='sessionConfirmClose'" class="bg-white rounded-xl shadow-xl w-full max-w-md flex flex-col">
      <div class="px-5 py-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد إغلاق الجلسة</div>
      <div class="p-5 text-sm text-slate-700 space-y-3">
        <div *ngIf="formErr" class="bg-red-50 text-red-600 p-3 rounded-md">{{formErr}}</div>
        
        <div *ngIf="isCurrentSession(actionSession?.sessionId)">
          <p class="text-red-600 leading-relaxed font-bold">تنبيه: أنت تقوم بإغلاق جلستك الحالية. سيؤدي هذا إلى تسجيل خروجك فورًا من النظام. هل تريد الاستمرار؟</p>
        </div>
        <div *ngIf="!isCurrentSession(actionSession?.sessionId)">
          <p class="leading-relaxed">هل أنت متأكد من رغبتك في إغلاق هذه الجلسة فوراً؟ سيتم تسجيل خروج الموظف وقد يفقد أي بيانات غير محفوظة.</p>
          <div class="bg-slate-50 p-2.5 rounded border border-slate-100 mt-2 text-xs text-slate-500 space-y-1">
            <div><strong>الموظف:</strong> {{actionSession?.employeeName}}</div>
            <div><strong>الجهاز:</strong> {{actionSession?.deviceName}} ({{actionSession?.deviceCode}})</div>
          </div>
        </div>
      </div>
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
        <button (click)="closeModal()" class="px-4 py-2 border border-slate-300 rounded-md text-sm text-slate-700">إلغاء</button>
        <button (click)="confirmForceCloseSession()" [disabled]="saving" class="px-4 py-2 bg-red-600 text-white rounded-md text-sm disabled:opacity-50">
          {{saving ? 'جاري الإغلاق...' : 'تأكيد'}}
        </button>
      </div>
    </div>
  </div>

  <!-- Toast -->
  <div *ngIf="toast" class="fixed bottom-4 left-4 bg-red-600 text-white px-4 py-3 rounded-lg shadow-lg z-50 flex gap-3 items-center">
    <span class="text-sm">{{toast}}</span>
    <button (click)="toast=''" class="text-white/80 hover:text-white">✕</button>
  </div>
</div>
  `
})
export class SettingsComponent implements OnInit {
  tabs = [
    { id: 'employees' as TabType, label: 'الموظفون' },
    { id: 'categories' as TabType, label: 'التصنيفات' },
    { id: 'store' as TabType, label: 'المتجر' },
    { id: 'backup' as TabType, label: 'النسخ الاحتياطي' },
    { id: 'audit' as TabType, label: 'سجل النشاط' },
    { id: 'devices' as TabType, label: 'الأجهزة' },
    { id: 'sessions' as TabType, label: 'الجلسات النشطة' }
  ];
  allScreens = ALL_SCREENS;
  activeTab: TabType = 'employees';
  activeModal: ModalType = null;
  loading = false;
  saving = false;
  toast = '';
  formErr = '';

  employees: EmployeeListItem[] = [];
  categories: CategoryItem[] = [];
  storeSettings: PublicSettingsResponse | null = null;
  backupResult: CreateBackupResponse | null = null;
  backupLoading = false;
  backupError = '';

  // Employee form state
  editEmpId: number | null = null;
  actionEmp: EmployeeListItem | null = null;
  empForm: any = {};
  empPermissions: Record<string, boolean> = {};
  resetPwd = '';

  // Category form state
  editCatId: number | null = null;
  actionCat: CategoryItem | null = null;
  catForm: any = {};

  // Audit logs state
  auditLogs: AuditLogListItem[] = [];
  selectedAuditLog: AuditLogDetailResponse | null = null;
  auditStatus: AuditLogStatusResponse | null = null;

  // Audit log filter fields
  filterEmployeeId: number | null = null;
  filterAction: string = '';
  filterEntityType: string = '';
  filterDateFrom: string = '';
  filterDateTo: string = '';

  // Audit log pagination
  auditPage: number = 1;
  auditPageSize: number = 50;
  auditTotalCount: number = 0;

  // Device state
  devices: PosDeviceListItem[] = [];
  editDeviceId: number | null = null;
  actionDevice: PosDeviceListItem | null = null;
  deviceForm: any = {};

  // Active sessions state
  activeSessions: ActiveSessionResponse[] = [];
  actionSession: ActiveSessionResponse | null = null;

  constructor(
    private api: SettingsApiService,
    private authService: AuthService,
    private sessionService: SessionService,
    private router: Router,
    private blackBox: BlackBoxRecorderService
  ) {}

  ngOnInit() { this.loadTab(); }

  switchTab(t: TabType) { this.activeTab = t; this.loadTab(); }

  loadTab() {
    this.loading = true;
    if (this.activeTab === 'employees') {
      this.api.getEmployees().subscribe({ next: r => { this.employees = r.items || []; this.loading = false; }, error: e => { this.showToast(mapErr(e)); this.loading = false; } });
    } else if (this.activeTab === 'categories') {
      this.api.getCategories().subscribe({ next: r => { this.categories = r.items || []; this.loading = false; }, error: e => { this.showToast(mapErr(e)); this.loading = false; } });
    } else if (this.activeTab === 'store') {
      this.api.getPublicSettings().subscribe({ next: r => { this.storeSettings = r; this.loading = false; }, error: e => { this.showToast(mapErr(e)); this.loading = false; } });
    } else if (this.activeTab === 'audit') {
      this.loadAuditLogs();
    } else if (this.activeTab === 'devices') {
      this.api.getDevices().subscribe({ next: r => { this.devices = r || []; this.loading = false; }, error: e => { this.showToast(mapErr(e)); this.loading = false; } });
    } else if (this.activeTab === 'sessions') {
      this.loadActiveSessions();
    } else {
      this.loading = false;
    }
  }

  // --- Permissions helpers ---
  getPermission(screen: string): boolean { return !!this.empPermissions[screen]; }
  togglePermission(screen: string) { this.empPermissions[screen] = !this.empPermissions[screen]; }
  permissionsArray(): PermissionEntry[] { return ALL_SCREENS.map(s => ({ screenKey: s, canAccess: !!this.empPermissions[s] })); }

  // --- Employee modals ---
  openEmpCreate() {
    this.editEmpId = null;
    this.empForm = { fullName: '', username: '', phone: '', password: '', isActive: true };
    this.empPermissions = {};
    this.formErr = '';
    this.activeModal = 'empCreate';
  }

  openEmpEdit(e: EmployeeListItem) {
    this.editEmpId = e.id;
    this.actionEmp = e;
    this.formErr = '';
    this.api.getEmployee(e.id).subscribe({ next: d => { this.empForm = { fullName: d.fullName, phone: d.phone || '', isActive: d.isActive }; this.empPermissions = {}; d.permissions.forEach(p => { this.empPermissions[p.screenKey] = p.canAccess; }); this.activeModal = 'empEdit'; }, error: err => this.showToast(mapErr(err)) });
  }

  openEmpReset(e: EmployeeListItem) { this.actionEmp = e; this.resetPwd = ''; this.formErr = ''; this.activeModal = 'empReset'; }
  openEmpDelete(e: EmployeeListItem) { this.actionEmp = e; this.activeModal = 'empDelete'; }

  saveEmployee() {
    this.formErr = '';
    if (!this.empForm.fullName) { this.formErr = 'الاسم مطلوب'; return; }
    this.saving = true;
    if (this.activeModal === 'empCreate') {
      if (!this.empForm.username || !this.empForm.password) { this.formErr = 'اسم المستخدم وكلمة المرور مطلوبان'; this.saving = false; return; }
      this.api.createEmployee({ fullName: this.empForm.fullName, username: this.empForm.username, phone: this.empForm.phone || undefined, password: this.empForm.password, permissions: this.permissionsArray() }).subscribe({ next: () => { this.saving = false; this.closeModal(); this.loadTab(); }, error: e => { this.saving = false; this.formErr = mapErr(e); } });
    } else if (this.editEmpId) {
      this.api.updateEmployee(this.editEmpId, { fullName: this.empForm.fullName, phone: this.empForm.phone || undefined, isActive: this.empForm.isActive, permissions: this.permissionsArray() }).subscribe({ next: () => { this.saving = false; this.closeModal(); this.loadTab(); }, error: e => { this.saving = false; this.formErr = mapErr(e); } });
    }
  }

  confirmResetPassword() {
    if (!this.actionEmp || !this.resetPwd) return;
    this.saving = true;
    this.api.resetPassword(this.actionEmp.id, { newPassword: this.resetPwd }).subscribe({
      next: () => {
        this.saving = false;
        this.closeModal();
        this.showToast('تم تغيير كلمة المرور بنجاح');
      },
      error: e => {
        this.saving = false;
        const defaultErr = mapErr(e);
        this.formErr = defaultErr === 'حدث خطأ أثناء تنفيذ العملية' && (e?.error?.message || e?.error?.error)
          ? (e.error.message || e.error.error)
          : defaultErr;
      }
    });
  }

  confirmDeleteEmployee() {
    if (!this.actionEmp) return;
    this.saving = true;
    this.api.deleteEmployee(this.actionEmp.id).subscribe({ next: r => { this.saving = false; this.closeModal(); this.loadTab(); if (r.action === 'DEACTIVATED') this.showToast('تم تعطيل الموظف بدلاً من حذفه'); }, error: e => { this.saving = false; this.closeModal(); this.showToast(mapErr(e)); } });
  }

  // --- Category modals ---
  openCatCreate() { this.editCatId = null; this.catForm = { name: '', isActive: true }; this.formErr = ''; this.activeModal = 'catCreate'; }
  openCatEdit(c: CategoryItem) { this.editCatId = c.id; this.actionCat = c; this.catForm = { name: c.name, isActive: c.isActive }; this.formErr = ''; this.activeModal = 'catEdit'; }
  openCatDelete(c: CategoryItem) { this.actionCat = c; this.activeModal = 'catDelete'; }

  saveCategory() {
    if (!this.catForm.name) { this.formErr = 'الاسم مطلوب'; return; }
    this.saving = true;
    if (this.activeModal === 'catCreate') {
      this.api.createCategory({ name: this.catForm.name }).subscribe({ next: () => { this.saving = false; this.closeModal(); this.loadTab(); }, error: e => { this.saving = false; this.formErr = mapErr(e); } });
    } else if (this.editCatId) {
      this.api.updateCategory(this.editCatId, { name: this.catForm.name, isActive: this.catForm.isActive }).subscribe({ next: () => { this.saving = false; this.closeModal(); this.loadTab(); }, error: e => { this.saving = false; this.formErr = mapErr(e); } });
    }
  }

  confirmDeleteCategory() {
    if (!this.actionCat) return;
    this.saving = true;
    this.api.deleteCategory(this.actionCat.id).subscribe({ next: r => { this.saving = false; this.closeModal(); this.loadTab(); if (r.action === 'DISABLED') this.showToast('تم تعطيل التصنيف بدلاً من حذفه'); }, error: e => { this.saving = false; this.closeModal(); this.showToast(mapErr(e)); } });
  }

  createBackup() {
    this.backupError = '';
    this.backupResult = null;
    this.backupLoading = true;
    this.api.createBackup().subscribe({
      next: r => {
        this.backupLoading = false;
        this.backupResult = r;
        this.blackBox.recordSuccess('CREATE_BACKUP', {
          pageName: 'Settings',
          entityType: 'SystemBackup',
          entityId: r.fileName,
          metadata: {
            fileName: r.fileName,
            sizeBytes: r.sizeBytes
          }
        });
      },
      error: e => {
        this.backupLoading = false;
        const mapped = mapErr(e);
        this.backupError = mapped === 'حدث خطأ أثناء تنفيذ العملية'
          ? 'فشل إنشاء النسخة الاحتياطية'
          : mapped;
        this.blackBox.recordFailure('CREATE_BACKUP', {
          pageName: 'Settings',
          entityType: 'SystemBackup',
          message: e?.error?.error || 'CREATE_BACKUP_FAILED'
        });
      }
    });
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 B';
    const mb = bytes / (1024 * 1024);
    return mb >= 1 ? `${mb.toFixed(2)} MB` : `${(bytes / 1024).toFixed(2)} KB`;
  }

  loadAuditLogs() {
    this.loading = true;
    const params: any = {
      page: this.auditPage,
      pageSize: this.auditPageSize
    };
    if (this.filterEmployeeId) params.employeeId = this.filterEmployeeId;
    if (this.filterAction) params.action = this.filterAction;
    if (this.filterEntityType) params.entityType = this.filterEntityType;
    
    if (this.filterDateFrom) {
      params.dateFrom = new Date(this.filterDateFrom).toISOString();
    }
    if (this.filterDateTo) {
      params.dateTo = new Date(this.filterDateTo).toISOString();
    }

    this.api.getAuditLogsStatus().subscribe({
      next: status => {
        this.auditStatus = status;
      },
      error: () => {
        // Silent fallback
      }
    });

    this.api.getAuditLogs(params).subscribe({
      next: r => {
        this.auditLogs = r.items || [];
        this.auditTotalCount = r.totalCount || 0;
        this.loading = false;
      },
      error: e => {
        this.showToast(mapErr(e));
        this.loading = false;
      }
    });
  }

  resetAuditFilters() {
    this.filterEmployeeId = null;
    this.filterAction = '';
    this.filterEntityType = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.auditPage = 1;
    this.loadAuditLogs();
  }

  openAuditDetail(id: number) {
    this.loading = true;
    this.api.getAuditLog(id).subscribe({
      next: d => {
        this.selectedAuditLog = d;
        this.activeModal = 'auditDetail';
        this.loading = false;
      },
      error: err => {
        this.showToast(mapErr(err));
        this.loading = false;
      }
    });
  }

  translateAction(action: string): string {
    const map: Record<string, string> = {
      'CREATE_BACKUP': 'إنشاء نسخة احتياطية',
      'RESET_PASSWORD': 'إعادة تعيين كلمة المرور',
      'COMPLETE_PURCHASE': 'إتمام فاتورة شراء',
      'COMPLETE_INVOICE': 'إتمام فاتورة بيع',
      'CREATE': 'إضافة / إنشاء',
      'UPDATE': 'تعديل / تحديث',
      'DELETE': 'حذف',
      'DEACTIVATE': 'تعطيل',
      'CREATE_DEVICE': 'إنشاء جهاز',
      'UPDATE_DEVICE': 'تعديل جهاز',
      'ENABLE_DEVICE': 'تفعيل جهاز',
      'DISABLE_DEVICE': 'تعطيل جهاز',
      'DELETE_DEVICE': 'حذف جهاز',
      'FORCE_CLOSE_SESSION': 'إغلاق جلسة إدارياً'
    };
    return map[action] || action;
  }

  prettyJson(jsonStr?: string): string {
    if (!jsonStr) return '—';
    try {
      const parsed = JSON.parse(jsonStr);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return jsonStr;
    }
  }

  changeAuditPage(direction: number) {
    const next = this.auditPage + direction;
    if (next < 1 || (direction > 0 && (next - 1) * this.auditPageSize >= this.auditTotalCount)) return;
    this.auditPage = next;
    this.loadAuditLogs();
  }

  // --- Device modals and operations ---
  openDeviceCreate() {
    this.editDeviceId = null;
    this.deviceForm = { deviceCode: '', deviceName: '', notes: '' };
    this.formErr = '';
    this.activeModal = 'deviceCreate';
  }

  openDeviceEdit(d: PosDeviceListItem) {
    this.editDeviceId = d.id;
    this.actionDevice = d;
    this.deviceForm = { deviceName: d.deviceName, notes: d.notes || '' };
    this.formErr = '';
    this.activeModal = 'deviceEdit';
  }

  openDeviceDelete(d: PosDeviceListItem) {
    if (d.deviceCode === 'DEFAULT_DEVICE') {
      this.showToast('لا يمكن حذف جهاز النظام الافتراضي');
      return;
    }
    this.actionDevice = d;
    this.activeModal = 'deviceDelete';
  }

  saveDevice() {
    this.formErr = '';
    if (!this.deviceForm.deviceName) {
      this.formErr = 'اسم الجهاز مطلوب';
      return;
    }
    this.saving = true;

    if (this.activeModal === 'deviceCreate') {
      if (!this.deviceForm.deviceCode) {
        this.formErr = 'رمز الجهاز مطلوب';
        this.saving = false;
        return;
      }
      this.api.createDevice(this.deviceForm).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadTab();
        },
        error: e => {
          this.saving = false;
          this.formErr = mapErr(e);
        }
      });
    } else if (this.editDeviceId) {
      this.api.updateDevice(this.editDeviceId, {
        deviceName: this.deviceForm.deviceName,
        notes: this.deviceForm.notes
      }).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadTab();
        },
        error: e => {
          this.saving = false;
          this.formErr = mapErr(e);
        }
      });
    }
  }

  toggleDeviceActive(d: PosDeviceListItem) {
    this.loading = true;
    const req = d.isActive ? this.api.disableDevice(d.id) : this.api.enableDevice(d.id);
    req.subscribe({
      next: () => {
        this.loadTab();
      },
      error: e => {
        this.loading = false;
        this.showToast(mapErr(e));
      }
    });
  }

  confirmDeleteDevice() {
    if (!this.actionDevice) return;
    this.saving = true;
    this.api.deleteDevice(this.actionDevice.id).subscribe({
      next: r => {
        this.saving = false;
        this.closeModal();
        this.loadTab();
        if (r.message === 'DEVICE_DEACTIVATED_INSTEAD_OF_DELETED') {
          this.showToast('تم تعطيل الجهاز بدلاً من حذفه لوجود جلسات مرتبطة به');
        } else {
          this.showToast('تم حذف الجهاز بنجاح');
        }
      },
      error: e => {
        this.saving = false;
        this.closeModal();
        this.showToast(mapErr(e));
      }
    });
  }

  copyDeviceCode(code: string) {
    navigator.clipboard.writeText(code).then(() => {
      this.showToast('تم نسخ رمز الجهاز إلى الحافظة');
    }).catch(() => {
      this.showToast('فشل نسخ رمز الجهاز');
    });
  }

  loadActiveSessions() {
    this.loading = true;
    this.api.getActiveSessions().subscribe({
      next: r => {
        this.activeSessions = r || [];
        this.loading = false;
      },
      error: e => {
        this.showToast(mapErr(e));
        this.loading = false;
      }
    });
  }

  isCurrentSession(sessionId?: number | null): boolean {
    if (sessionId === undefined || sessionId === null) return false;
    const currentId = this.sessionService.getSessionId();
    return currentId !== null && Number(currentId) === sessionId;
  }

  openSessionCloseConfirm(s: ActiveSessionResponse) {
    this.actionSession = s;
    this.formErr = '';
    this.activeModal = 'sessionConfirmClose';
  }

  confirmForceCloseSession() {
    if (!this.actionSession) return;
    this.saving = true;
    this.api.forceCloseSession(this.actionSession.sessionId).subscribe({
      next: () => {
        this.blackBox.recordSuccess('FORCE_CLOSE_SESSION', {
          pageName: 'Settings',
          entityType: 'CashSession',
          entityId: this.actionSession!.sessionId,
          metadata: {
            employeeId: this.actionSession!.employeeId,
            deviceCode: this.actionSession!.deviceCode
          }
        });
        this.saving = false;
        const closedSessionId = this.actionSession!.sessionId;
        this.closeModal();

        if (this.isCurrentSession(closedSessionId)) {
          this.sessionService.clearSession();
          this.authService.setAuthenticated(false);
          this.router.navigate(['/login']);
        } else {
          this.loadActiveSessions();
          this.showToast('تم إغلاق الجلسة بنجاح');
        }
      },
      error: e => {
        this.blackBox.recordFailure('FORCE_CLOSE_SESSION', {
          pageName: 'Settings',
          entityType: 'CashSession',
          entityId: this.actionSession?.sessionId,
          message: e?.error?.error || 'FORCE_CLOSE_SESSION_FAILED'
        });
        this.saving = false;
        const defaultErr = mapErr(e);
        this.formErr = defaultErr === 'حدث خطأ أثناء تنفيذ العملية' ? 'حدث خطأ أثناء إغلاق الجلسة' : defaultErr;
      }
    });
  }

  closeModal() { this.activeModal = null; this.formErr = ''; this.saving = false; }
  showToast(msg: string) { this.toast = msg; setTimeout(() => { this.toast = ''; }, 5000); }
}
