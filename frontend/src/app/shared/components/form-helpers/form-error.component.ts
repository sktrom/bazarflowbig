import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-error',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="show" class="text-sm text-red-500 mt-1">
      <ng-content></ng-content>
    </div>
  `
})
export class FormErrorComponent {
  @Input() show: boolean = false;
}
