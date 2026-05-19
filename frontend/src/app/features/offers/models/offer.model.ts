export interface OfferListItem {
  id: number;
  productId: number;
  productName: string;
  discountType: string;
  discountValue: number;
  isActive: boolean;
}

export interface OfferListResponse {
  items: OfferListItem[];
}

export interface OfferDetailResponse {
  id: number;
  productId: number;
  productName: string;
  discountType: string;
  discountValue: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateOfferRequest {
  productId: number;
  discountType: string;
  discountValue: number;
}

export interface UpdateOfferRequest {
  productId: number;
  discountType: string;
  discountValue: number;
}

export interface CancelOfferResponse {
  success: boolean;
  message: string;
}

export interface DeleteOfferResponse {
  success: boolean;
  message: string;
}

export interface OfferProductLookupItem {
  productId: number;
  name: string;
  barcode: string;
  priceUsd: number;
}

export interface OfferProductLookupResponse {
  items: OfferProductLookupItem[];
}
