import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-filter',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-slate-50 p-4 rounded-lg border border-slate-200 flex flex-wrap gap-4 items-end mb-4">
      <ng-content></ng-content>
      <div class="flex gap-2 mr-auto">
        <button (click)="apply.emit()" class="btn-primary">تطبيق</button>
        <button (click)="reset.emit()" class="btn-secondary">إعادة ضبط</button>
      </div>
    </div>
  `
})
export class FilterComponent {
  @Output() apply = new EventEmitter<void>();
  @Output() reset = new EventEmitter<void>();
}
