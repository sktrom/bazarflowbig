# Workflow: init-angular-screen

## Objective
Outline the non-code process for initializing a new screen in the Angular frontend.

## Phases
1. **Screen Definition:**
   - Define module and components required.
   - Set up route definitions.
2. **Layout & Visuals Check:**
   - Ensure screen honors **RTL direction**.
   - Read `docs/design-system.md` to adapt valid colors (Emerald, Amber, Rose, Navy) and shapes (`rounded-2xl`).
   - Validate if this screen is cashier-facing. If yes, prioritize wide hit-targets and large primary buttons.
3. **Data Binding Mockup:**
   - Define state and typing logic to expect from the backend.
4. **Permissions Configuration:**
   - Note screen-level permission IDs to protect route access.
