import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="overflow-x-auto rounded-lg border border-slate-200 shadow-sm">
      <table class="w-full text-right text-sm text-slate-600">
        <thead class="bg-slate-50 text-slate-700">
          <tr>
            <th *ngFor="let col of columns" class="px-4 py-3 font-semibold">{{ col.label }}</th>
            <th *ngIf="hasActions" class="px-4 py-3 font-semibold">الإجراءات</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-200 bg-white">
          <tr *ngFor="let row of data" class="hover:bg-slate-50 transition-colors">
            <td *ngFor="let col of columns" class="px-4 py-3">{{ row[col.key] }}</td>
            <td *ngIf="hasActions" class="px-4 py-3">
              <ng-content select="[actions]"></ng-content>
            </td>
          </tr>
          <tr *ngIf="!data || data.length === 0">
            <td [attr.colspan]="columns.length + (hasActions ? 1 : 0)" class="px-4 py-8 text-center text-slate-500">
              لا توجد بيانات للعرض
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class TableComponent {
  @Input() columns: { key: string; label: string }[] = [];
  @Input() data: any[] = [];
  @Input() hasActions: boolean = false;
}
