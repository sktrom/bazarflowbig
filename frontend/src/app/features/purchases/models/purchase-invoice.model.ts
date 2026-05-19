export interface PurchaseInvoiceListItem {
  id: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  externalInvoiceNumber?: string | null;
  subtotalUsd: number;
  totalUsd: number;
  createdAt: string;
  updatedAt: string;
}

export interface PurchaseInvoiceListResponse {
  items: PurchaseInvoiceListItem[];
}

export interface PurchaseInvoiceDetailResponse {
  id: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string;
  createdByEmployeeId: number;
  createdByEmployeeName: string;
  status: string;
  externalInvoiceNumber?: string | null;
  notes?: string | null;
  subtotalUsd: number;
  totalUsd: number;
  createdAt: string;
  updatedAt: string;
  lines: PurchaseInvoiceLineDto[];
}

export interface PurchaseInvoiceLineDto {
  id: number;
  productId: number;
  productName: string;
  barcode: string;
  quantity: number;
  unitCostUsd: number;
  lineTotalUsd: number;
  expiryDate?: string | null;
  notes?: string | null;
  sortOrder: number;
}

export interface CreatePurchaseInvoiceRequest {
  supplierId: number;
  externalInvoiceNumber?: string | null;
  notes?: string | null;
}

export interface UpdatePurchaseInvoiceRequest {
  supplierId: number;
  externalInvoiceNumber?: string | null;
  notes?: string | null;
}

export interface CreatePurchaseInvoiceLineRequest {
  productId: number;
  quantity: number;
  unitCostUsd: number;
  expiryDate?: string | null;
  notes?: string | null;
}

export interface UpdatePurchaseInvoiceLineRequest {
  quantity: number;
  unitCostUsd: number;
  expiryDate?: string | null;
  notes?: string | null;
}

export interface DeletePurchaseInvoiceResponse {
  success: boolean;
  action: string;
  message: string;
}

export interface DeletePurchaseInvoiceLineResponse {
  success: boolean;
  message: string;
}

export interface PurchaseProductLookupItem {
  productId: number;
  name: string;
  barcode: string;
  priceUsd: number;
  hasExpiry: boolean;
  baseUnit: string;
}

export interface PurchaseProductLookupResponse {
  items: PurchaseProductLookupItem[];
}
