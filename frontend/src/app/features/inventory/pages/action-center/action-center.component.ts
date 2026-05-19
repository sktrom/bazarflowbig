import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActionCenterApiService } from '../../services/action-center-api.service';
import { ActionCenterResponseDto } from '../../models/action-center.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-action-center',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="p-6 rtl text-right" dir="rtl">
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-800">مركز القرارات</h1>
        <p class="text-sm text-gray-500">ماذا يجب أن أفعل اليوم؟</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="flex justify-center items-center py-20">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="bg-red-50 text-red-600 p-4 rounded-lg mb-6">
        {{ errorMessage }}
        <button (click)="loadData()" class="text-red-700 underline mr-2 font-bold">إعادة المحاولة</button>
      </div>

      <div *ngIf="!isLoading && data">
        
        <!-- Summary Cards -->
        <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
          
          <div class="bg-red-50 border border-red-100 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'outOfStock'">
            <div class="text-3xl font-bold text-red-600 mb-1">{{ data.summary.outOfStockCount }}</div>
            <div class="text-sm font-medium text-red-800">نفذت الكمية</div>
          </div>

          <div class="bg-orange-50 border border-orange-100 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'lowStock'">
            <div class="text-3xl font-bold text-orange-600 mb-1">{{ data.summary.lowStockCount }}</div>
            <div class="text-sm font-medium text-orange-800">مخزون منخفض</div>
          </div>

          <div class="bg-red-100 border border-red-200 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'expired'">
            <div class="text-3xl font-bold text-red-700 mb-1">{{ data.summary.expiredBatchesCount }}</div>
            <div class="text-sm font-medium text-red-900">منتهي الصلاحية</div>
          </div>

          <div class="bg-yellow-50 border border-yellow-100 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'expiringSoon'">
            <div class="text-3xl font-bold text-yellow-600 mb-1">{{ data.summary.expiringSoonBatchesCount }}</div>
            <div class="text-sm font-medium text-yellow-800">قريب الانتهاء</div>
          </div>

          <div class="bg-gray-50 border border-gray-200 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'inactive'">
            <div class="text-3xl font-bold text-gray-600 mb-1">{{ data.summary.inactiveWithStockCount }}</div>
            <div class="text-sm font-medium text-gray-800">خامل بمخزون</div>
          </div>

          <div class="bg-blue-50 border border-blue-100 rounded-xl p-4 text-center cursor-pointer hover:shadow-md transition-shadow" (click)="activeTab = 'offers'">
            <div class="text-3xl font-bold text-blue-600 mb-1">{{ data.summary.offerCandidatesCount }}</div>
            <div class="text-sm font-medium text-blue-800">مرشح لعرض</div>
          </div>

        </div>

        <!-- Top Urgent Actions -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 mb-8" *ngIf="data.topUrgentActions.length > 0">
          <h2 class="text-lg font-bold text-gray-800 mb-4 flex items-center">
            <span class="bg-red-100 text-red-600 px-2 py-0.5 rounded text-xs font-bold ml-2">عاجل</span>
            أهم الإجراءات المطلوبة
          </h2>
          
          <div class="space-y-3">
            <div *ngFor="let action of data.topUrgentActions" 
                 class="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-lg border"
                 [ngClass]="{'bg-red-50 border-red-100': action.severity === 'HIGH', 'bg-orange-50 border-orange-100': action.severity === 'MEDIUM'}">
              
              <div class="flex items-start mb-2 sm:mb-0">
                <div class="ml-4 mt-1">
                  <div *ngIf="action.severity === 'HIGH'" class="h-3 w-3 bg-red-500 rounded-full animate-pulse"></div>
                  <div *ngIf="action.severity === 'MEDIUM'" class="h-3 w-3 bg-orange-400 rounded-full"></div>
                </div>
                <div>
                  <h3 class="font-bold text-gray-900">{{ action.productName }} <span class="text-xs text-gray-500 font-normal">({{ action.barcode }})</span></h3>
                  <p class="text-sm mt-1" [ngClass]="{'text-red-700': action.severity === 'HIGH', 'text-orange-700': action.severity === 'MEDIUM'}">
                    {{ action.message }}
                  </p>
                </div>
              </div>

              <div class="shrink-0 sm:mr-4">
                <span class="inline-block px-3 py-1 rounded text-sm font-bold bg-white shadow-sm border border-gray-200">
                  {{ action.recommendedAction }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty State for Urgent Actions -->
        <div *ngIf="data.topUrgentActions.length === 0" class="bg-green-50 border border-green-100 rounded-xl p-8 text-center mb-8">
          <div class="text-green-500 mb-2 text-4xl">🎉</div>
          <h3 class="text-lg font-bold text-green-800">كل شيء على ما يرام!</h3>
          <p class="text-green-600 mt-1">لا توجد إجراءات عاجلة مطلوبة في الوقت الحالي.</p>
        </div>

        <!-- Detail Tabs -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div class="flex overflow-x-auto border-b border-gray-200 bg-gray-50">
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'outOfStock', 'text-gray-600 hover:text-gray-800': activeTab !== 'outOfStock'}"
                    (click)="activeTab = 'outOfStock'">
              نفذت الكمية ({{ data.outOfStock.length }})
            </button>
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'lowStock', 'text-gray-600 hover:text-gray-800': activeTab !== 'lowStock'}"
                    (click)="activeTab = 'lowStock'">
              مخزون منخفض ({{ data.lowStock.length }})
            </button>
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'expired', 'text-gray-600 hover:text-gray-800': activeTab !== 'expired'}"
                    (click)="activeTab = 'expired'">
              منتهي الصلاحية ({{ data.expired.length }})
            </button>
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'expiringSoon', 'text-gray-600 hover:text-gray-800': activeTab !== 'expiringSoon'}"
                    (click)="activeTab = 'expiringSoon'">
              قريب الانتهاء ({{ data.expiringSoon.length }})
            </button>
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'inactive', 'text-gray-600 hover:text-gray-800': activeTab !== 'inactive'}"
                    (click)="activeTab = 'inactive'">
              خامل ({{ data.inactiveWithStock.length }})
            </button>
            <button class="px-6 py-3 text-sm font-medium whitespace-nowrap focus:outline-none"
                    [ngClass]="{'text-blue-600 border-b-2 border-blue-600 bg-white': activeTab === 'offers', 'text-gray-600 hover:text-gray-800': activeTab !== 'offers'}"
                    (click)="activeTab = 'offers'">
              عروض مرشحة ({{ data.offerCandidates.length }})
            </button>
          </div>

          <div class="p-0">
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                  <tr>
                    <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">المنتج</th>
                    <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">الباركود</th>
                    <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">المخزون الحالي</th>
                    <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider" *ngIf="activeTab === 'expired' || activeTab === 'expiringSoon'">تاريخ الانتهاء</th>
                  </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                  <tr *ngIf="getActiveList().length === 0">
                    <td colspan="4" class="px-6 py-8 text-center text-gray-500">لا توجد سجلات في هذه القائمة.</td>
                  </tr>
                  <tr *ngFor="let item of getActiveList()" class="hover:bg-gray-50">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{{ item.productName }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{{ item.barcode }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm">
                      <span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-100 text-gray-800">
                        {{ item.currentStock }}
                      </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500" *ngIf="activeTab === 'expired' || activeTab === 'expiringSoon'">
                      <span [ngClass]="{'text-red-600 font-bold': activeTab === 'expired'}">
                        {{ getAsBatch(item).expiryDate | date:'yyyy-MM-dd' }}
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ActionCenterComponent implements OnInit {
  private readonly apiService = inject(ActionCenterApiService);

  isLoading = false;
  errorMessage = '';
  data: ActionCenterResponseDto | null = null;
  
  activeTab: 'outOfStock' | 'lowStock' | 'expired' | 'expiringSoon' | 'inactive' | 'offers' = 'outOfStock';

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.data = null;

    this.apiService.getActionCenterSummary()
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (res) => {
          this.data = res;
          // Set default tab based on what has data
          if (res.outOfStock.length > 0) this.activeTab = 'outOfStock';
          else if (res.expired.length > 0) this.activeTab = 'expired';
          else if (res.expiringSoon.length > 0) this.activeTab = 'expiringSoon';
          else if (res.lowStock.length > 0) this.activeTab = 'lowStock';
        },
        error: (err) => {
          this.errorMessage = 'حدث خطأ أثناء تحميل بيانات مركز القرارات. يرجى المحاولة مرة أخرى.';
          console.error('Error loading action center data', err);
        }
      });
  }

  getActiveList(): any[] {
    if (!this.data) return [];
    
    switch (this.activeTab) {
      case 'outOfStock': return this.data.outOfStock;
      case 'lowStock': return this.data.lowStock;
      case 'expired': return this.data.expired;
      case 'expiringSoon': return this.data.expiringSoon;
      case 'inactive': return this.data.inactiveWithStock;
      case 'offers': return this.data.offerCandidates;
      default: return [];
    }
  }

  getAsBatch(item: any): any {
    return item;
  }
}
