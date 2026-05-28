import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { 
  ProductsApiService, 
  ProductListItem, 
  CategoryItem, 
  CreateProductRequest, 
  UpdateProductRequest 
} from './products-api.service';
import { BlackBoxRecorderService } from '../../../core/services/black-box-recorder.service';

export interface ProductsState {
  products: ProductListItem[];
  categories: CategoryItem[];
  isLoading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProductsStateService {
  private stateObj: ProductsState = {
    products: [],
    categories: [],
    isLoading: false,
    error: null
  };

  private stateSubject = new BehaviorSubject<ProductsState>(this.stateObj);
  state$ = this.stateSubject.asObservable();

  constructor(
    private api: ProductsApiService,
    private blackBox: BlackBoxRecorderService
  ) {}

  private patchState(partial: Partial<ProductsState>) {
    this.stateObj = { ...this.stateObj, ...partial };
    this.stateSubject.next(this.stateObj);
  }

  loadInitialData() {
    this.patchState({ isLoading: true, error: null });
    
    // Load lookup data
    this.api.getCategoriesLookup().subscribe({
      next: (res) => this.patchState({ categories: res.items }),
      error: (err) => this.handleError(err)
    });

    // Load main list
    this.api.getProducts().subscribe({
      next: (res) => this.patchState({ products: res.items, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  refreshProducts() {
    this.patchState({ isLoading: true, error: null });
    this.api.getProducts().subscribe({
      next: (res) => this.patchState({ products: res.items, isLoading: false }),
      error: (err) => { this.handleError(err); this.patchState({ isLoading: false }); }
    });
  }

  createProduct(request: CreateProductRequest): Observable<any> {
    this.patchState({ isLoading: true, error: null });
    return this.api.createProduct(request).pipe(
      tap(() => {
        this.blackBox.recordSuccess('CREATE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          metadata: {
            barcode: request.barcode,
            categoryId: request.categoryId
          }
        });
        this.refreshProducts(); // reload list
      }),
      catchError(err => {
        this.blackBox.recordFailure('CREATE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          message: err?.error?.error || 'CREATE_PRODUCT_FAILED',
          metadata: {
            barcode: request.barcode,
            categoryId: request.categoryId
          }
        });
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  updateProduct(id: number, request: UpdateProductRequest): Observable<any> {
    this.patchState({ isLoading: true, error: null });
    return this.api.updateProduct(id, request).pipe(
      tap(() => {
        this.blackBox.recordSuccess('UPDATE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          entityId: id,
          metadata: {
            barcode: request.barcode,
            categoryId: request.categoryId,
            isActive: request.isActive
          }
        });
        this.refreshProducts();
      }),
      catchError(err => {
        this.blackBox.recordFailure('UPDATE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          entityId: id,
          message: err?.error?.error || 'UPDATE_PRODUCT_FAILED',
          metadata: {
            barcode: request.barcode,
            categoryId: request.categoryId
          }
        });
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  deleteProduct(id: number): Observable<any> {
    this.patchState({ isLoading: true, error: null });
    return this.api.deleteProduct(id).pipe(
      tap(() => {
        this.blackBox.recordSuccess('DELETE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          entityId: id
        });
        this.refreshProducts();
      }),
      catchError(err => {
        this.blackBox.recordFailure('DELETE_PRODUCT', {
          pageName: 'Products',
          entityType: 'Product',
          entityId: id,
          message: err?.error?.error || 'DELETE_PRODUCT_FAILED'
        });
        this.handleError(err);
        this.patchState({ isLoading: false });
        throw err;
      })
    );
  }

  setError(message: string): void {
    this.patchState({ error: message });
  }

  clearError() {
    this.patchState({ error: null });
  }

  private handleError(err: HttpErrorResponse) {
    let msg = 'حدث خطأ غير متوقع';
    if (err.error && err.error.error) {
      const code = err.error.error;
      switch(code) {
        case 'PRODUCT_BARCODE_ALREADY_EXISTS': msg = 'هذا الباركود مستخدم بالفعل'; break;
        case 'BARCODE_ALREADY_EXISTS': msg = 'الباركود موجود مسبقاً، يرجى استخدام باركود مختلف'; break;
        case 'CATEGORY_NOT_FOUND': msg = 'التصنيف غير موجود'; break;
        default: msg = code;
      }
    } else if (err.status === 409) {
      msg = 'هذا الباركود مستخدم بالفعل';
    } else if (err.status === 403) {
      msg = 'غير مصرح لك بإجراء هذه العملية';
    }
    this.patchState({ error: msg });
  }
}
