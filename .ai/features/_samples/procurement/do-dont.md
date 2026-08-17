# Procurement — Do and Don't

> Procurement is a **reference sample**: a worked example to read and copy from, then delete. Nothing here is about extending it.

## DO ✅

1. **Read** the procurement code before you write your first feature. It answers "where does a service go", "how is a controller gated", "how does a Vue page talk to the API" faster than any prose can.
2. **Copy** patterns into your own entities — controller skeleton, service shape, Mapster mapping style, DTO flattening, sidebar nav wiring, the frontend service-then-page split.
3. **Compare** your own status enum against `EPurchaseOrderStatus` and check you mirror it to the frontend the same way. Enum drift between backend and frontend is the most common bug in this kind of app.
4. **Notice the `// === SAMPLE: procurement ... ===` fences** in shared files like `Program.cs`, `MainDbContext.cs`, and `AccessFunctionCatalog.cs`. They exist so a removable feature stays removable — use the same trick if you add another optional module.
5. **Remove** the sample once your real entities exist, following [`remove.md`](./remove.md). Dead demo code is confusing to your teammates and to the person marking your project.

## DON'T ❌

1. **Don't** extend procurement with your project's rules. If your project genuinely involves purchase orders, copy the code into your own namespace, rename it, and delete the original — see [`customize.md`](./customize.md).
2. **Don't** treat the sample as production code. The seed data is entirely fictional ("Acme Technology Supplies", "Alice Tan") and the approval chain is hardcoded for the demo rather than modelled properly.
3. **Don't** delete only some procurement files. Partial removal breaks the build — the entities, DTOs, services, controllers, mappings, seeds, access functions, routes, and nav entries come out together.
4. **Don't** ship the sample alongside your real feature. Two parallel domains in one app makes it genuinely unclear which one is the project.
5. **Don't** reuse the `Procurement*` access-function namespace for your own codes. Use your own (`Api.YourFeatureRead`), so removing the sample never touches your permissions.
6. **Don't** improve the sample in your project and expect the change to survive. Fixes to the reference sample belong upstream in the template repo; anything you change locally is thrown away when you delete it.
