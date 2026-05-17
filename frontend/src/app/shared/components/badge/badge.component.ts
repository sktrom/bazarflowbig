import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="getBadgeClass()">
      <ng-content></ng-content>
    </span>
  `
})
export class BadgeComponent {
  @Input() type: 'success' | 'danger' | 'warning' | 'info' | 'default' = 'default';

  getBadgeClass(): string {
    switch (this.type) {
      case 'success': return 'badge-success';
      case 'danger': return 'badge-danger';
      case 'warning': return 'badge-warning';
      case 'info': return 'bg-blue-100 text-blue-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  }
}
