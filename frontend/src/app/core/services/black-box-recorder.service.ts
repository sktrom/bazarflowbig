import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BlackBoxApiService, CreateBlackBoxEventRequest } from './black-box-api.service';

export interface BlackBoxRecordContext {
  pageName?: string | null;
  elementKey?: string | null;
  entityType?: string | null;
  entityId?: string | number | null;
  message?: string | null;
  metadata?: Record<string, unknown> | null;
}

@Injectable({ providedIn: 'root' })
export class BlackBoxRecorderService {
  private readonly sensitiveKeyParts = [
    'password',
    'pwd',
    'token',
    'secret',
    'connectionstring',
    'authorization',
    'sessiontoken',
    'passwordhash',
    'passwordsalt',
    'apikey'
  ];

  constructor(
    private api: BlackBoxApiService,
    private router: Router
  ) {}

  recordSuccess(actionType: string, context: BlackBoxRecordContext = {}): void {
    this.record(actionType, 'SUCCESS', context);
  }

  recordFailure(actionType: string, context: BlackBoxRecordContext = {}): void {
    this.record(actionType, 'FAILED', context);
  }

  recordBlocked(actionType: string, context: BlackBoxRecordContext = {}): void {
    this.record(actionType, 'BLOCKED', context);
  }

  recordCancelled(actionType: string, context: BlackBoxRecordContext = {}): void {
    this.record(actionType, 'CANCELLED', context);
  }

  sanitizeMetadata(metadata?: Record<string, unknown> | null): Record<string, unknown> | null {
    if (!metadata || typeof metadata !== 'object') {
      return null;
    }

    const sanitized = this.sanitizeValue(metadata);
    if (!sanitized || Array.isArray(sanitized) || typeof sanitized !== 'object') {
      return null;
    }

    return Object.keys(sanitized).length ? sanitized as Record<string, unknown> : null;
  }

  private record(actionType: string, result: string, context: BlackBoxRecordContext): void {
    const request: CreateBlackBoxEventRequest = {
      route: this.router.url || null,
      pageName: context.pageName || this.inferPageName(this.router.url),
      actionType,
      elementKey: context.elementKey || null,
      entityType: context.entityType || null,
      entityId: context.entityId === undefined || context.entityId === null ? null : String(context.entityId),
      result,
      message: context.message || null,
      metadata: this.sanitizeMetadata(context.metadata)
    };

    this.api.createEvent(request).pipe(
      catchError(() => EMPTY)
    ).subscribe();
  }

  private sanitizeValue(value: unknown): unknown {
    if (Array.isArray(value)) {
      return value.map(item => this.sanitizeValue(item)).filter(item => item !== undefined);
    }

    if (value && typeof value === 'object') {
      const result: Record<string, unknown> = {};
      Object.entries(value as Record<string, unknown>).forEach(([key, nestedValue]) => {
        if (this.isSensitiveKey(key)) {
          return;
        }

        const sanitized = this.sanitizeValue(nestedValue);
        if (sanitized !== undefined) {
          result[key] = sanitized;
        }
      });
      return result;
    }

    return value;
  }

  private isSensitiveKey(key: string): boolean {
    const normalized = key.toLowerCase();
    return this.sensitiveKeyParts.some(part => normalized.includes(part));
  }

  private inferPageName(route: string): string | null {
    const segment = (route || '').split('?')[0].split('/').filter(Boolean)[0];
    return segment || null;
  }
}
