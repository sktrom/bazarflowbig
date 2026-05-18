export interface InventoryListResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  items: InventoryListItemDto[];
}

export interface InventoryListItemDto {
  productId: number;
  productName: string;
  barcode: string;
  categoryId: number;
  categoryName: string;
  baseUnit: string;
  priceUsd: number;
  hasCarton: boolean;
  cartonQuantity?: number;
  cartonPriceUsd?: number;
  hasExpiry: boolean;
  isActive: boolean;
  totalQuantityAvailable: number;
  batchCount: number;
  nearestExpiryDate?: string;
  stockStatus: string;
  expiryStatus?: string;
}

export interface InventoryDetailsResponse {
  productId: number;
  productName: string;
  barcode: string;
  categoryId: number;
  categoryName: string;
  baseUnit: string;
  priceUsd: number;
  hasCarton: boolean;
  cartonQuantity?: number;
  cartonPriceUsd?: number;
  hasExpiry: boolean;
  isActive: boolean;
  totalQuantityAvailable: number;
  stockStatus: string;
  expiryStatus?: string;
  batches: InventoryBatchDto[];
}

export interface InventoryBatchDto {
  batchId: number;
  quantityReceived: number;
  quantityAvailable: number;
  entryDate?: string;
  expiryDate?: string;
  entryInvoiceNumber?: string;
  enteredByEmployeeId: number;
  daysUntilExpiry?: number;
  expiryStatus?: string;
}
