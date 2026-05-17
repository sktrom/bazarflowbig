import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ProductListItem {
  id: number;
  name: string;
  barcode: string;
  categoryId: number;
  categoryName: string;
  priceUsd: number;
  isActive: boolean;
}

export interface ProductListResponse {
  items: ProductListItem[];
}

export interface ProductDetailResponse {
  id: number;
  name: string;
  barcode: string;
  categoryId: number;
  baseUnit: string;
  priceUsd: number;
  hasCarton: boolean;
  cartonQuantity?: number;
  cartonPriceUsd?: number;
  hasExpiry: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  name: string;
  barcode: string;
  categoryId: number;
  baseUnit: string;
  priceUsd: number;
  hasCarton: boolean;
  cartonQuantity?: number | null;
  cartonPriceUsd?: number | null;
  hasExpiry: boolean;
}

export interface UpdateProductRequest extends CreateProductRequest {
  isActive: boolean;
}

export interface BatchItem {
  id: number;
  productId: number;
  quantityOriginal: number;
  quantityRemaining: number;
  unitCostUsd: number;
  expiryDate?: string | null;
  createdAt: string;
  isDepleted: boolean;
}

export interface BatchListResponse {
  items: BatchItem[];
}

export interface CreateBatchRequest {
  quantity: number;
  unitCostUsd: number;
  expiryDate?: string | null;
}

export interface CategoryItem {
  id: number;
  name: string;
  isActive: boolean;
}

export interface CategoryListResponse {
  items: CategoryItem[];
}

@Injectable({ providedIn: 'root' })
export class ProductsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/products`;

  constructor(private http: HttpClient) {}

  // Products
  getProducts(): Observable<ProductListResponse> {
    return this.http.get<ProductListResponse>(this.baseUrl);
  }

  getProduct(id: number): Observable<ProductDetailResponse> {
    return this.http.get<ProductDetailResponse>(`${this.baseUrl}/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<ProductDetailResponse> {
    return this.http.post<ProductDetailResponse>(this.baseUrl, request);
  }

  updateProduct(id: number, request: UpdateProductRequest): Observable<ProductDetailResponse> {
    return this.http.put<ProductDetailResponse>(`${this.baseUrl}/${id}`, request);
  }

  deleteProduct(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  // Batches
  getBatches(productId: number): Observable<BatchListResponse> {
    return this.http.get<BatchListResponse>(`${this.baseUrl}/${productId}/batches`);
  }

  createBatch(productId: number, request: CreateBatchRequest): Observable<BatchItem> {
    return this.http.post<BatchItem>(`${this.baseUrl}/${productId}/batches`, request);
  }

  // Categories Lookup
  getCategoriesLookup(): Observable<CategoryListResponse> {
    return this.http.get<CategoryListResponse>(`${this.baseUrl}/categories-lookup`);
  }

  // Export Entry
  exportProducts(): Observable<Blob> {
    return this.http.post(`${environment.apiUrl}/api/exports/products`, {}, { responseType: 'blob' });
  }
}
