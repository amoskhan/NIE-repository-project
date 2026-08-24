import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const [, , scriptName, ...scriptArguments] = process.argv;
if (!scriptName || !/^[A-Za-z0-9-]+$/.test(scriptName)) {
  console.error(
    "Usage: node build/Invoke-PlatformScript.mjs <script-name> [arguments]",
  );
  process.exit(2);
}

const buildDirectory = dirname(fileURLToPath(import.meta.url));
const windows = process.platform === "win32";
const scriptPath = join(
  buildDirectory,
  `${scriptName}.${windows ? "ps1" : "sh"}`,
);
if (!existsSync(scriptPath)) {
  console.error(`Platform script was not found: ${scriptPath}`);
  process.exit(2);
}

const command = windows ? "pwsh" : "bash";
const commandArguments = windows
  ? ["-NoProfile", "-File", scriptPath, ...scriptArguments]
  : [scriptPath, ...scriptArguments];
const result = spawnSync(command, commandArguments, { stdio: "inherit" });
if (result.error) {
  console.error(`Could not start ${command}: ${result.error.message}`);
  process.exit(1);
}
if (result.signal) {
  console.error(`${command} terminated after receiving ${result.signal}.`);
  process.exit(1);
}
process.exit(result.status ?? 1);
