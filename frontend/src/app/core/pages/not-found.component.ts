import { Component } from '@angular/core';

@Component({
  selector: 'app-not-found',
  standalone: true,
  template: `
    <div class="flex items-center justify-center h-screen bg-slate-50">
      <div class="text-center">
        <h1 class="text-6xl font-bold text-slate-400 mb-4">404</h1>
        <p class="text-xl text-slate-700">الصفحة المطلوبة غير موجودة.</p>
      </div>
    </div>
  `
})
export class NotFoundComponent {}
