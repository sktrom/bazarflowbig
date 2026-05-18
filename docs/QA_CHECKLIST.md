# QA Checklist

Before any major release or deployment, ensure all the following flows and features are verified manually:

- [ ] **Auth / Session / Navigation:** Verify login, logout, and token management. Ensure route guards are protecting restricted pages.
- [ ] **Cashier sale flow:** Verify processing a new sale from adding products to checking out.
- [ ] **Complete invoice + inventory deduction:** Ensure that a completed invoice correctly deducts the sold quantities from the inventory.
- [ ] **Suspended invoice:** Verify that an invoice can be suspended without deducting inventory.
- [ ] **Cancel suspended invoice + release inventory:** Ensure cancelling an invoice works correctly and any related resources are freed.
- [ ] **Products CRUD:** Verify Create, Read, Update, and Delete operations for products.
- [ ] **Product batches:** Verify batch tracking, expiry dates, and correct quantities.
- [ ] **Inventory list/details:** Verify that inventory accurately reflects current stock status and details.
- [ ] **Offers create/cancel/delete-used behavior:** Verify adding offers, cancelling active ones, and preventing the deletion of used offers.
- [ ] **Invoices list/details/adjustments:** Verify invoice history, details view, and processing of adjustment requests.
- [ ] **Reports tabs/charts/filters:** Verify rendering of reports, filtering by date and status, and correct chart drawing.
- [ ] **Settings employees/categories/store read-only:** Verify viewing settings and ensuring store variables remain read-only where appropriate.
- [ ] **Exports:** 
  - [ ] Products Export
  - [ ] Invoices Export
  - [ ] Offers Export
  - [ ] Inventory Export
