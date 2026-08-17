# PDF Generation - Do and Don't

## DO

1. DO generate report HTML on the server from trusted data and encode user-sourced text.
2. DO reuse the shared report header/footer partial for report documents so every generated PDF looks like it came from the same application.
3. DO guard report endpoints with `AccessFunctionCodes.Api.ReportRead`.
4. DO expose an inline PDF endpoint separately from the attachment download endpoint.
5. DO keep report filters explicit in `ReportRequestDto` and document new filters in the report type definition.

## DON'T

1. DON'T render untrusted HTML directly into report output.
2. DON'T assume Playwright exists on every developer or server machine; document and configure the executable path.
3. DON'T return EF entities directly from report endpoints.
4. DON'T generate PDFs in controllers; keep conversion in `IPdfGenerationService`.
5. DON'T bypass access functions for report previews because previews expose the same data as downloads.
