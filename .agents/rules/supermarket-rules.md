# Supermarket Project Rules

## 1. Architecture
- **Backend:** ASP.NET Core Web API
- **Frontend:** Angular
- **Database:** SQL Server

## 2. Design & Layout
- Layout MUST be **cashier-first**.
- Visual identity MUST follow the approved design system in `docs/design-system-reference.md` carefully.

## 3. Security & Permissions
- Permissions are strictly **screen-level only**. No complex granular element-based permission overrides.
- All sensitive flows must be **transaction-based**.

## 4. Business Logic Core Rules
- **Completed Invoices:** A completed invoice is NEVER opened directly for modifications.
- **Adjustments:** Invoice adjustments happen ONLY through dedicated adjustment requests.
- **Barcodes:** Strict rule - One barcode per product.
- **Cartons:** A carton is handled strictly as a `quantity` + `line total` adjustment. It is never treated as a distinctly separate product variant.
- **Data Types:** All money fields must use the `decimal` type.
