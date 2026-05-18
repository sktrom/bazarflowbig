import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private permissionsSubject: BehaviorSubject<string[]>;
  permissions$: Observable<string[]>;

  constructor(private sessionService: SessionService) {
    const initialPermissions = this.sessionService.getPermissions();
    this.permissionsSubject = new BehaviorSubject<string[]>(initialPermissions);
    this.permissions$ = this.permissionsSubject.asObservable();
  }

  setPermissions(permissions: string[]): void {
    this.sessionService.setPermissions(permissions);
    this.permissionsSubject.next(permissions);
  }

  hasPermission(screenKey: string): boolean {
    return this.permissionsSubject.value.includes(screenKey);
  }
}
