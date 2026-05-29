# BazarFlow Endpoint Authorization Matrix

This document provides a comprehensive audit of all API endpoints in BazarFlow (V2-05), their required protection levels, and identified gaps.

## Classification Categories

1. **Public Allowed:** Endpoints designed to be accessible without any session or authentication.
2. **Authenticated Only:** Endpoints that require a valid session (`[RequireActiveSession]`) but do not require a specific UI screen permission.
3. **Permission Protected:** Endpoints that require both a valid session and explicit permission to access a specific screen (`[RequireScreenPermission]`).

---

## 1. Public Allowed Endpoints

| Controller | Method | Route | Description | Risk Level |
| :--- | :--- | :--- | :--- | :--- |
| `SetupController` | `GET` | `/api/setup/status` | Checks if the system setup is completed. | Low |
| `SetupController` | `POST` | `/api/setup/complete` | Submits initial setup parameters. Safely rejects if setup already completed. | Low |
| `AuthController` | `POST` | `/api/auth/login` | Authenticates an employee and issues a session token. | Medium (Brute-force risk) |
| `SettingsController` | `GET` | `/api/settings/public` | Fetches non-sensitive public settings (e.g., store name, exchange rate) for frontend bootstrap. | Low |

---

## 2. Authenticated Only Endpoints

| Controller | Method | Route | Required Protection | Description | Risk Level |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `AuthController` | `POST` | `/api/auth/logout` | `[RequireActiveSession]` | Terminates the current session. | Low |
| `AuthController` | `GET` | `/api/auth/me` | `[RequireActiveSession]` | Retrieves current employee details. | Low |
| `AuthController` | `GET` | `/api/auth/permissions` | `[RequireActiveSession]` | Retrieves permissions for the current user. | Low |

---

## 3. Permission Protected Endpoints

| Controller | Protected Resource | Required ScreenKey | Status |
| :--- | :--- | :--- | :--- |
| `ActionCenterController` | Inventory Alerts / Actions | `Inventory` | Protected |
| `AdjustmentRequestsController` | Stock Adjustments | `Invoices` | Protected |
| `AuditLogsController` | Audit Trail | `Settings` | Protected |
| `BlackBoxController` | Black Box Logs | `BlackBox` | Protected |
| `CartController` | POS Cart Operations | `Sales` | Protected |
| `CartFinalizationController` | POS Checkout | `Sales` | Protected |
| `CashierProductsController` | POS Product Fetch | `Sales` | Protected |
| `CategoriesController` | Product Categories | `Settings` | Protected |
| `DevicesController` | POS Devices Management | `Settings` | Protected |
| `EmployeesController` | Employee Management | `Settings` | Protected |
| `ExportsController` | Products Export | `Products` | Protected |
| `ExportsController` | Invoices Export | `Invoices` | Protected |
| `ExportsController` | Offers Export | `Offers` | Protected |
| `ExportsController` | Inventory Export | `Inventory` | Protected |
| `ExportsController` | Reports Export | `Reports` | Protected |
| `InventoryController` | Inventory View/Search | `Inventory` | Protected |
| `InvoicesQueryController` | Invoices View/Search | `Invoices` | Protected |
| `OffersController` | Promotional Offers | `Offers` | Protected |
| `PrintsController` | Printing Services | `Reports` | Protected |
| `ProductBatchesController` | Batches & Expiry | `Products` | Protected |
| `ProductsController` | Products CRUD | `Products` | Protected |
| `PurchaseInvoicesController`| Purchases CRUD | `Purchases` | Protected |
| `ReportsController` | Reporting & Analytics | `Reports` | Protected |
| `SessionsController` | Session Management | `Settings` | Protected |
| `SuppliersController` | Suppliers CRUD | `Purchases` | Protected |
| `SystemMaintenanceController`| Backup Operations | `Settings` | Protected |

---

## 4. Authorization Gaps & Findings

Overall, the API endpoints are remarkably well-structured regarding authorization attributes (`[RequireActiveSession]` and `[RequireScreenPermission]`). There are no glaringly unprotected sensitive endpoints discovered during this audit. However, there are a few architectural gaps to be addressed in subsequent hardening phases:

1. **SystemMaintenanceController / Backup**
   - **Current State:** Protected by `[RequireScreenPermission("Settings")]`.
   - **Gap:** Backups are highly sensitive. Any employee with "Settings" access can trigger a backup or potentially exploit a path traversal vulnerability if the backend allows arbitrary paths (not evaluated here).
   - **Recommendation (V2-05B):** Ensure path traversal protections exist in `SystemMaintenanceController`. Consider a more restrictive permission (e.g., `Backup` or `System`) instead of generic `Settings`.

2. **AuditLogsController & DevicesController**
   - **Current State:** Protected by `[RequireScreenPermission("Settings")]`.
   - **Gap:** "Settings" is becoming an overloaded permission. An employee who needs to manage devices might not need to see the entire system Audit Log. 
   - **Recommendation (V2-05B):** Break down "Settings" into more granular permissions (e.g., `AuditLogs`, `Devices`, `Employees`).

3. **Public Setup / Settings Endpoints**
   - **Current State:** `/api/setup` is `[AllowAnonymous]` but checks `setup_completed`. `/api/settings/public` is unprotected.
   - **Gap:** While not a direct vulnerability, these could be subjected to DDoS/Throttling abuse.
   - **Recommendation (V2-05B):** Apply global rate limiting.

4. **Missing Endpoints in SettingsController?**
   - **Current State:** `SettingsController` only has `/api/settings/public`.
   - **Gap:** Where are the internal system settings (e.g., changing store name, receipt headers) managed? If they are managed in `Categories` or `Devices`, that's fine, but if there's a missing Settings update endpoint, it needs to be identified. (A quick review showed settings updates might be scattered or handled by individual controllers like `SystemMaintenance`).

5. **Rate Limiting on `/api/auth/login`**
   - **Current State:** Currently unprotected against brute force (returns 403 or 401 without delay).
   - **Gap:** Brute force risk on LAN.
   - **Recommendation (V2-05B):** Implement IP-based or Account-based rate limiting on login attempts.

---

### Conclusion of V2-05A Audit
No critical hotfixes (patches) were required immediately because all controllers use `[RequireScreenPermission]` and `[RequireActiveSession]` appropriately. The next steps involve refining permissions and adding defense-in-depth measures.
