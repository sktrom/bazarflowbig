import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CartLineDto {
  lineId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPriceUsdOriginal: number;
  lineTotalUsdOriginal: number;
  lineTotalUsdEffective: number;
  isPriceOverridden: boolean;
  offerId?: number;
}

export interface CartResponse {
  invoiceId?: number;
  status: string;
  customerName?: string;
  invoiceDiscountType?: string;
  invoiceDiscountValue?: number;
  subtotalUsd: number;
  totalUsd: number;
  lines: CartLineDto[];
}

export interface ProductDto {
  id: number;
  name: string;
  barcode: string;
  basePriceUsd: number;
  categoryId: number;
  categoryName: string;
  stockQuantity: number;
}

export interface CategoryDto {
  id: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class CashierApiService {
  private readonly cartBase = `${environment.apiUrl}/api/cashier/cart`;
  private readonly productsBase = `${environment.apiUrl}/api/cashier/products`;
  private readonly categoriesBase = `${environment.apiUrl}/api/categories`; // Allowed for all? We'll see, maybe better to just extract from products.

  constructor(private http: HttpClient) {}

  // Products Source for Cashier
  getCashierProducts(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(this.productsBase);
  }

  // Categories (assuming accessible or we can derive them)
  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(this.categoriesBase);
  }

  // Cart Load
  getCurrentCart(): Observable<CartResponse> {
    return this.http.get<CartResponse>(`${this.cartBase}/current`);
  }

  // Cart Add
  addByBarcode(barcode: string): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.cartBase}/items/by-barcode`, { barcode });
  }

  addByProduct(productId: number): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.cartBase}/items/by-product`, { productId });
  }

  // Cart Line Edit/Delete
  updateLine(lineId: number, quantity?: number, overrideLineTotalUsd?: number | null): Observable<CartResponse> {
    return this.http.patch<CartResponse>(`${this.cartBase}/items/${lineId}`, { quantity, overrideLineTotalUsd });
  }

  deleteLine(lineId: number): Observable<CartResponse> {
    return this.http.delete<CartResponse>(`${this.cartBase}/items/${lineId}`);
  }

  // Customer & Discount
  updateCustomer(customerName: string): Observable<CartResponse> {
    return this.http.put<CartResponse>(`${this.cartBase}/customer`, { customerName });
  }

  deleteCustomer(): Observable<CartResponse> {
    return this.http.delete<CartResponse>(`${this.cartBase}/customer`);
  }

  updateDiscount(discountType: string, discountValue: number): Observable<CartResponse> {
    return this.http.patch<CartResponse>(`${this.cartBase}/discount`, { discountType, discountValue });
  }

  // Finalization
  suspendCart(suspensionReason: string): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.cartBase}/suspend`, { suspensionReason });
  }

  completeCart(): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.cartBase}/complete`, {});
  }

  cancelCart(): Observable<CartResponse> {
    return this.http.delete<CartResponse>(`${this.cartBase}/current`);
  }
}
