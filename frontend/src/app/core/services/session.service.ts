import { Injectable } from '@angular/core';

export interface StoredEmployee {
  employeeId: number;
  fullName: string;
  deviceCode: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly SESSION_KEY = 'x_session_id';
  private readonly SESSION_TOKEN_KEY = 'x_session_token';
  private readonly EMPLOYEE_KEY = 'session_employee';

  private readonly PERMISSIONS_KEY = 'session_permissions';
  private readonly DEVICE_CODE_KEY = 'bazarflow_device_code';

  // sessionId is numeric (long) from backend — stored as string representation
  setSessionId(sessionId: number): void {
    localStorage.setItem(this.SESSION_KEY, sessionId.toString());
  }

  getSessionId(): string | null {
    return localStorage.getItem(this.SESSION_KEY);
  }

  setSessionToken(token: string): void {
    localStorage.setItem(this.SESSION_TOKEN_KEY, token);
  }

  getSessionToken(): string | null {
    return localStorage.getItem(this.SESSION_TOKEN_KEY);
  }

  setEmployee(employee: StoredEmployee): void {
    localStorage.setItem(this.EMPLOYEE_KEY, JSON.stringify(employee));
  }

  getEmployee(): StoredEmployee | null {
    const raw = localStorage.getItem(this.EMPLOYEE_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as StoredEmployee; } catch { return null; }
  }

  setPermissions(permissions: string[]): void {
    localStorage.setItem(this.PERMISSIONS_KEY, JSON.stringify(permissions));
  }

  getPermissions(): string[] {
    const raw = localStorage.getItem(this.PERMISSIONS_KEY);
    if (!raw) return [];
    try { return JSON.parse(raw) as string[]; } catch { return []; }
  }

  setDeviceCode(code: string): void {
    localStorage.setItem(this.DEVICE_CODE_KEY, code);
  }

  getDeviceCode(): string | null {
    return localStorage.getItem(this.DEVICE_CODE_KEY);
  }

  clearDeviceCode(): void {
    localStorage.removeItem(this.DEVICE_CODE_KEY);
  }

  clearSession(): void {
    localStorage.removeItem(this.SESSION_TOKEN_KEY);
    localStorage.removeItem(this.SESSION_KEY);
    localStorage.removeItem(this.EMPLOYEE_KEY);
    localStorage.removeItem(this.PERMISSIONS_KEY);
  }
}
