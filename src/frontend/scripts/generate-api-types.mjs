#!/usr/bin/env node
/**
 * OpenAPI TypeScript Client Generator
 *
 * Generates TypeScript types from the running API's built-in OpenAPI spec.
 * Usage: pnpm run generate:api
 *
 * Prerequisites: API must be running on localhost:5002
 */

import { execSync } from "node:child_process";
import { mkdirSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const workspaceRoot = resolve(__dirname, "..");
const outputDir = resolve(workspaceRoot, "packages/platform/src/api-types");

if (!existsSync(outputDir)) {
  mkdirSync(outputDir, { recursive: true });
}

const apiUrl = process.env.API_URL || "http://localhost:5002";

const openApiSpecUrl = `${apiUrl}/openapi/v1.json`;

console.log(`Fetching OpenAPI spec from ${openApiSpecUrl} ...`);

try {
  execSync(
    `npx openapi-typescript ${openApiSpecUrl} -o ${resolve(outputDir, "api.d.ts")}`,
    { stdio: "inherit", cwd: workspaceRoot },
  );
  console.log(`\n✓ Types generated at packages/platform/src/api-types/api.d.ts`);
} catch {
  console.error(
    "\n✗ Failed to generate types. Is the API running on",
    apiUrl,
    "?",
  );
  process.exit(1);
}
