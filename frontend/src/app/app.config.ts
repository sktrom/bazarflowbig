import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { routes } from './app.routes';
import { SettingsBootstrapService } from './core/services/settings-bootstrap.service';
import { SessionInterceptor } from './core/interceptors/session.interceptor';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';

export function initializeApp(settingsBootstrapService: SettingsBootstrapService) {
  return settingsBootstrapService.loadSettings();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [SettingsBootstrapService],
      multi: true
    },
    { provide: HTTP_INTERCEPTORS, useClass: SessionInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true }
  ]
};
