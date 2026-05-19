import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SuppliersApiService } from '../services/suppliers-api.service';
import {
  SupplierDetailResponse,
  SupplierListItem,
  UpdateSupplierRequest
} from '../models/supplier.model';

type SupplierModal = 'form' | 'delete' | null;
type SupplierStatusFilter = 'active' | 'inactive' | null;

interface SupplierForm {
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
}

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 h-full flex flex-col bg-slate-50 text-slate-800" dir="rtl">
      <div class="flex justify-between items-center mb-6 shrink-0">
        <div>
          <h1 class="text-2xl font-bold text-slate-900">الموردون</h1>
          <p class="text-sm text-slate-500 mt-1">إدارة بيانات الموردين</p>
        </div>
        <button class="btn-primary" (click)="openCreateModal()" [disabled]="isLoading">
          <svg class="w-4 h-4 ml-2 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
          </svg>
          إضافة مورد
        </button>
      </div>

      <div class="bg-white p-4 rounded-lg shadow-sm border border-slate-200 mb-4 shrink-0 flex flex-wrap gap-4 items-center">
        <div class="relative flex-1 min-w-[220px]">
          <input
            type="text"
            [(ngModel)]="searchTerm"
            (ngModelChange)="applyFilters()"
            placeholder="بحث بالاسم أو الهاتف أو البريد..."
            class="input-field pl-10">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
          </span>
        </div>
        <div class="w-44">
          <select [(ngModel)]="filterStatus" (ngModelChange)="applyFilters()" class="input-field">
            <option [ngValue]="null">كل الحالات</option>
            <option [value]="'active'">نشط</option>
            <option [value]="'inactive'">غير نشط</option>
          </select>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-slate-200 flex-1 overflow-hidden flex flex-col">
        <div class="overflow-x-auto flex-1">
          <table class="w-full text-right text-sm">
            <thead class="bg-slate-50 text-slate-500 font-medium border-b border-slate-200 sticky top-0">
              <tr>
                <th class="py-3 px-4 w-20 text-center">رقم المورد</th>
                <th class="py-3 px-4">الاسم</th>
                <th class="py-3 px-4">الهاتف</th>
                <th class="py-3 px-4">البريد</th>
                <th class="py-3 px-4 text-center">الحالة</th>
                <th class="py-3 px-4">آخر تحديث</th>
                <th class="py-3 px-4 text-center w-24">إجراءات</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngIf="isLoading && !suppliers.length">
                <td colspan="7" class="py-8 text-center text-slate-400">جاري التحميل...</td>
              </tr>
              <tr *ngIf="!isLoading && !suppliers.length">
                <td colspan="7" class="py-8 text-center text-slate-400">لا توجد موردون</td>
              </tr>
              <tr *ngIf="!isLoading && suppliers.length && !filteredSuppliers.length">
                <td colspan="7" class="py-8 text-center text-slate-400">لا توجد بيانات مطابقة</td>
              </tr>
              <tr *ngFor="let supplier of filteredSuppliers" class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 text-center font-mono text-slate-500 text-xs">{{ supplier.id }}</td>
                <td class="py-3 px-4 font-medium">{{ supplier.name }}</td>
                <td class="py-3 px-4 text-slate-500">{{ supplier.phone || '—' }}</td>
                <td class="py-3 px-4 text-slate-500">{{ supplier.email || '—' }}</td>
                <td class="py-3 px-4 text-center">
                  <span class="px-2 py-1 rounded-full text-xs font-medium"
                        [class.bg-green-100]="supplier.isActive"
                        [class.text-green-800]="supplier.isActive"
                        [class.bg-slate-100]="!supplier.isActive"
                        [class.text-slate-600]="!supplier.isActive">
                    {{ supplier.isActive ? 'نشط' : 'غير نشط' }}
                  </span>
                </td>
                <td class="py-3 px-4 text-slate-500">{{ supplier.updatedAt | date:'yyyy-MM-dd HH:mm' }}</td>
                <td class="py-3 px-4 text-center">
                  <div class="flex items-center justify-center gap-2">
                    <button class="text-slate-400 hover:text-primary transition-colors" title="تعديل" (click)="openEditModal(supplier)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                      </svg>
                    </button>
                    <button class="text-slate-400 hover:text-red-600 transition-colors" title="حذف أو تعطيل" (click)="openDeleteModal(supplier)">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="bg-slate-50 p-3 border-t border-slate-200 text-sm text-slate-500 flex justify-between items-center">
          <span>إجمالي الموردين: {{ filteredSuppliers.length }}</span>
        </div>
      </div>

      <div *ngIf="toast" class="fixed bottom-4 left-4 bg-slate-900 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span class="text-sm">{{ toast }}</span>
        <button (click)="toast = ''" class="text-white/80 hover:text-white">x</button>
      </div>

      <div *ngIf="error && !activeModal" class="fixed bottom-4 left-4 bg-red-600 text-white p-4 rounded shadow-lg z-50 flex gap-4 items-center">
        <span class="text-sm">{{ error }}</span>
        <button (click)="error = ''" class="text-white/80 hover:text-white">x</button>
      </div>

      <div *ngIf="activeModal" class="fixed inset-0 bg-slate-900/50 z-40 flex items-center justify-center p-4">
        <div *ngIf="activeModal === 'form'" class="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <div class="p-4 border-b border-slate-100 font-bold text-slate-800 shrink-0">
            {{ editId ? 'تعديل مورد' : 'إضافة مورد جديد' }}
          </div>
          <div class="p-6 overflow-y-auto flex-1 space-y-4 text-sm">
            <div *ngIf="formErr" class="bg-red-50 border border-red-200 text-red-700 rounded p-3">{{ formErr }}</div>
            <div>
              <label class="block text-slate-600 mb-1">الاسم <span class="text-red-500">*</span></label>
              <input type="text" [(ngModel)]="formData.name" class="input-field" [class.border-red-500]="formErr">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-slate-600 mb-1">الهاتف</label>
                <input type="text" [(ngModel)]="formData.phone" class="input-field">
              </div>
              <div>
                <label class="block text-slate-600 mb-1">البريد</label>
                <input type="email" [(ngModel)]="formData.email" class="input-field">
              </div>
            </div>
            <div>
              <label class="block text-slate-600 mb-1">العنوان</label>
              <input type="text" [(ngModel)]="formData.address" class="input-field">
            </div>
            <div>
              <label class="block text-slate-600 mb-1">الملاحظات</label>
              <textarea [(ngModel)]="formData.notes" class="input-field min-h-[90px] resize-y"></textarea>
            </div>
            <label class="flex items-center gap-2 cursor-pointer" *ngIf="editId">
              <input type="checkbox" [(ngModel)]="formData.isActive" class="rounded border-slate-300 text-primary focus:ring-primary">
              <span class="font-medium text-slate-700">المورد نشط</span>
            </label>
          </div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end shrink-0 border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-primary text-sm" (click)="saveSupplier()" [disabled]="actionLoading || !isValidForm()">حفظ</button>
          </div>
        </div>

        <div *ngIf="activeModal === 'delete'" class="bg-white rounded-lg shadow-xl w-full max-w-sm overflow-hidden">
          <div class="p-4 border-b border-red-100 bg-red-50 font-bold text-red-800">تأكيد الحذف أو التعطيل</div>
          <div class="p-4 text-slate-700 text-sm">
            هل أنت متأكد من حذف أو تعطيل المورد "{{ deleteCandidate?.name }}"؟
          </div>
          <div *ngIf="formErr" class="mx-4 mb-4 bg-red-50 border border-red-200 text-red-700 rounded p-3 text-sm">{{ formErr }}</div>
          <div class="p-4 bg-slate-50 flex gap-2 justify-end border-t border-slate-100">
            <button class="btn-secondary text-sm" (click)="closeModal()" [disabled]="actionLoading">إلغاء</button>
            <button class="btn-danger text-sm" (click)="confirmDelete()" [disabled]="actionLoading">تأكيد</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class SuppliersComponent implements OnInit {
  suppliers: SupplierListItem[] = [];
  filteredSuppliers: SupplierListItem[] = [];
  isLoading = false;
  actionLoading = false;
  error = '';
  formErr = '';
  toast = '';

  searchTerm = '';
  filterStatus: SupplierStatusFilter = null;

  activeModal: SupplierModal = null;
  editId: number | null = null;
  formData: SupplierForm = this.emptyForm();
  deleteCandidate: SupplierListItem | null = null;

  constructor(private api: SuppliersApiService) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.isLoading = true;
    this.error = '';
    this.api.getSuppliers().subscribe({
      next: response => {
        this.suppliers = response.items || [];
        this.applyFilters();
        this.isLoading = false;
      },
      error: error => {
        this.error = this.mapError(error);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();
    let result = this.suppliers;

    if (term) {
      result = result.filter(s =>
        s.name.toLowerCase().includes(term) ||
        (s.phone || '').toLowerCase().includes(term) ||
        (s.email || '').toLowerCase().includes(term)
      );
    }

    if (this.filterStatus) {
      const isActive = this.filterStatus === 'active';
      result = result.filter(s => s.isActive === isActive);
    }

    this.filteredSuppliers = result;
  }

  openCreateModal(): void {
    this.editId = null;
    this.formData = this.emptyForm();
    this.formErr = '';
    this.activeModal = 'form';
  }

  openEditModal(supplier: SupplierListItem): void {
    this.formErr = '';
    this.actionLoading = true;
    this.api.getSupplier(supplier.id).subscribe({
      next: details => {
        this.editId = supplier.id;
        this.formData = this.toForm(details);
        this.activeModal = 'form';
        this.actionLoading = false;
      },
      error: error => {
        this.error = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  isValidForm(): boolean {
    return !!this.formData.name.trim();
  }

  saveSupplier(): void {
    this.formErr = '';
    if (!this.isValidForm()) {
      this.formErr = 'اسم المورد مطلوب';
      return;
    }

    this.actionLoading = true;
    const request = this.cleanForm();
    const operation = this.editId
      ? this.api.updateSupplier(this.editId, request as UpdateSupplierRequest)
      : this.api.createSupplier(request);

    operation.subscribe({
      next: () => {
        this.closeModal();
        this.actionLoading = false;
        this.loadSuppliers();
      },
      error: error => {
        this.formErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  openDeleteModal(supplier: SupplierListItem): void {
    this.deleteCandidate = supplier;
    this.formErr = '';
    this.activeModal = 'delete';
  }

  confirmDelete(): void {
    if (!this.deleteCandidate) return;

    this.actionLoading = true;
    this.formErr = '';
    this.api.deleteSupplier(this.deleteCandidate.id).subscribe({
      next: response => {
        this.toast = response.action === 'DEACTIVATED'
          ? 'تم تعطيل المورد لأنه مستخدم'
          : 'تم حذف المورد';
        this.closeModal();
        this.actionLoading = false;
        this.loadSuppliers();
      },
      error: error => {
        this.formErr = this.mapError(error);
        this.actionLoading = false;
      }
    });
  }

  closeModal(): void {
    this.activeModal = null;
    this.editId = null;
    this.deleteCandidate = null;
    this.formErr = '';
  }

  private emptyForm(): SupplierForm {
    return {
      name: '',
      phone: null,
      email: null,
      address: null,
      notes: null,
      isActive: true
    };
  }

  private toForm(details: SupplierDetailResponse): SupplierForm {
    return {
      name: details.name,
      phone: details.phone || null,
      email: details.email || null,
      address: details.address || null,
      notes: details.notes || null,
      isActive: details.isActive
    };
  }

  private cleanForm(): SupplierForm {
    return {
      name: this.formData.name.trim(),
      phone: this.cleanOptional(this.formData.phone),
      email: this.cleanOptional(this.formData.email),
      address: this.cleanOptional(this.formData.address),
      notes: this.cleanOptional(this.formData.notes),
      isActive: this.formData.isActive
    };
  }

  private cleanOptional(value: string | null): string | null {
    const trimmed = value?.trim();
    return trimmed ? trimmed : null;
  }

  private mapError(error: unknown): string {
    const code = error instanceof HttpErrorResponse ? error.error?.error : null;
    switch (code) {
      case 'SUPPLIER_NAME_REQUIRED':
        return 'اسم المورد مطلوب';
      case 'SUPPLIER_NAME_ALREADY_EXISTS':
        return 'اسم المورد موجود مسبقًا';
      case 'SUPPLIER_NOT_FOUND':
        return 'المورد غير موجود';
      default:
        return 'حدث خطأ أثناء تنفيذ العملية';
    }
  }
}
