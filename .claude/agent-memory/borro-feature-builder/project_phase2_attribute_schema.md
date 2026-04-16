---
name: Phase 2 ItemAttributes Schema
description: Flat ItemAttributes owned-type field list — names, CLR types, and which Category uses each field
type: project
---

`ItemAttributes` is a sealed class in `Borro.Domain.Entities` with the following nullable fields:

| Property      | CLR Type  | Used By                                      |
|---------------|-----------|----------------------------------------------|
| Mileage       | int?      | Vehicle                                      |
| Transmission  | string?   | Vehicle                                      |
| Bedrooms      | int?      | RealEstate                                   |
| Megapixels    | int?      | Electronics                                  |
| Brand         | string?   | Electronics, Tools, Sports, Other            |
| Condition     | string?   | Electronics, Tools, Sports, Other            |

All fields are nullable. Fields irrelevant to the current Category remain null and serialize as JSON null in PostgreSQL.

**Why:** Prevents JSONB key drift between backend EF config, frontend dynamic form fields, and search filter keys. Single source of truth is this file + `ItemAttributes.cs`.

**How to apply:** When adding new category-specific attributes in Sub-Phase C or later, update this file and `ItemAttributes.cs` together. Never add ad-hoc string keys to a dictionary — extend the typed properties instead.
