import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BlackBoxApiService, BlackBoxEventListItem, BlackBoxEventDetailResponse } from '../../core/services/black-box-api.service';

@Component({
  selector: 'app-black-box',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
<div class="h-full flex flex-col bg-slate-50 text-slate-800 animate-fadeIn" dir="rtl">
  <!-- Header -->
  <div class="bg-white border-b border-slate-200 px-6 py-4 shrink-0 shadow-sm">
    <h1 class="text-2xl font-bold text-slate-850">الصندوق الأسود</h1>
    <p class="text-sm text-slate-500 mt-1">سجل النشاط التشغيلي المهم في النظام</p>
  </div>

  <!-- Content Area -->
  <div class="flex-1 overflow-auto p-6 space-y-4">
    <!-- Loading spinner (initial load / no data) -->
    <div *ngIf="loading && !events.length" class="flex flex-col items-center justify-center py-20 bg-white rounded-xl border border-slate-200 shadow-sm">
      <div class="w-10 h-10 border-4 border-slate-200 border-t-primary rounded-full animate-spin"></div>
      <p class="text-sm text-slate-500 mt-4 font-medium">جاري تحميل سجلات النشاط...</p>
    </div>

    <!-- Forbidden / Unauthorized State -->
    <div *ngIf="isForbidden" class="bg-red-50 border border-red-200 text-red-800 rounded-xl p-8 text-center max-w-xl mx-auto shadow-sm space-y-3">
      <div class="text-3xl">🚫</div>
      <h3 class="font-bold text-lg text-red-950">عذرًا، لا تملك الصلاحية للوصول إلى الصندوق الأسود.</h3>
      <p class="text-sm text-red-750">يرجى التواصل مع مسؤول النظام لمنحك الصلاحية اللازمة (BlackBox).</p>
    </div>

    <!-- General Error State -->
    <div *ngIf="error && !isForbidden" class="bg-red-50 border border-red-200 text-red-800 rounded-xl p-6 text-center max-w-xl mx-auto shadow-sm space-y-3">
      <div class="text-3xl">⚠️</div>
      <h3 class="font-bold text-lg text-red-950">حدث خطأ أثناء تحميل البيانات</h3>
      <p class="text-sm text-red-700">{{ error }}</p>
      <button (click)="loadEvents()" class="px-4 py-2 bg-red-600 text-white text-xs font-semibold rounded-md hover:bg-red-700 transition duration-150 shadow-sm">
        إعادة المحاولة
      </button>
    </div>

    <ng-container *ngIf="!isForbidden && !error">
      <!-- Filters Card -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-4 space-y-4">
        <div class="flex items-center justify-between pb-2 border-b border-slate-100">
          <h3 class="font-bold text-slate-800 text-sm">تصفية سجل النشاط</h3>
          <button (click)="resetFilters()" class="text-xs text-primary hover:underline font-semibold transition duration-150">مسح الفلاتر</button>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3 text-xs">
          <!-- Date From -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">من تاريخ</label>
            <input type="date" [(ngModel)]="filterDateFrom" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Date To -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">إلى تاريخ</label>
            <input type="date" [(ngModel)]="filterDateTo" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Employee ID -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">رقم الموظف</label>
            <input type="number" [(ngModel)]="filterEmployeeId" placeholder="مثال: 1" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Device Code -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">رمز الجهاز</label>
            <input type="text" [(ngModel)]="filterDeviceCode" placeholder="مثال: POS-01" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Action Type -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">نوع العملية</label>
            <input type="text" [(ngModel)]="filterActionType" placeholder="مثال: COMPLETE_INVOICE" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Page Name -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">اسم الصفحة</label>
            <input type="text" [(ngModel)]="filterPageName" placeholder="مثال: Cashier" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Result -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">النتيجة</label>
            <select [(ngModel)]="filterResult" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary bg-white">
              <option value="">الكل</option>
              <option value="SUCCESS">ناجح (SUCCESS)</option>
              <option value="FAILED">فاشل (FAILED)</option>
              <option value="BLOCKED">محظور (BLOCKED)</option>
              <option value="CANCELLED">ملغى (CANCELLED)</option>
            </select>
          </div>
          <!-- Entity Type -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">نوع الكيان</label>
            <input type="text" [(ngModel)]="filterEntityType" placeholder="مثال: Invoice" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Entity ID -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">رقم الكيان</label>
            <input type="text" [(ngModel)]="filterEntityId" placeholder="مثال: 105" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
          <!-- Route -->
          <div>
            <label class="block text-slate-500 mb-1 font-medium">المسار</label>
            <input type="text" [(ngModel)]="filterRoute" placeholder="مثال: /cashier" class="w-full px-3 py-1.5 border border-slate-350 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary">
          </div>
        </div>

        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <button (click)="search()" class="px-5 py-2 bg-primary text-white rounded-md text-xs font-semibold hover:bg-primary-dark transition duration-150 shadow-sm flex items-center gap-1.5">
            <span *ngIf="loading" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            تحديث
          </button>
        </div>
      </div>

      <!-- Table Card -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
        <div class="overflow-x-auto">
          <table class="w-full text-right border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 text-slate-500 border-b border-slate-200 font-semibold select-none">
                <th class="p-3">الوقت (UTC)</th>
                <th class="p-3">الموظف</th>
                <th class="p-3">الجهاز</th>
                <th class="p-3">الصفحة</th>
                <th class="p-3">نوع العملية</th>
                <th class="p-3">الكيان</th>
                <th class="p-3">النتيجة</th>
                <th class="p-3">الرسالة</th>
                <th class="p-3 text-left">التفاصيل</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-slate-700">
              <tr *ngFor="let item of events" class="hover:bg-slate-50 transition duration-150">
                <td class="p-3 whitespace-nowrap text-slate-500 font-mono">{{ item.createdAtUtc | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td class="p-3 font-medium whitespace-nowrap">
                  {{ item.employeeName || '—' }} 
                  <span *ngIf="item.employeeId" class="text-slate-400 font-normal font-mono text-[10px] mr-1">(رقم: {{ item.employeeId }})</span>
                </td>
                <td class="p-3 font-mono text-slate-650">{{ item.deviceCode || '—' }}</td>
                <td class="p-3 whitespace-nowrap">{{ item.pageName || '—' }}</td>
                <td class="p-3 font-medium text-slate-900 whitespace-nowrap">{{ item.actionType }}</td>
                <td class="p-3 font-mono">
                  {{ item.entityType || '—' }}
                  <span *ngIf="item.entityId" class="text-slate-400 font-normal text-[10px] mr-1">#{{ item.entityId }}</span>
                </td>
                <td class="p-3">
                  <span class="px-2 py-0.5 rounded-full font-semibold text-[10px] border"
                    [class.bg-green-50]="item.result === 'SUCCESS'" [class.text-green-700]="item.result === 'SUCCESS'" [class.border-green-150]="item.result === 'SUCCESS'"
                    [class.bg-red-50]="item.result === 'FAILED'" [class.text-red-700]="item.result === 'FAILED'" [class.border-red-150]="item.result === 'FAILED'"
                    [class.bg-amber-50]="item.result === 'BLOCKED'" [class.text-amber-700]="item.result === 'BLOCKED'" [class.border-amber-150]="item.result === 'BLOCKED'"
                    [class.bg-slate-100]="item.result === 'CANCELLED' || (item.result !== 'SUCCESS' && item.result !== 'FAILED' && item.result !== 'BLOCKED')" 
                    [class.text-slate-700]="item.result === 'CANCELLED' || (item.result !== 'SUCCESS' && item.result !== 'FAILED' && item.result !== 'BLOCKED')"
                    [class.border-slate-200]="item.result === 'CANCELLED' || (item.result !== 'SUCCESS' && item.result !== 'FAILED' && item.result !== 'BLOCKED')">
                    {{ translateResult(item.result) }}
                  </span>
                </td>
                <td class="p-3 max-w-xs truncate" [title]="item.message || ''">{{ item.message || '—' }}</td>
                <td class="p-3 text-left">
                  <button (click)="openDetails(item.id)" class="px-3 py-1 text-primary hover:bg-slate-100 border border-transparent hover:border-slate-200 rounded-md transition duration-150 font-medium">
                    تفاصيل
                  </button>
                </td>
              </tr>

              <!-- Empty State -->
              <tr *ngIf="events.length === 0 && !loading">
                <td colspan="9" class="p-10 text-center text-slate-400 bg-white">
                  <div class="text-3xl mb-2">🔍</div>
                  <p class="font-medium text-sm text-slate-500">لا توجد سجلات مطابقة للفلاتر المحددة.</p>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination Bar -->
        <div class="px-4 py-3 bg-slate-50 border-t border-slate-200 flex items-center justify-between text-xs text-slate-500 select-none">
          <div>
            <span>عرض الصفحة <strong class="text-slate-700 font-bold">{{ page }}</strong> (إجمالي السجلات: <strong class="text-slate-700 font-bold">{{ totalCount }}</strong>)</span>
          </div>
          <div class="flex items-center gap-2">
            <button (click)="changePage(-1)" [disabled]="page <= 1 || loading" class="px-3 py-1.5 border border-slate-300 bg-white rounded-md shadow-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-50 transition duration-150 font-medium text-slate-700">
              السابق
            </button>
            <button (click)="changePage(1)" [disabled]="page * pageSize >= totalCount || loading" class="px-3 py-1.5 border border-slate-300 bg-white rounded-md shadow-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-50 transition duration-150 font-medium text-slate-700">
              التالي
            </button>
          </div>
        </div>
      </div>
    </ng-container>
  </div>

  <!-- Details Modal -->
  <div *ngIf="activeModal==='details'" class="fixed inset-0 bg-slate-900/50 z-50 flex items-center justify-center p-4 backdrop-blur-sm animate-fadeIn">
    <div class="bg-white rounded-xl shadow-xl w-full max-w-3xl flex flex-col max-h-[90vh] border border-slate-200 overflow-hidden">
      <!-- Modal Header -->
      <div class="px-5 py-4 border-b border-slate-200 font-bold text-slate-800 flex justify-between items-center bg-slate-50">
        <span class="text-sm">تفاصيل سجل الصندوق الأسود #{{ selectedEvent?.id }}</span>
        <button (click)="closeModal()" class="text-slate-400 hover:text-slate-650 transition duration-150 text-xl font-bold">✕</button>
      </div>

      <!-- Modal Body -->
      <div class="p-5 overflow-y-auto flex-1 space-y-4 text-xs">
        <!-- Event Details Fields -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 bg-slate-50 p-4 rounded-lg border border-slate-200 text-slate-700">
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">العملية</span>
            <span class="font-bold text-slate-900">{{ selectedEvent?.actionType }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">نوع الكيان</span>
            <span class="font-semibold text-slate-800">{{ selectedEvent?.entityType || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">معرّف الكيان</span>
            <span class="font-mono text-slate-850 font-medium">{{ selectedEvent?.entityId || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">الموظف</span>
            <span class="font-semibold text-slate-800">
              {{ selectedEvent?.employeeName || '—' }}
              <strong *ngIf="selectedEvent?.employeeId" class="font-normal text-slate-500 font-mono">(رقم: {{ selectedEvent?.employeeId }})</strong>
            </span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">رقم الجلسة</span>
            <span class="font-mono text-slate-800">{{ selectedEvent?.sessionId || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">التاريخ والوقت (UTC)</span>
            <span class="text-slate-800 font-mono">{{ selectedEvent?.createdAtUtc | date:'yyyy-MM-dd HH:mm:ss' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">رمز الجهاز</span>
            <span class="font-mono text-slate-800 font-medium">{{ selectedEvent?.deviceCode || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">المسار</span>
            <span class="font-mono text-slate-800">{{ selectedEvent?.route || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">اسم الصفحة</span>
            <span class="font-medium text-slate-850">{{ selectedEvent?.pageName || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">النتيجة</span>
            <span class="px-2 py-0.5 rounded-full font-bold text-[10px] border inline-block"
              [class.bg-green-50]="selectedEvent?.result === 'SUCCESS'" [class.text-green-700]="selectedEvent?.result === 'SUCCESS'" [class.border-green-200]="selectedEvent?.result === 'SUCCESS'"
              [class.bg-red-50]="selectedEvent?.result === 'FAILED'" [class.text-red-700]="selectedEvent?.result === 'FAILED'" [class.border-red-200]="selectedEvent?.result === 'FAILED'"
              [class.bg-amber-50]="selectedEvent?.result === 'BLOCKED'" [class.text-amber-700]="selectedEvent?.result === 'BLOCKED'" [class.border-amber-200]="selectedEvent?.result === 'BLOCKED'"
              [class.bg-slate-100]="selectedEvent?.result === 'CANCELLED' || (selectedEvent?.result !== 'SUCCESS' && selectedEvent?.result !== 'FAILED' && selectedEvent?.result !== 'BLOCKED')"
              [class.text-slate-700]="selectedEvent?.result === 'CANCELLED' || (selectedEvent?.result !== 'SUCCESS' && selectedEvent?.result !== 'FAILED' && selectedEvent?.result !== 'BLOCKED')"
              [class.border-slate-250]="selectedEvent?.result === 'CANCELLED' || (selectedEvent?.result !== 'SUCCESS' && selectedEvent?.result !== 'FAILED' && selectedEvent?.result !== 'BLOCKED')">
              {{ translateResult(selectedEvent?.result || '') }}
            </span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">عنوان IP</span>
            <span class="font-mono text-slate-800">{{ selectedEvent?.ipAddress || '—' }}</span>
          </div>
          <div>
            <span class="text-slate-400 block mb-0.5 font-medium">مفتاح العنصر (Element Key)</span>
            <span class="font-mono text-slate-800">{{ selectedEvent?.elementKey || '—' }}</span>
          </div>
          <div class="md:col-span-3">
            <span class="text-slate-400 block mb-0.5 font-medium">الرسالة</span>
            <span class="text-slate-700 bg-white px-2.5 py-1.5 rounded border border-slate-200 block text-xs leading-relaxed font-sans">{{ selectedEvent?.message || '—' }}</span>
          </div>
          <div class="md:col-span-3">
            <span class="text-slate-400 block mb-0.5 font-medium">متصفح المستخدم (User Agent)</span>
            <span class="font-mono text-slate-600 bg-white px-2.5 py-1.5 rounded border border-slate-200 block text-[10px] break-all leading-normal">{{ selectedEvent?.userAgent || '—' }}</span>
          </div>
        </div>

        <!-- Metadata Section -->
        <div class="space-y-2">
          <h4 class="font-bold text-slate-850 text-xs">بيانات إضافية (Metadata JSON)</h4>
          <pre class="bg-slate-950 text-emerald-400 p-3 rounded-lg overflow-x-auto font-mono text-xs text-left" style="direction: ltr; unicode-bidi: embed;"><code>{{ prettyJson(selectedEvent?.metadataJson) }}</code></pre>
        </div>
      </div>

      <!-- Modal Footer -->
      <div class="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end">
        <button (click)="closeModal()" class="px-5 py-2 bg-primary text-white rounded-md text-xs font-semibold hover:bg-primary-dark transition duration-150 shadow-sm">
          إغلاق
        </button>
      </div>
    </div>
  </div>
</div>
  `
})
export class BlackBoxComponent implements OnInit {
  filterDateFrom = '';
  filterDateTo = '';
  filterEmployeeId: number | null = null;
  filterDeviceCode = '';
  filterActionType = '';
  filterPageName = '';
  filterResult = '';
  filterEntityType = '';
  filterEntityId = '';
  filterRoute = '';

  page = 1;
  pageSize = 50;
  totalCount = 0;

  loading = false;
  error: string | null = null;
  events: BlackBoxEventListItem[] = [];
  selectedEvent: BlackBoxEventDetailResponse | null = null;
  activeModal: 'details' | null = null;
  isForbidden = false;

  constructor(private apiService: BlackBoxApiService) {}

  ngOnInit() {
    this.loadEvents();
  }

  loadEvents() {
    this.loading = true;
    this.error = null;
    this.isForbidden = false;

    const params: any = {
      page: this.page,
      pageSize: this.pageSize
    };

    if (this.filterDateFrom) params.dateFrom = this.filterDateFrom;
    if (this.filterDateTo) params.dateTo = this.filterDateTo;
    if (this.filterEmployeeId !== null && this.filterEmployeeId !== undefined) params.employeeId = this.filterEmployeeId;
    if (this.filterDeviceCode.trim()) params.deviceCode = this.filterDeviceCode.trim();
    if (this.filterActionType.trim()) params.actionType = this.filterActionType.trim();
    if (this.filterPageName.trim()) params.pageName = this.filterPageName.trim();
    if (this.filterResult.trim()) params.result = this.filterResult.trim();
    if (this.filterEntityType.trim()) params.entityType = this.filterEntityType.trim();
    if (this.filterEntityId.trim()) params.entityId = this.filterEntityId.trim();
    if (this.filterRoute.trim()) params.route = this.filterRoute.trim();

    this.apiService.getEvents(params).subscribe({
      next: (res) => {
        this.events = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        if (err?.status === 403) {
          this.isForbidden = true;
        } else {
          this.error = err?.error?.error || 'حدث خطأ أثناء تحميل سجلات النشاط.';
        }
      }
    });
  }

  search() {
    this.page = 1;
    this.loadEvents();
  }

  resetFilters() {
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.filterEmployeeId = null;
    this.filterDeviceCode = '';
    this.filterActionType = '';
    this.filterPageName = '';
    this.filterResult = '';
    this.filterEntityType = '';
    this.filterEntityId = '';
    this.filterRoute = '';
    this.page = 1;
    this.loadEvents();
  }

  changePage(direction: number) {
    const target = this.page + direction;
    if (target < 1 || (direction > 0 && (target - 1) * this.pageSize >= this.totalCount)) {
      return;
    }
    this.page = target;
    this.loadEvents();
  }

  openDetails(id: number) {
    this.loading = true;
    this.apiService.getEvent(id).subscribe({
      next: (res) => {
        this.selectedEvent = res;
        this.activeModal = 'details';
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        alert(err?.error?.error || 'حدث خطأ أثناء تحميل تفاصيل السجل.');
      }
    });
  }

  closeModal() {
    this.activeModal = null;
    this.selectedEvent = null;
  }

  translateResult(res: string): string {
    const map: Record<string, string> = {
      SUCCESS: 'ناجح',
      FAILED: 'فشل',
      BLOCKED: 'محظور',
      CANCELLED: 'ملغى'
    };
    return map[res] || res;
  }

  prettyJson(jsonStr?: string | null): string {
    if (!jsonStr) return '—';
    try {
      const parsed = JSON.parse(jsonStr);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return jsonStr;
    }
  }
}
