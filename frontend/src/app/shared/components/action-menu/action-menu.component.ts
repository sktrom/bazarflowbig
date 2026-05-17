import { Component, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-action-menu',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative inline-block text-right" (click)="toggleMenu($event)">
      <button class="text-slate-500 hover:text-slate-700 p-1 rounded focus:outline-none focus:ring-2 focus:ring-primary">
        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"></path></svg>
      </button>
      
      <div *ngIf="isOpen" class="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg border border-slate-200 z-10 py-1">
        <ng-content></ng-content>
      </div>
    </div>
  `
})
export class ActionMenuComponent {
  isOpen = false;

  constructor(private eRef: ElementRef) {}

  toggleMenu(event: Event) {
    event.stopPropagation();
    this.isOpen = !this.isOpen;
  }

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if(!this.eRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }
}
