import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';

@Component({
  selector: 'app-header',
  standalone: true,
  template: `
    <header class="bg-surface flex items-center justify-between h-full px-4 md:px-6 shadow-sm">
      <div class="flex items-center gap-4">
        <!-- Mobile menu toggle (omitted logic for foundation simplicity) -->
        <button class="md:hidden text-slate-500 hover:text-slate-700">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <h2 class="text-xl font-bold text-slate-800 hidden sm:block">Bazarflow</h2>
      </div>
      <div class="flex items-center gap-4">
        <div class="text-sm font-medium text-slate-600">{{ username }}</div>
        <button (click)="onLogout()" class="btn-secondary text-sm py-1 px-3">Logout</button>
      </div>
    </header>
  `
})
export class HeaderComponent implements OnInit {
  username = 'Username';

  constructor(
    private authService: AuthService,
    private sessionService: SessionService,
    private router: Router
  ) {}

  ngOnInit() {
    const employee = this.sessionService.getEmployee();
    if (employee && employee.fullName) {
      this.username = employee.fullName;
    }
  }

  onLogout() {
    this.authService.logout().subscribe(() => {
      this.router.navigate(['/login']);
    });
  }
}
