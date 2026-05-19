import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { SettingsApiService } from '../services/settings-api.service';
import { EmployeeListItem, PermissionEntry, CategoryItem, PublicSettingsResponse, CreateBackupResponse } from '../models/settings.model';

type TabType = 'employees' | 'categories' | 'store' | 'backup';
type ModalType = 'empCreate' | 'empEdit' | 'empDelete' | 'empReset' | 'catCreate' | 'catEdit' | 'catDelete' | null;

const ALL_SCREENS = ['Sales','Products','Invoices','Offers','Reports','Inventory','Settings'];

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
      <div *ngIf="activeTab==='store'">
        <div class="max-w-md bg-white rounded-xl border border-slate-200 shadow-sm p-6 space-y-4">
          <h2 class="font-bold text-slate-700">معلومات المتجر</h2>
          <div><span class="text-xs text-slate-500 block mb-1">اسم المتجر</span><p class="font-medium text-slate-800">{{storeSettings?.storeName||'—'}}</p></div>
          <div><span class="text-xs text-slate-500 block mb-1">سعر الصرف</span><p class="font-medium text-slate-800">1 USD = {{storeSettings?.exchangeRate||0}} SYP</p></div>
          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 text-amber-800 text-sm">⚠️ تعديل اسم المتجر وسعر الصرف غير متاح في هذه النسخة.</div>
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
    { id: 'backup' as TabType, label: 'النسخ الاحتياطي' }
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

  constructor(private api: SettingsApiService) {}

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
      },
      error: e => {
        this.backupLoading = false;
        const mapped = mapErr(e);
        this.backupError = mapped === 'حدث خطأ أثناء تنفيذ العملية'
          ? 'فشل إنشاء النسخة الاحتياطية'
          : mapped;
      }
    });
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 B';
    const mb = bytes / (1024 * 1024);
    return mb >= 1 ? `${mb.toFixed(2)} MB` : `${(bytes / 1024).toFixed(2)} KB`;
  }

  closeModal() { this.activeModal = null; this.formErr = ''; this.saving = false; }
  showToast(msg: string) { this.toast = msg; setTimeout(() => { this.toast = ''; }, 5000); }
}
