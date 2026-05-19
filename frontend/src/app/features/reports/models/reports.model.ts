export interface ReportList<T> {
  items: T[];
}

// --- Sales ---
export interface SalesInvoiceReportDto {
  invoiceId: number;
  invoiceNumber: string;
  createdAt: string;
  status: string;
  totalUsd: number;
  totalSyp?: number;
  employeeName: string;
}

export interface SalesItemReportDto {
  productId: number;
  productName: string;
  totalQuantitySold: number;
  totalRevenueUsd: number;
}

export interface SalesChartDto {
  dateLabel: string;
  revenueUsd: number;
}

// --- Products ---
export interface ProductSummaryReportDto {
  productId: number;
  productName: string;
  categoryName: string;
  totalStockQuantity: number;
  totalStockValueUsd: number;
  isActive: boolean;
}

export interface ProductMovementReportDto {
  productId: number;
  productName: string;
  movementDate: string;
  movementType: string;
  quantity: number;
  referenceNumber: string;
}

export interface ProductChartDto {
  productName: string;
  totalSalesRevenueUsd: number;
}

// --- Employees ---
export interface EmployeeSummaryReportDto {
  employeeId: number;
  employeeName: string;
  totalInvoicesHandled: number;
  totalSalesRevenueUsd: number;
}

export interface EmployeeActivityReportDto {
  employeeId: number;
  employeeName: string;
  activityDate: string;
  activityType: string;
  details: string;
}

export interface EmployeeChartDto {
  employeeName: string;
  totalSalesRevenueUsd: number;
}

// --- Inventory ---
export interface InventorySummaryReportDto {
  productId: number;
  productName: string;
  categoryName: string;
  totalQuantityAvailable: number;
  totalStockValueUsd: number;
  stockStatus: string;
}

export interface InventoryBatchReportDto {
  batchId: number;
  productName: string;
  quantityReceived: number;
  quantityAvailable: number;
  entryDate?: string;
  entryInvoiceNumber: string;
}

export interface InventoryChartDto {
  categoryName: string;
  totalStockValueUsd: number;
}

export interface InventoryValuationDto {
  productId: number;
  productName: string;
  categoryName: string;
  totalQuantityAvailable: number;
  knownCostQuantity: number;
  missingCostQuantity: number;
  knownStockValueUsd: number;
  hasMissingCost: boolean;
  isValuationComplete: boolean;
}

// --- Profit ---
export interface ProfitSalesInvoiceDto {
  invoiceId: number;
  invoiceNumber: string;
  createdAt: string;
  revenueUsd: number;
  knownCostUsd: number;
  profitUsd: number;
  marginPercent?: number | null;
  hasMissingCost: boolean;
  isProfitComplete: boolean;
  missingCostQuantity: number;
}

export interface ProfitProductDto {
  productId: number;
  productName: string;
  quantitySold: number;
  revenueUsd: number;
  knownCostUsd: number;
  profitUsd: number;
  marginPercent?: number | null;
  hasMissingCost: boolean;
  isProfitComplete: boolean;
  missingCostQuantity: number;
}

// --- Expiry ---
export interface ExpirySummaryReportDto {
  productId: number;
  productName: string;
  expiredBatchesCount: number;
  expiringSoonBatchesCount: number;
  totalExpiredValueUsd: number;
}

export interface ExpiryBatchReportDto {
  batchId: number;
  productName: string;
  quantityAvailable: number;
  expiryDate?: string;
  expiryStatus: string;
  daysUntilExpiry?: number;
}

export interface ExpiryChartDto {
  expiryStatus: string;
  batchCount: number;
}
