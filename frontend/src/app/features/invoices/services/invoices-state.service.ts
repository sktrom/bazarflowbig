import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { InvoicesApiService, InvoiceListItem, InvoiceListResponse } from './invoices-api.service';
import { tap, catchError } from 'rxjs/operators';

export interface InvoicesState {
  items: InvoiceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  error: string | null;
  filters: any;
}

@Injectable({ providedIn: 'root' })
export class InvoicesStateService {
  private initialState: InvoicesState = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    isLoading: false,
    error: null,
    filters: {}
  };

  private stateSubject = new BehaviorSubject<InvoicesState>(this.initialState);
  state$ = this.stateSubject.asObservable();

  constructor(private api: InvoicesApiService) {}

  private patchState(partial: Partial<InvoicesState>) {
    const current = this.stateSubject.value;
    this.stateSubject.next({ ...current, ...partial });
  }

  loadInvoices(filters?: any, page: number = 1) {
    const currentFilters = filters || this.stateSubject.value.filters;
    this.patchState({ isLoading: true, error: null, filters: currentFilters, page });

    this.api.getInvoices({ ...currentFilters, page, pageSize: this.stateSubject.value.pageSize }).subscribe({
      next: (res) => {
        this.patchState({
          items: res.items,
          totalCount: res.totalCount,
          isLoading: false
        });
      },
      error: (err) => {
        this.patchState({
          isLoading: false,
          error: err.error?.error || 'حدث خطأ أثناء تحميل الفواتير'
        });
      }
    });
  }

  clearError() {
    this.patchState({ error: null });
  }

  setError(message: string) {
    this.patchState({ error: message });
  }
}
