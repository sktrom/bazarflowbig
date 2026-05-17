import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SettingsBootstrapService {
  private settingsSubject = new BehaviorSubject<any>(null);
  settings$ = this.settingsSubject.asObservable();

  constructor(private http: HttpClient) {}

  loadSettings() {
    return () => {
      // Return a Promise so APP_INITIALIZER waits for it
      return new Promise<void>((resolve) => {
        this.http.get(`${environment.apiUrl}/api/settings/public`).pipe(
          tap(settings => {
            this.settingsSubject.next(settings);
            resolve();
          }),
          catchError(err => {
            console.error('Failed to bootstrap settings', err);
            // Even if it fails, we resolve so the app can start (maybe show offline mode)
            resolve();
            return of(null);
          })
        ).subscribe();
      });
    };
  }
}
