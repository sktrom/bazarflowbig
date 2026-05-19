export interface ActionCenterSummaryDto {
  outOfStockCount: number;
  lowStockCount: number;
  expiredBatchesCount: number;
  expiringSoonBatchesCount: number;
  inactiveWithStockCount: number;
  offerCandidatesCount: number;
}

export interface TopUrgentActionDto {
  type: string;
  severity: string;
  productId: number;
  productName: string;
  barcode: string;
  message: string;
  recommendedAction: string;
}

export interface ProductActionItemDto {
  productId: number;
  productName: string;
  barcode: string;
  currentStock: number;
}

export interface BatchActionItemDto extends ProductActionItemDto {
  batchId: number;
  expiryDate?: string;
}

export interface ActionCenterResponseDto {
  summary: ActionCenterSummaryDto;
  topUrgentActions: TopUrgentActionDto[];
  outOfStock: ProductActionItemDto[];
  lowStock: ProductActionItemDto[];
  expiringSoon: BatchActionItemDto[];
  expired: BatchActionItemDto[];
  inactiveWithStock: ProductActionItemDto[];
  offerCandidates: ProductActionItemDto[];
}
