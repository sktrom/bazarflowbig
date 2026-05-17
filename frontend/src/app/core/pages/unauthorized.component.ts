import { Component } from '@angular/core';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  template: `
    <div class="flex items-center justify-center h-screen bg-slate-50">
      <div class="text-center">
        <h1 class="text-6xl font-bold text-red-500 mb-4">403</h1>
        <p class="text-xl text-slate-700">عذراً، غير مصرح لك بالوصول إلى هذه الصفحة.</p>
      </div>
    </div>
  `
})
export class UnauthorizedComponent {}
