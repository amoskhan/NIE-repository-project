export default {
  "src/frontend/**/*.{ts,vue}": () =>
    "pnpm --dir src/frontend exec eslint . --max-warnings 0 --suppressions-location eslint-suppressions.json",
  "src/backend/**/*.cs": () =>
    "dotnet format src/backend/Backend.sln --verify-no-changes --severity warn --no-restore",
};
