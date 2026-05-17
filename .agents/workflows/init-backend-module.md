# Workflow: init-backend-module

## Objective
Outline the non-code process for initializing a new backend module in the ASP.NET Core Web API.

## Phases
1. **Module Definition:**
   - Define module domain logic limitations and boundaries.
2. **Data Structure Design:**
   - Map out entities (ensure money is `decimal`).
   - Draft database relationships targeted for SQL Server.
3. **API Contracts Formulation:**
   - Define expected inputs/outputs, strictly respecting rules (e.g. no direct invoice mutation endpoints, strictly adjustment request endpoints).
4. **Transaction Planning:**
   - Ensure boundary operations explicitly outline EF Core transaction bounds.
