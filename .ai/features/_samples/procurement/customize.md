# Procurement — Customize

This file is for the **rare** case where your project genuinely is a purchasing / requisition system and
you want to keep the sample as your starting point rather than delete it.

If that is not you — and for most projects it is not — the answer is always: **don't customize it,
[remove it](./remove.md), and copy the patterns into your own feature.**

## Adopt procurement as your real feature

1. **Rename it into your own vocabulary.** Keeping the sample's names means every reviewer has to work
   out which parts are yours. Apply these mappings per-file, reviewing each hit:
   - `Procurement` → `<YourFeature>`
   - `PurchaseOrder` → `<YourEntity>`
   - `Vendor` → `<YourMaster>`
   - `CatalogItem` → `<YourCatalog>`
2. **Move the files out of `Samples/`.** Once the code is yours it is not a sample; leaving it under
   `Samples/Procurement/` invites someone (or a future cleanup task) to delete it.
3. **Remove the `// === SAMPLE: procurement ... ===` fences.** They mark removable wiring. Code you
   intend to keep should not be fenced as removable.
4. **Rebuild the migration history.** Squash the sample's migrations and generate a clean initial
   migration for your renamed schema rather than carrying the demo's history forward.
5. **Replace the fictional seed data.** `DatabaseSeeder`'s vendors, people, catalog items, and orders
   are invented for the demo. Substitute your own seed, or delete the seeding entirely if your data
   comes from elsewhere. Do not ship "Acme Technology Supplies" in a real system.
6. **Model your actual workflow states.** `EPurchaseOrderStatus` is the demo's state machine. Replace
   it with yours, and mirror the enum to the frontend — drift between the two ends is the most common
   bug in this shape of app.
7. **Replace the hardcoded approval chain.** `PurchaseOrderController.Submit` seeds fixed stages
   (`"Manager"`, `"Finance"`, `"Procurement"`) as string literals. Promote them to an enum, or drive
   them from configuration or a table, before you rely on them.
8. **Refresh the role bundles** in `AccessFunctionCatalog` so the seeded roles match the actual roles
   in your project, not the demo's.
9. **Rename the feedback namespace** from `procurement.` to your own slug — see
   [`.ai/features/feedback-widget/customize.md`](../../feedback-widget/customize.md) §1.
