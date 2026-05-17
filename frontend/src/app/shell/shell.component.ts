import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { SidebarComponent } from './sidebar/sidebar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, SidebarComponent],
  template: `
    <div class="flex h-screen overflow-hidden bg-background">
      <app-sidebar class="w-64 flex-shrink-0 border-l border-slate-200 hidden md:block"></app-sidebar>
      <div class="flex flex-col flex-1 min-w-0">
        <app-header class="h-16 border-b border-slate-200 flex-shrink-0"></app-header>
        <main class="flex-1 overflow-auto p-4 md:p-6">
          <div class="mx-auto max-w-7xl h-full">
            <router-outlet></router-outlet>
          </div>
        </main>
      </div>
    </div>
  `
})
export class ShellComponent {}
