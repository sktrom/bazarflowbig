export interface EmployeeListItem {
  id: number;
  fullName: string;
  username: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
}

export interface EmployeeListResponse {
  items: EmployeeListItem[];
}

export interface PermissionItem {
  screenId: number;
  screenKey: string;
  screenName: string;
  canAccess: boolean;
}

export interface EmployeeDetailResponse {
  id: number;
  fullName: string;
  username: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  permissions: PermissionItem[];
}

export interface PermissionEntry {
  screenKey: string;
  canAccess: boolean;
}

export interface CreateEmployeeRequest {
  fullName: string;
  username: string;
  phone?: string;
  password: string;
  permissions?: PermissionEntry[];
}

export interface UpdateEmployeeRequest {
  fullName: string;
  phone?: string;
  isActive: boolean;
  permissions?: PermissionEntry[];
}

export interface DeleteEmployeeResponse {
  success: boolean;
  action: string;
  message: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface ResetPasswordResponse {
  success: boolean;
  message: string;
}

// --- Categories ---
export interface CategoryItem {
  id: number;
  name: string;
  isActive: boolean;
}

export interface CategoryListResponse {
  items: CategoryItem[];
}

export interface CreateCategoryRequest {
  name: string;
}

export interface UpdateCategoryRequest {
  name: string;
  isActive: boolean;
}

export interface DeleteCategoryResponse {
  success: boolean;
  action: string;
  message: string;
}

// --- Settings ---
export interface PublicSettingsResponse {
  storeName: string;
  exchangeRate: number;
}

export interface CreateBackupResponse {
  success: boolean;
  fileName: string;
  createdAt: string;
  sizeBytes: number;
  message: string;
  backupDirectory: string;
}

// --- Audit Logs ---
export interface AuditLogListItem {
  id: number;
  employeeId?: number;
  employeeName?: string;
  sessionId?: number;
  action: string;
  entityType: string;
  entityId?: string;
  entityDisplayName?: string;
  createdAt: string;
  hasBefore: boolean;
  hasAfter: boolean;
  hasMetadata: boolean;
}

export interface AuditLogListResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AuditLogListItem[];
}

export interface AuditLogDetailResponse extends AuditLogListItem {
  beforeJson?: string;
  afterJson?: string;
  metadataJson?: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface AuditLogStatusResponse {
  totalCount: number;
  oldestCreatedAt?: string;
  newestCreatedAt?: string;
  approximateLargeJsonCount: number;
  recommendedRetentionDays: number;
  cleanupEnabled: boolean;
}

// --- POS Devices ---
export interface PosDeviceListItem {
  id: number;
  deviceCode: string;
  deviceName: string;
  isActive: boolean;
  notes?: string;
  lastLoginAt?: string;
  createdAt: string;
}

export interface PosDeviceDetailsResponse {
  id: number;
  deviceCode: string;
  deviceName: string;
  isActive: boolean;
  notes?: string;
  lastLoginAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePosDeviceRequest {
  deviceCode: string;
  deviceName: string;
  notes?: string;
}

export interface UpdatePosDeviceRequest {
  deviceName: string;
  notes?: string;
}

export interface DeletePosDeviceResponse {
  success: boolean;
  message: string;
}

export interface EnableDisablePosDeviceResponse {
  success: boolean;
  message: string;
}

export interface ActiveSessionResponse {
  sessionId: number;
  employeeId: number;
  employeeName: string;
  username: string;
  deviceId: number;
  deviceCode: string;
  deviceName: string;
  startedAt: string;
  lastSeenAt?: string | null;
  expiresAt?: string | null;
}
