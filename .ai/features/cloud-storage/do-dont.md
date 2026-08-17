# Cloud Storage - Do and Don't

## DO

1. DO validate file size, extension, and MIME type before storage.
2. DO keep provider credentials in secrets or environment-specific configuration.
3. DO store only metadata in PostgreSQL and file bytes in the configured provider.
4. DO normalize storage keys so user-provided filenames cannot escape the intended folder or bucket prefix.
5. DO stream downloads instead of loading large files fully into memory when practical.

## DON'T

1. DON'T trust the browser-provided MIME type as the only validation signal.
2. DON'T expose absolute local filesystem paths in API responses.
3. DON'T write provider-specific code directly in controllers.
4. DON'T log access keys, signed URLs, or full object paths that reveal sensitive tenant data.
5. DON'T delete metadata before the provider delete has succeeded or been safely queued for retry.
