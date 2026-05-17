import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { CashierApiService, CartResponse, ProductDto } from './cashier-api.service';
import { HttpErrorResponse } from '@angular/common/http';

export interface CashierState {
  cart: CartResponse | null;
  products: ProductDto[];
  isLoading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class CashierStateService {
  private stateObj: CashierState = {
    cart: null,
    products: [],
    isLoading: false,
    error: null
  };

  private stateSubject = new BehaviorSubject<CashierState>(this.stateObj);
  state$ = this.stateSubject.asObservable();

  constructor(private api: CashierApiService) {}

  private patchState(partial: Partial<CashierState>) {
    this.stateObj = { ...this.stateObj, ...partial };
    this.stateSubject.next(this.stateObj);
  }

  // --- Initial Load ---
  
  loadInitialState() {
    this.patchState({ isLoading: true, error: null });
    
    // Load products and current cart independently or chained
    // We'll just trigger them and let state update as they arrive
    this.api.getCashierProducts().subscribe({
      next: (prods) => this.patchState({ products: prods }),
      error: (err) => this.handleError(err)
    });

    this.api.getCurrentCart().subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => {
        // 404 NO_WORKING_CART_EXISTS means empty state, not an error for the UI
        if (err.status === 404 && err.error?.error === 'NO_WORKING_CART_EXISTS') {
          this.patchState({ cart: this.getEmptyCart(), isLoading: false, error: null });
        } else {
          this.handleError(err);
          this.patchState({ isLoading: false });
        }
      }
    });
  }

  // --- Cart Operations ---

  addByProduct(productId: number) {
    this.patchState({ isLoading: true, error: null });
    this.api.addByProduct(productId).subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  addByBarcode(barcode: string) {
    this.patchState({ isLoading: true, error: null });
    this.api.addByBarcode(barcode).subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  updateLine(lineId: number, quantity?: number, overrideLineTotalUsd?: number | null) {
    this.patchState({ isLoading: true, error: null });
    this.api.updateLine(lineId, quantity, overrideLineTotalUsd).subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  deleteLine(lineId: number) {
    this.patchState({ isLoading: true, error: null });
    this.api.deleteLine(lineId).subscribe({
      next: (cart) => {
        if (!cart || !cart.lines || cart.lines.length === 0) {
          this.patchState({ cart: this.getEmptyCart(), isLoading: false });
        } else {
          this.patchState({ cart, isLoading: false });
        }
      },
      error: (err) => { 
        if (err.status === 404) {
          // If deleted last line and cart is gone
          this.patchState({ cart: this.getEmptyCart(), isLoading: false });
        } else {
          this.handleError(err); 
          this.patchState({ isLoading: false }); 
        }
      }
    });
  }

  // --- Customer / Discount ---

  updateCustomer(name: string) {
    this.patchState({ isLoading: true, error: null });
    this.api.updateCustomer(name).subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  deleteCustomer() {
    this.patchState({ isLoading: true, error: null });
    this.api.deleteCustomer().subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  updateDiscount(type: string, val: number) {
    this.patchState({ isLoading: true, error: null });
    this.api.updateDiscount(type, val).subscribe({
      next: (cart) => this.patchState({ cart, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  // --- Finalization ---

  suspendCart(reason: string): Observable<CartResponse> {
    this.patchState({ isLoading: true, error: null });
    return this.api.suspendCart(reason).pipe(
      tap(() => this.patchState({ cart: this.getEmptyCart(), isLoading: false })),
      catchError(err => {
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  completeCart(): Observable<CartResponse> {
    this.patchState({ isLoading: true, error: null });
    return this.api.completeCart().pipe(
      tap(() => this.patchState({ cart: this.getEmptyCart(), isLoading: false })),
      catchError(err => {
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  cancelCart(): Observable<CartResponse> {
    this.patchState({ isLoading: true, error: null });
    return this.api.cancelCart().pipe(
      tap(() => this.patchState({ cart: this.getEmptyCart(), isLoading: false })),
      catchError(err => {
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  clearError() {
    this.patchState({ error: null });
  }

  // --- Helpers ---

  private getEmptyCart(): CartResponse {
    return {
      status: 'Working',
      subtotalUsd: 0,
      totalUsd: 0,
      lines: []
    };
  }

  private handleError(err: HttpErrorResponse) {
    let msg = 'حدث خطأ غير متوقع';
    if (err.error && err.error.error) {
      const code = err.error.error;
      switch(code) {
        case 'CUSTOMER_NAME_REQUIRED': msg = 'اسم العميل مطلوب لهذه العملية'; break;
        case 'INSUFFICIENT_INVENTORY': msg = 'الكمية غير متوفرة في المخزون'; break;
        case 'INVALID_QUANTITY': msg = 'الكمية غير صحيحة'; break;
        case 'NO_WORKING_CART_EXISTS': msg = 'لا توجد فاتورة نشطة'; break;
        default: msg = code;
      }
    } else if (err.status === 403) {
      msg = 'غير مصرح لك بإجراء هذه العملية';
    }
    this.patchState({ error: msg });
  }
}
