import { Injectable } from '@angular/core';

export interface StoredEmployee {
  employeeId: number;
  fullName: string;
  deviceCode: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly SESSION_KEY = 'x_session_id';
  private readonly EMPLOYEE_KEY = 'session_employee';

  // sessionId is numeric (long) from backend — stored as string representation
  setSessionId(sessionId: number): void {
    localStorage.setItem(this.SESSION_KEY, sessionId.toString());
  }

  getSessionId(): string | null {
    return localStorage.getItem(this.SESSION_KEY);
  }

  setEmployee(employee: StoredEmployee): void {
    localStorage.setItem(this.EMPLOYEE_KEY, JSON.stringify(employee));
  }

  getEmployee(): StoredEmployee | null {
    const raw = localStorage.getItem(this.EMPLOYEE_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as StoredEmployee; } catch { return null; }
  }

  clearSession(): void {
    localStorage.removeItem(this.SESSION_KEY);
    localStorage.removeItem(this.EMPLOYEE_KEY);
  }
}
