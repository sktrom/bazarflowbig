import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private permissionsSubject = new BehaviorSubject<string[]>([]);
  permissions$ = this.permissionsSubject.asObservable();

  setPermissions(permissions: string[]): void {
    this.permissionsSubject.next(permissions);
  }

  hasPermission(screenKey: string): boolean {
    // If the list is empty, we might not have loaded them yet, or user has none.
    // In a real scenario, this gets populated after successful login/session restore.
    return this.permissionsSubject.value.includes(screenKey);
  }
}
