# Agent Memory Index

- [Phase 2 ItemAttributes Schema](project_phase2_attribute_schema.md) — Flat nullable fields on ItemAttributes owned type: which Category uses Mileage/Transmission/Bedrooms/Megapixels/Brand/Condition
- [Phase 2 JSONB Strategy](project_phase2_jsonb_strategy.md) — OwnsOne+ToJson mapping; GIN index with jsonb_path_ops; EF.Functions.JsonContains for search in Sub-Phase C
- [Phase 2 Auth Claims Shape](project_phase2_auth_claims_shape.md) — JWT sub claim = UserId as Guid string; Phase 1 must emit this; maps to ClaimTypes.NameIdentifier in ASP.NET Core
