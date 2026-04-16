---
name: Phase 2 JSONB Strategy
description: EF Core owned type + ToJson strategy for ItemAttributes JSONB column, GIN index decision, and search approach
type: project
---

Chose a **flat owned type** (`ItemAttributes` sealed class) mapped via `OwnsOne(i => i.Attributes, a => a.ToJson())` in `BorroDbContext`. This replaces the prior `Dictionary<string,object>` with manual `HasConversion`.

Key decisions:
- Mapping: `OwnsOne(...).ToJson()` (EF Core 8+ JSON column feature, NOT `HasConversion`).
- Search: will use `EF.Functions.JsonContains` / JSON path queries in Sub-Phase C handlers for attribute-key filters (e.g., Mileage range, Transmission).
- GIN index: `CREATE INDEX IX_Items_Attributes_Gin ON "Items" USING gin ("Attributes" jsonb_path_ops)` added via raw SQL in migration `20260416054602_AddItemFullSchema`. Operator class `jsonb_path_ops` supports `@>` (containment) queries used by `JsonContains`.
- `Down()` drops with `DROP INDEX IF EXISTS IX_Items_Attributes_Gin`.

**Why:** `jsonb_path_ops` GIN index enables efficient `@>` containment checks needed for multi-attribute filtering in the search handler. Without it, full-table scans on the JSONB column would degrade at scale.

**How to apply:** In Sub-Phase C search query, use `EF.Functions.JsonContains(i.Attributes, ...)` or Npgsql JSONB operators. Do not use `LIKE` or string deserialization for attribute filters.
