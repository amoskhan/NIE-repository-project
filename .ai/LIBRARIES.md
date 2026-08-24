# Library and platform minimums

Rules version: 2026.08.07.1

These are minimum supported stable versions. The NIE Template pins exact versions it has tested. A derived application may use a newer stable compatible version after restore, lint, type-check, build, unit, integration, browser, security, and deployment checks pass. Major upgrades require an ADR and breaking-change review. Preview, beta, RC, nightly, floating latest, and unbounded versions require an owned, approved, expiring exception.

## Package selection order

The baseline below is authoritative for packages already selected by the template. For a new dependency, AI agents must use this order and record why the selected layer is the first one that satisfies the requirement:

| Rank | Choice | Required interpretation |
| --- | --- | --- |
| 1 | Platform capability already in the runtime/framework | Prefer the .NET BCL, ASP.NET Core shared framework, browser platform, or language capability and avoid an unnecessary package. |
| 2 | Official Microsoft package for a .NET platform concern | Prefer supported `System.*`, `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, Entity Framework Core, and other official Microsoft packages when they are the correct platform abstraction. This is not permission to couple business logic to Azure or another hosted service. |
| 3 | Technology owner's official open-source package | Prefer packages maintained by the official project, foundation, or recognized owner, such as Npgsql for PostgreSQL and packages in the official Vue, Vite, OpenTelemetry, or framework organization. |
| 4 | Mature leading open-source package | Select an actively maintained, secure, documented, well-tested, widely adopted project with a compatible OSI-approved license and a sustainable contributor base. |
| 5 | Proprietary or provider-exclusive dependency | Use only through the approved-exception process and an application-owned adapter with a credible exit plan. |

“Most popular” means current, independently verifiable adoption within the relevant ecosystem, not simply the highest star or download count. Agents must compare the serious candidates using functional fit, security history and response, maintainership, license, stable release support, documentation, ecosystem adoption, performance, testability, interoperability, transitive dependency risk, operational burden, and replacement cost. Prefer the simplest package that meets the requirement and is already coherent with this stack; when the conformant candidates are otherwise equivalent, choose the most widely adopted ecosystem-standard package.

For NuGet packages, verify the package ID and owner against official documentation and the source repository, declare tested versions centrally in `src/backend/Directory.Packages.props`, keep project references versionless, use approved package sources and source mapping where multiple feeds exist, commit application lock files where configured, restore in locked mode in CI, and run vulnerability auditing. Apply equivalent provenance, lockfile, and audit controls to npm packages, container images, and CI actions.

Provider-neutrality is an architecture boundary, not a ban on provider SDKs. A provider SDK may exist in an infrastructure adapter or composition root, but its types, configuration model, proprietary identifiers, and error contracts must not leak into domain/application contracts. Prefer open protocols and portable formats, and retain contract tests that can be run against an alternative provider.

| ID | Technology | Package/runtime | Minimum | Ecosystem | Detection source |
| --- | --- | --- | --- | --- | --- |
| dotnet-sdk | .NET SDK | dotnet | 10.0.102 | runtime | global.json |
| dotnet-runtime | .NET / ASP.NET Core | net | 10.0.0 | runtime | src/backend/Directory.Build.props |
| code-analysis | Microsoft.CodeAnalysis.NetAnalyzers | Microsoft.CodeAnalysis.NetAnalyzers | 10.0.201 | nuget | Microsoft.CodeAnalysis.NetAnalyzers |
| fluentvalidation | FluentValidation | FluentValidation | 12.1.1 | nuget | FluentValidation |
| fluentvalidation-di | FluentValidation dependency injection extensions | FluentValidation.DependencyInjectionExtensions | 12.1.1 | nuget | FluentValidation.DependencyInjectionExtensions |
| xunit-v3 | xUnit.net v3 with Microsoft Testing Platform v2 | xunit.v3.mtp-v2 | 3.2.2 | nuget | xunit.v3.mtp-v2 |
| microsoft-testing-platform | Microsoft Testing Platform | Microsoft.Testing.Platform | 2.3.0 | nuget | transitive test dependency |
| microsoft-testing-coverage | Microsoft Testing Platform code coverage | Microsoft.Testing.Extensions.CodeCoverage | 18.9.0 | nuget | src/backend/Tests |
| nsubstitute | NSubstitute | NSubstitute | 6.0.0 | nuget | NSubstitute |
| coverlet-console | Coverlet console | coverlet.console | 6.0.4 | dotnet-tool | .config/dotnet-tools.json |
| reportgenerator | ReportGenerator | dotnet-reportgenerator-globaltool | 5.5.11 | dotnet-tool | .config/dotnet-tools.json |
| stryker-dotnet | Stryker.NET | dotnet-stryker | 4.16.0 | dotnet-tool | .config/dotnet-tools.json |
| api-versioning | ASP.NET API Versioning | Asp.Versioning.Mvc | 8.1.1 | nuget | Asp.Versioning.Mvc |
| aspnet-openapi | Microsoft.AspNetCore.OpenApi | Microsoft.AspNetCore.OpenApi | 10.0.10 | nuget | Microsoft.AspNetCore.OpenApi |
| openapi-model | Microsoft.OpenApi | Microsoft.OpenApi | 2.7.5 | nuget | Microsoft.OpenApi |
| aspnet-oidc | ASP.NET Core OpenID Connect | Microsoft.AspNetCore.Authentication.OpenIdConnect | 10.0.5 | nuget | Microsoft.AspNetCore.Authentication.OpenIdConnect |
| aspnet-wsfed | ASP.NET Core WS-Federation | Microsoft.AspNetCore.Authentication.WsFederation | 10.0.5 | nuget | Microsoft.AspNetCore.Authentication.WsFederation |
| ef-core | Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget | Microsoft.EntityFrameworkCore |
| npgsql-ef | Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget | Npgsql.EntityFrameworkCore.PostgreSQL |
| rabbitmq | RabbitMQ server | rabbitmq | 4.3.4 | service | deploy/local/service-integration.compose.yml |
| rabbitmq-dotnet | RabbitMQ official .NET client | RabbitMQ.Client | 7.2.2 | nuget | RabbitMQ.Client |
| grpc-aspnet | ASP.NET Core gRPC server | Grpc.AspNetCore | 2.80.0 | nuget | Grpc.AspNetCore |
| grpc-health | ASP.NET Core gRPC health checks | Grpc.AspNetCore.HealthChecks | 2.80.0 | nuget | Grpc.AspNetCore.HealthChecks |
| grpc-client | gRPC .NET client | Grpc.Net.Client | 2.80.0 | nuget | Grpc.Net.Client |
| grpc-client-factory | gRPC .NET client factory | Grpc.Net.ClientFactory | 2.80.0 | nuget | Grpc.Net.ClientFactory |
| grpc-core-api | gRPC core API | Grpc.Core.Api | 2.80.0 | nuget | Grpc.Core.Api |
| grpc-tools | gRPC protobuf tooling | Grpc.Tools | 2.83.0 | nuget | Grpc.Tools |
| protobuf | Protocol Buffers runtime | Google.Protobuf | 3.35.1 | nuget | Google.Protobuf |
| postgresql | PostgreSQL | postgres | 17.0.0 | service | .devcontainer/docker-compose.yml, build/docker-compose.yml |
| redis-cache | Microsoft Redis distributed cache | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.5 | nuget | Microsoft.Extensions.Caching.StackExchangeRedis |
| valkey | Valkey | valkey | 8.0.0 | service | .devcontainer/docker-compose.yml, build/docker-compose.yml |
| mapster | Mapster | Mapster | 10.0.4 | nuget | Mapster |
| tickerq | TickerQ | TickerQ | 10.2.5 | nuget | TickerQ |
| mailkit | MailKit | MailKit | 4.16.0 | nuget | MailKit |
| aws-s3 | AWS SDK for S3 | AWSSDK.S3 | 4.0.17 | nuget | AWSSDK.S3 |
| localstack | LocalStack | localstack | 4.11.1 | service | .devcontainer/docker-compose.yml |
| playwright-dotnet | Microsoft.Playwright | Microsoft.Playwright | 1.58.0 | nuget | Microsoft.Playwright |
| jose-jwt | jose-jwt | jose-jwt | 5.2.0 | nuget | jose-jwt |
| identitymodel-tokens | Microsoft IdentityModel Tokens | Microsoft.IdentityModel.Tokens | 8.17.0 | nuget | Microsoft.IdentityModel.Tokens |
| sentry-dotnet | Sentry for ASP.NET Core | Sentry.AspNetCore | 6.4.1 | nuget | Sentry.AspNetCore |
| otel-dotnet | OpenTelemetry .NET hosting | OpenTelemetry.Extensions.Hosting | 1.15.3 | nuget | OpenTelemetry.Extensions.Hosting |
| health-npgsql | ASP.NET Core PostgreSQL Health Checks | AspNetCore.HealthChecks.NpgSql | 9.0.0 | nuget | AspNetCore.HealthChecks.NpgSql |
| health-redis | ASP.NET Core Redis Health Checks | AspNetCore.HealthChecks.Redis | 9.0.0 | nuget | AspNetCore.HealthChecks.Redis |
| azure-openai | Azure.AI.OpenAI | Azure.AI.OpenAI | 2.1.0 | nuget | Azure.AI.OpenAI |
| agents-ai | Microsoft.Agents.AI | Microsoft.Agents.AI | 1.3.0 | nuget | Microsoft.Agents.AI |
| extensions-ai | Microsoft.Extensions.AI | Microsoft.Extensions.AI | 10.5.0 | nuget | Microsoft.Extensions.AI |
| pgvector | Pgvector | Pgvector | 0.3.2 | nuget | Pgvector |
| pgvector-ef | Pgvector.EntityFrameworkCore | Pgvector.EntityFrameworkCore | 0.3.0 | nuget | Pgvector.EntityFrameworkCore |
| node | Node.js | node | 24.19.0 | runtime | src/frontend/package.json |
| pnpm | pnpm | pnpm | 10.33.0 | runtime | src/frontend/package.json |
| vue | Vue | vue | 3.5.30 | npm | vue |
| vee-validate | VeeValidate | vee-validate | 4.15.1 | npm | vee-validate |
| vee-validate-zod | VeeValidate Zod integration | @vee-validate/zod | 4.15.1 | npm | @vee-validate/zod |
| vue-router | Vue Router | vue-router | 4.5.1 | npm | vue-router |
| vite | Vite | vite | 8.0.16 | npm | vite |
| vitest | Vitest | vitest | 4.1.10 | npm | vitest |
| vitest-coverage-v8 | Vitest V8 coverage provider | @vitest/coverage-v8 | 4.1.10 | npm | src/frontend/apps/main/package.json |
| vue-test-utils | Vue Test Utils | @vue/test-utils | 2.4.11 | npm | @vue/test-utils |
| jsdom | jsdom | jsdom | 30.0.1 | npm | jsdom |
| stryker-js | StrykerJS core, plugin API, and Vitest runner | @stryker-mutator/core | 9.6.1 | npm | src/frontend/package.json |
| typescript | TypeScript | typescript | 6.0.2 | npm | typescript |
| vue-tsc | vue-tsc | vue-tsc | 3.2.6 | npm | vue-tsc |
| eslint | ESLint | eslint | 10.8.0 | npm | eslint |
| typescript-eslint | typescript-eslint | typescript-eslint | 8.65.0 | npm | typescript-eslint |
| eslint-plugin-vue | eslint-plugin-vue | eslint-plugin-vue | 10.10.0 | npm | eslint-plugin-vue |
| tailwindcss | Tailwind CSS | tailwindcss | 4.2.2 | npm | tailwindcss |
| axios | Axios | axios | 1.18.0 | npm | axios |
| js-cookie | js-cookie | js-cookie | 3.0.7 | npm | js-cookie |
| postcss | PostCSS | postcss | 8.5.18 | npm | postcss |
| otel-js-core | OpenTelemetry JavaScript core | @opentelemetry/core | 2.8.0 | npm | @opentelemetry/core |
| otel-js-otlp | OpenTelemetry JavaScript OTLP HTTP exporter | @opentelemetry/exporter-trace-otlp-http | 0.219.0 | npm | @opentelemetry/exporter-trace-otlp-http |
| sentry-vue | Sentry for Vue | @sentry/vue | 9.47.1 | npm | @sentry/vue |
| vue-i18n | Vue I18n | vue-i18n | 11.1.3 | npm | vue-i18n |
| zod | Zod | zod | 3.25.67 | npm | zod |
| uuid-js | uuid | uuid | 14.0.1 | npm | uuid |
| vueuse | VueUse | @vueuse/core | 14.2.1 | npm | @vueuse/core |
| heroicons-vue | Heroicons for Vue | @heroicons/vue | 2.2.0 | npm | @heroicons/vue |
| class-variance-authority | Class Variance Authority | class-variance-authority | 0.7.1 | npm | class-variance-authority |
| clsx | clsx | clsx | 2.1.1 | npm | clsx |
| tailwind-merge | tailwind-merge | tailwind-merge | 3.5.0 | npm | tailwind-merge |
| signalr | SignalR JavaScript client | @microsoft/signalr | 10.0.0 | npm | @microsoft/signalr |
| onesignal-web | OneSignal Web SDK | OneSignalSDK.page.js | 16.0.0 | cdn | src/frontend/apps/main/src/services/oneSignalService.ts |
| helm | Helm | helm | 3.17.3 | tool | deploy/pipeline/buildspec.deploy.yml |

## Upgrade evidence

- Record old and new versions, release notes reviewed, compatibility impact, migrations, and rollback.
- Run the complete affected test pyramid and dependency/security scans.
- For a newly introduced or replaced dependency, record the evaluated candidates, publisher and repository verification, open-source license, maintenance/security health, adoption evidence, selected abstraction boundary, alternative provider, and exit/migration plan.
- Never remove a required library and replace it with bespoke code unless the applicable feature rule is changed in the canonical template through architecture and security review.
