# Shared Utilities — Verify

## Workspace install

```bash
pnpm install
# Expect: no peer-dep warnings about @apptemplate/shared
```

## Type check the package

```bash
pnpm --filter @apptemplate/shared type-check
# Expect: no errors. The script is `tsc --noEmit`.
```

## Type check both consumer apps

```bash
pnpm --filter main type-check
pnpm --filter auth type-check
# Expect: no missing-symbol errors against @apptemplate/shared
```

## Subpath import smoke

In a scratch file inside an app:

```ts
import {
  i18n,
  setLocale,
  type SupportedLocale,
} from "@apptemplate/shared/i18n";
import { useApi, useAuth } from "@apptemplate/shared/composables";
import type { Role, User, ApiResponse } from "@apptemplate/shared/types";

// All of the above must resolve without any TypeScript "cannot find module" errors
```

`pnpm --filter <app> type-check` confirms.

## i18n smoke

1. In the main FE, open the language switcher (or call `setLocale('en')` from console).
2. Inspect `t('common.save')` — should return the English label.
3. Switch to another locale (after adding one per `customize.md` § 3) — same key returns the localized label.
4. Confirm the locale persists — typically via localStorage; the `setLocale` helper writes to it.

## API helper smoke

```ts
// In a Vue page:
import { useApi } from "@apptemplate/shared";
const api = useApi();

const { data } = await api.get<{ status: string }>("/health/ready");
console.log(data.status);
// Expect: "healthy"
```

## Form validation smoke

```ts
import { useFormValidation } from "@apptemplate/shared";
import { z } from "zod";

const schema = z.object({
  email: z.string().email(),
  age: z.number().min(18),
});

const { validate, errors } = useFormValidation(schema);

await validate({ email: "not-an-email", age: 5 });
console.log(errors.value);
// Expect: { email: ["Invalid email"], age: ["Number must be greater than or equal to 18"] }
```

## Type contract with backend

```bash
# Confirm the shared types match BE DTOs
diff <(cat src/frontend/packages/shared/src/types/role.ts | grep -E "^\s+\w+:" | tr -d ' ;') \
     <(cat src/backend/Libraries/Shared/Dto/RoleDto.cs | grep -E "public " | sed 's/public //;s/{ get; set; }//' | tr -d ' ;')
# Expect: shapes align (loose check; field names should match casing-insensitively)
```

(This is a shape sanity check, not a strict diff — generated TS clients would be the rigorous answer for a future iteration.)

## No styles in the package

```bash
# Confirm the package contains no CSS / SCSS files (visual styles belong in @apptemplate/ui)
find src/frontend/packages/shared/src -name "*.css" -o -name "*.scss" -o -name "*.sass"
# Expect: no matches
```

## No .vue files (yet)

```bash
# The split rule: .vue components belong in @apptemplate/ui, not here
find src/frontend/packages/shared/src -name "*.vue"
# Expect: no matches (the components/ folder has only an empty index.ts)
```

## No app-specific imports

```bash
# Confirm the package does not import from @/ aliases or from sibling apps
grep -rn "from \"@/" src/frontend/packages/shared/src
grep -rn "from \"\.\./\.\./main" src/frontend/packages/shared/src
# Expect: no matches
```

## Workspace resolution

```bash
# Confirm the package is reachable via workspace:* in both apps
pnpm --filter main why @apptemplate/shared
pnpm --filter auth why @apptemplate/shared
# Expect: linked from packages/shared via workspace:*
```
