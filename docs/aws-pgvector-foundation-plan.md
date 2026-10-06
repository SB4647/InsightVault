# AWS pgvector Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a local PostgreSQL + pgvector path that performs permission-filtered vector retrieval in the database while retaining the current SQL Server development path and adding validation-only AWS Terraform foundations.

**Architecture:** The Application layer will request ranked, authorised chunk matches through an interface and will continue to own query validation and embedding orchestration. Infrastructure will choose the configured database provider, store vectors in PostgreSQL's `vector` type for the PostgreSQL path, and retain the existing SQL Server repository fallback. PostgreSQL integration tests will use disposable Docker containers; normal tests and Terraform validation will not authenticate to or create AWS resources.

**Tech Stack:** .NET 10, EF Core 10, PostgreSQL, pgvector, Npgsql EF Core provider, xUnit, Testcontainers for .NET, Docker Compose, Terraform, AWS provider (validation only).

**Spec:** `docs/production-aws-design.md`

## Global Constraints

- Domain must not reference Application or Infrastructure; Application must not reference Infrastructure.
- Controllers remain thin; business workflows remain asynchronous and dependency-injected.
- Existing SQL Server configuration and migrations must continue to work.
- The PostgreSQL local path must start through Docker and must not require AWS credentials, static keys, or paid model calls.
- PostgreSQL vectors use pgvector; access filtering happens inside the database query before a result is returned.
- Terraform lives under `infra/aws`, passes `fmt` and `validate`, and contains no default `apply` path.
- AWS applies and live smoke tests require a separate explicit approval and use the developer's named IAM Identity Center profile, never static keys.

## Review Focus

- A viewer must retrieve only a document explicitly shared with them; Task 3 adds a PostgreSQL integration test for owner, shared, and unrelated records.
- A document without an embedding must not become a search result; Task 3 adds a PostgreSQL integration test for this filter.
- A query vector with a different dimension must fail clearly instead of returning misleading rankings; Task 3 adds an integration test for the provider error mapping.
- SQL Server remains usable when PostgreSQL is not selected; Task 2 adds a DI configuration test for the default SQL Server selection.
- The local database is disposable and must not be mistaken for a cloud environment; Task 1 documents the Docker commands and explicit PostgreSQL profile.

---

## File Structure

- `docker-compose.yml` — retain SQL Server and add an opt-in PostgreSQL + pgvector service and named volume.
- `docker-compose.override.example.yml` — show local provider selection without storing secrets.
- `src/InsightVault.Infrastructure/DependencyInjection.cs` — select SQL Server or PostgreSQL from a validated configuration value and register provider-specific persistence services.
- `src/InsightVault.Infrastructure/Persistence/*DbContext.cs` — share the existing model while keeping SQL Server and PostgreSQL migrations associated with different context types.
- `src/InsightVault.Infrastructure/Persistence/PostgresMigrations/*` — reproducible PostgreSQL schema, including `CREATE EXTENSION vector` and the vector column/index.
- `src/InsightVault.Application/Interfaces/IVectorSearchRepository.cs` — provider-neutral request/result contract for ranked authorised chunk search.
- `src/InsightVault.Application/Features/Search/SemanticSearchService.cs` — embed the query, delegate database search, and keep validation at the Application boundary.
- `src/InsightVault.Infrastructure/Persistence/Repositories/*` — SQL Server fallback and PostgreSQL implementation of the vector-search interface.
- `tests/InsightVault.Tests/Infrastructure/PostgresVectorSearchTests.cs` — xUnit/Testcontainers proof of persistence, vector ranking, filtering, and error behaviour.
- `infra/aws/{versions.tf,providers.tf,variables.tf,main.tf,outputs.tf,README.md}` — validation-only provider/tag foundation; no resource blocks in this milestone.
- `README.md` and `docs/deployment.md` — local PostgreSQL instructions, test prerequisites, and the no-apply rule.

### Task 1: Local pgvector developer environment

**Files:**
- Modify: `docker-compose.yml`
- Modify: `docker-compose.override.example.yml`
- Modify: `README.md`
- Modify: `docs/deployment.md`

**Interfaces:**
- Consumes: existing Docker API and SQL Server services.
- Produces: a `postgres` Compose profile exposing PostgreSQL to the host and a documented `Database:Provider=Postgres` local setting.

- [ ] **Step 1: Add the failing configuration documentation check**

Create a short xUnit configuration test that loads PostgreSQL local settings and asserts the provider value is `Postgres` and its connection string targets the Compose service. This test must not open a network connection.

- [ ] **Step 2: Run the configuration test to verify it fails**

Run: `dotnet test tests/InsightVault.Tests --filter FullyQualifiedName~DatabaseConfigurationTests`

Expected: FAIL because the PostgreSQL settings fixture and documented provider configuration do not exist.

- [ ] **Step 3: Add the opt-in PostgreSQL service and documentation**

Add a `postgres` profile using a pinned `pgvector/pgvector` image, a named data volume, a health check, and host port `5433`. Preserve the existing SQL Server service and make PostgreSQL opt-in so current `docker compose up --build` behaviour remains SQL Server-based. Document `docker compose --profile postgres up -d postgres`, how to set `Database__Provider=Postgres`, and the data-reset command; mark all data disposable.

- [ ] **Step 4: Run the configuration test and Compose rendering check**

Run: `dotnet test tests/InsightVault.Tests --filter FullyQualifiedName~DatabaseConfigurationTests`

Expected: PASS.

Run: `docker compose --profile postgres config`

Expected: exits successfully and lists both the existing services and the `postgres` service without secret values.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml docker-compose.override.example.yml README.md docs/deployment.md tests/InsightVault.Tests/Infrastructure/DatabaseConfigurationTests.cs
git commit -m "feat: add local pgvector compose profile"
```

### Task 2: Provider-aware EF Core persistence

**Files:**
- Modify: `src/InsightVault.Infrastructure/InsightVault.Infrastructure.csproj`
- Modify: `src/InsightVault.Infrastructure/DependencyInjection.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/PostgresApplicationDbContext.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/PostgresMigrations/*`
- Modify: `src/InsightVault.Infrastructure/Persistence/Configurations/EmbeddingConfiguration.cs`
- Modify: `src/InsightVault.Api/appsettings.json`
- Modify: `src/InsightVault.Api/appsettings.Development.json`
- Test: `tests/InsightVault.Tests/Infrastructure/DatabaseConfigurationTests.cs`

**Interfaces:**
- Consumes: `Database:Provider`, `ConnectionStrings:DefaultConnection`, the existing `ApplicationDbContext`, and EF Core 10.
- Produces: `PostgresApplicationDbContext` with `vector` extension/schema support, and an `InsightVaultDbContext` service registration that resolves to the selected provider.

- [ ] **Step 1: Extend the failing configuration test**

Assert that `Database:Provider=SqlServer` registers the existing SQL Server context, `Postgres` registers `PostgresApplicationDbContext`, and any other value throws an `InvalidOperationException` that names `Database:Provider`.

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test tests/InsightVault.Tests --filter FullyQualifiedName~DatabaseConfigurationTests`

Expected: FAIL because the provider selection and PostgreSQL context are absent.

- [ ] **Step 3: Implement provider selection and PostgreSQL schema support**

Add the EF Core PostgreSQL and pgvector provider packages at versions compatible with EF Core 10. Extract the shared entity model into a common persistence context base; retain `ApplicationDbContext` as the SQL Server context so current SQL Server migrations remain valid. Add `PostgresApplicationDbContext` with a separate migrations history and configure pgvector through the provider's `UseVector`/model extension mechanism. Map the embedding's persisted vector representation to a fixed-dimension PostgreSQL `vector` column, enable the `vector` extension in its initial migration, and create a cosine-distance index suitable for the initial embedding dimension.

Configure `DependencyInjection.AddInfrastructure` to validate the provider name, call `UseSqlServer` for the default path and `UseNpgsql` for `Postgres`, then register the selected context behind the common persistence type. Keep Identity and existing repository registrations functional for both paths.

- [ ] **Step 4: Run configuration, existing repository, and migration generation checks**

Run: `dotnet test tests/InsightVault.Tests --filter "FullyQualifiedName~DatabaseConfigurationTests|FullyQualifiedName~DocumentRepositoryTests"`

Expected: PASS.

Run: `dotnet ef migrations list --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api --context ApplicationDbContext`

Expected: lists the existing SQL Server migrations.

Run: `dotnet ef migrations list --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api --context PostgresApplicationDbContext`

Expected: lists the PostgreSQL initial migration without connecting to AWS.

- [ ] **Step 5: Commit**

```bash
git add src/InsightVault.Infrastructure src/InsightVault.Api/appsettings.json src/InsightVault.Api/appsettings.Development.json tests/InsightVault.Tests/Infrastructure/DatabaseConfigurationTests.cs
git commit -m "feat: add PostgreSQL pgvector persistence path"
```

### Task 3: Database-backed vector retrieval

**Files:**
- Create: `src/InsightVault.Application/Interfaces/IVectorSearchRepository.cs`
- Modify: `src/InsightVault.Application/Features/Search/SemanticSearchService.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/Repositories/DocumentRepository.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Repositories/PostgresVectorSearchRepository.cs`
- Modify: `src/InsightVault.Infrastructure/DependencyInjection.cs`
- Test: `tests/InsightVault.Tests/Application/SemanticSearchServiceTests.cs`
- Test: `tests/InsightVault.Tests/Infrastructure/PostgresVectorSearchTests.cs`

**Interfaces:**
- Consumes: `IEmbeddingService.GenerateEmbeddingAsync(string, CancellationToken)`, `SearchDocumentsQuery`, and the selected persistence context.
- Produces: `IVectorSearchRepository.SearchAsync(VectorSearchRequest request, CancellationToken)` returning `IReadOnlyList<VectorSearchMatch>` with document ID/name, chunk ID/index, text, and cosine score.

- [ ] **Step 1: Write failing Application and PostgreSQL integration tests**

Update `SemanticSearchServiceTests` so the stub repository verifies it receives the generated embedding, owner ID, and `MaxResults`; it returns pre-ranked matches without Application-side cosine computation. Add `PostgresVectorSearchTests` using a disposable pgvector Testcontainers database that asserts: closest vector ranks first; an owner sees their document; a shared viewer sees only the shared document; an unrelated viewer sees neither; chunks without embeddings are excluded; and a dimension mismatch produces a clear failure.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/InsightVault.Tests --filter "FullyQualifiedName~SemanticSearchServiceTests|FullyQualifiedName~PostgresVectorSearchTests"`

Expected: Application tests fail because `IVectorSearchRepository` does not exist; PostgreSQL tests fail for the same reason before a container is started.

- [ ] **Step 3: Implement the provider-neutral contract and repository implementations**

Create `VectorSearchRequest(string OwnerUserId, IReadOnlyList<float> QueryEmbedding, int MaxResults)` and `VectorSearchMatch(Guid DocumentId, string DocumentName, Guid ChunkId, int ChunkIndex, string Text, double Score)` in the Application interface file. Change `SemanticSearchService.SearchAsync` to validate the request, generate one embedding, call `IVectorSearchRepository.SearchAsync`, and map matches to `SearchResultDto`; remove its in-memory document load and cosine algorithm.

Implement the PostgreSQL repository using parameterised EF Core/Npgsql query support for pgvector cosine distance. The SQL/query must join documents, chunks, and embeddings, constrain the owner-or-viewer permission predicate before ordering, ignore null embeddings, order by cosine distance then document name/chunk index, and limit by `MaxResults`. Register it only for `Postgres`. Implement the SQL Server fallback behind the same interface using the current authorised document load and cosine algorithm, preserving present behaviour until SQL Server is retired.

- [ ] **Step 4: Run focused and full backend tests**

Run: `dotnet test tests/InsightVault.Tests --filter "FullyQualifiedName~SemanticSearchServiceTests|FullyQualifiedName~PostgresVectorSearchTests"`

Expected: PASS, with Docker running for the pgvector integration tests.

Run: `dotnet test InsightVault.slnx`

Expected: PASS; no test makes an AWS or Azure OpenAI call.

- [ ] **Step 5: Commit**

```bash
git add src/InsightVault.Application src/InsightVault.Infrastructure tests/InsightVault.Tests/Application/SemanticSearchServiceTests.cs tests/InsightVault.Tests/Infrastructure/PostgresVectorSearchTests.cs
git commit -m "feat: search PostgreSQL vectors in the database"
```

### Task 4: Validation-only AWS Terraform foundation

**Files:**
- Create: `infra/aws/versions.tf`
- Create: `infra/aws/providers.tf`
- Create: `infra/aws/variables.tf`
- Create: `infra/aws/main.tf`
- Create: `infra/aws/outputs.tf`
- Create: `infra/aws/README.md`
- Modify: `README.md`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: Terraform CLI, the optional `AWS_PROFILE=insightvault-dev` local environment variable, and no Terraform state.
- Produces: an AWS Terraform module that `init -backend=false`, `fmt -check`, and `validate` can run without an AWS apply or credentials.

- [ ] **Step 1: Add a failing CI validation expectation**

Add an AWS Terraform validation job/step that targets `infra/aws` with `terraform fmt -check -recursive`, `terraform init -backend=false`, and `terraform validate`. It must contain no `terraform plan` or `terraform apply` command.

- [ ] **Step 2: Run the pipeline-equivalent Terraform command to verify it fails**

Run: `terraform -chdir=infra/aws validate`

Expected: FAIL because the directory and Terraform configuration do not yet exist.

- [ ] **Step 3: Create the provider and tag foundation**

Pin compatible Terraform and AWS provider version ranges, expose `aws_region` with default `ap-southeast-2`, and require `project_name`, `environment`, and `managed_by` tags. Configure provider default tags only. Do not declare resources, remote state, access keys, or a default apply workflow. Document that `init -backend=false` and `validate` are safe checks; document `AWS_PROFILE=insightvault-dev` only as future manual authentication context, not as an application credential source.

- [ ] **Step 4: Run Terraform and CI configuration checks**

Run: `terraform -chdir=infra/aws fmt -check -recursive`

Expected: PASS.

Run: `terraform -chdir=infra/aws init -backend=false`

Expected: PASS without creating AWS resources.

Run: `terraform -chdir=infra/aws validate`

Expected: PASS without creating AWS resources.

- [ ] **Step 5: Commit**

```bash
git add infra/aws README.md .github/workflows/ci.yml
git commit -m "chore: add validation-only AWS Terraform foundation"
```

### Task 5: Milestone verification and learning handoff

**Files:**
- Modify: `README.md`
- Modify: `docs/deployment.md`

**Interfaces:**
- Consumes: the completed Docker Compose profile, PostgreSQL migration, test suite, and Terraform module.
- Produces: a reproducible, no-cost local validation checklist and an accurate scope statement for the AWS portfolio evidence.

- [ ] **Step 1: Document the exact local evidence commands**

Add a “pgvector foundation evidence” section that gives these commands in order: start the PostgreSQL profile, run the PostgreSQL migration, run the integration-test filter, run the full test suite, run Terraform formatting/init/validation, and stop containers. State which commands require Docker and which never contact AWS.

- [ ] **Step 2: Run the full evidence sequence**

Run: `docker compose --profile postgres up -d postgres`

Expected: PostgreSQL health check becomes healthy.

Run: `dotnet test InsightVault.slnx`

Expected: PASS.

Run: `terraform -chdir=infra/aws fmt -check -recursive; terraform -chdir=infra/aws validate`

Expected: PASS; no AWS resource is created.

Run: `docker compose --profile postgres down`

Expected: containers stop while the named volume remains intact unless the documented reset command is deliberately chosen.

- [ ] **Step 3: Review the diff and commit**

Run: `git diff --check HEAD~5..HEAD`

Expected: no output.

```bash
git add README.md docs/deployment.md
git commit -m "docs: add pgvector foundation verification guide"
```

## Self-Review

- Spec coverage: Tasks 1–3 implement the first milestone's local pgvector service, provider-specific path, vector persistence/retrieval, permission filtering, and xUnit evidence. Task 4 adds only the Terraform provider/tag foundation required for this milestone. Task 5 turns the result into reproducible portfolio evidence. S3, SQS, a worker, and deployable AWS resources are deliberately deferred to their own approved plans.
- Step scan: each task begins with a focused failing check, implements a named boundary, reruns an explicit command, and ends with its own commit.
- Type consistency: `SemanticSearchService` consumes `IVectorSearchRepository`; its `VectorSearchRequest` and `VectorSearchMatch` records are defined in the same Application interface file and are implemented by both the SQL Server fallback and PostgreSQL repository.
- Review focus: the five listed risks are pinned to Task 1, 2, or 3 tests rather than left as general aspirations.
- Proportion: the plan specifies boundaries and proofs, not implementation bodies or cloud resources.
