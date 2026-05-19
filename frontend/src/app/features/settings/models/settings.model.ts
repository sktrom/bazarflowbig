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
