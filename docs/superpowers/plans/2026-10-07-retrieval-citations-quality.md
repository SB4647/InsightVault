# Retrieval, Citations, and Quality Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add auditable hybrid retrieval with configurable quality controls, source metadata, and persisted answer provenance.

**Architecture:** Application owns retrieval settings validation, hybrid candidate fusion, answer provenance contracts, and chat orchestration. Infrastructure owns page-aware PDF extraction, EF Core persistence, and PostgreSQL/SQL Server candidate queries. The API and React client retain their thin existing request/response roles.

**Tech Stack:** .NET 10, EF Core 10, PostgreSQL pgvector/full-text search, SQL Server fallback, PdfPig, xUnit, React/TypeScript.

**Spec:** `docs/superpowers/specs/2026-10-07-retrieval-citations-quality-design.md`

## Global Constraints

- Domain must not reference Application or Infrastructure; Application must not reference Infrastructure.
- Preserve Azure Blob as the default storage provider and the existing optional S3 path.
- Do not deploy AWS, Azure, PostgreSQL, or SQS resources and introduce no static cloud credentials.
- Top-K and minimum similarity are server-owned, bounded configuration values; callers cannot request unbounded retrieval.
- Every candidate query remains restricted to processed documents and existing owner/viewer permissions.
- Persist answer provenance only after successful answer generation; do not add a user-facing answer-history UI.
- New public or non-obvious methods receive XML summary comments.

## Review Focus

- A user cannot retrieve an unshared chunk through either full-text or vector paths; Task 4 tests owner/viewer permission filtering.
- A low-quality full-text-only result cannot bypass the configured similarity threshold; Task 3 tests threshold filtering.
- A duplicate returned by both paths appears once with a stable rank; Task 3 tests fusion de-duplication.
- A generation or save failure cannot leave an orphan answer/citation graph; Task 5 tests transactional failure behaviour.
- Existing rows migrate to valid metadata defaults without becoming unsearchable; Task 2 inspects migrations and persistence.

---

### Task 1: Add page-aware document and chunk metadata

**Files:**
- Modify: `src/InsightVault.Domain/Entities/Document.cs`
- Modify: `src/InsightVault.Domain/Entities/DocumentChunk.cs`
- Create: `src/InsightVault.Application/Interfaces/ExtractedDocumentPage.cs`
- Modify: `src/InsightVault.Application/Interfaces/ITextExtractionService.cs`
- Modify: `src/InsightVault.Application/Features/Documents/Processing/IDocumentChunkingService.cs`
- Modify: `src/InsightVault.Application/Features/Documents/Processing/DocumentChunkingService.cs`
- Modify: `src/InsightVault.Application/Features/Documents/Processing/DocumentProcessingService.cs`
- Modify: `src/InsightVault.Infrastructure/Documents/PdfTextExtractionService.cs`
- Test: `tests/InsightVault.Tests/Application/DocumentChunkingServiceTests.cs`
- Test: `tests/InsightVault.Tests/Application/DocumentProcessingServiceTests.cs`

**Interfaces:** Consumes current extraction/chunking services. Produces `ExtractedDocumentPage`, page-aware `DocumentTextChunk`, `Document.Version`, and source metadata.

- [ ] Write failing tests: new document version is `1`; chunks retain source page/section; processing passes source metadata into persisted chunks.
- [ ] Run `dotnet test tests/InsightVault.Tests/InsightVault.Tests.csproj --filter "FullyQualifiedName~DocumentChunkingServiceTests|FullyQualifiedName~DocumentProcessingServiceTests"`; expect failure because page-aware contracts do not exist.
- [ ] Implement `ITextExtractionService.ExtractPagesAsync(Stream, CancellationToken) -> IReadOnlyList<ExtractedDocumentPage>`; PdfPig emits 1-based pages with null section titles.
- [ ] Implement `IDocumentChunkingService.Chunk(IReadOnlyList<ExtractedDocumentPage>, int, int)` returning chunks with text, index, source page, and optional section. Preserve current validation.
- [ ] Add `Document.Version`, `DocumentChunk.SourcePageNumber`, and `DocumentChunk.SectionTitle`; reject non-positive pages and normalize section titles.
- [ ] Update processing to embed the page-aware chunks without changing idempotency/failure semantics.
- [ ] Re-run the focused tests; expect pass.
- [ ] Commit `feat: preserve document chunk source metadata`.

### Task 2: Persist metadata and answer provenance

**Files:**
- Create: `src/InsightVault.Domain/Entities/ChatAnswer.cs`
- Create: `src/InsightVault.Domain/Entities/ChatAnswerCitation.cs`
- Create: `src/InsightVault.Application/Interfaces/IChatAnswerRepository.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Configurations/ChatAnswerConfiguration.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Configurations/ChatAnswerCitationConfiguration.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/Configurations/DocumentConfiguration.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/Configurations/DocumentChunkConfiguration.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Repositories/ChatAnswerRepository.cs`
- Modify: `src/InsightVault.Infrastructure/DependencyInjection.cs`
- Create: EF-generated `AddRetrievalProvenance` migration under `src/InsightVault.Infrastructure/Persistence/Migrations/`
- Create: EF-generated `AddRetrievalProvenance` migration under `src/InsightVault.Infrastructure/Persistence/PostgresMigrations/`
- Test: `tests/InsightVault.Tests/Infrastructure/DocumentRepositoryTests.cs`
- Test: `tests/InsightVault.Tests/Infrastructure/ChatAnswerRepositoryTests.cs`

**Interfaces:** Consumes Task 1 metadata. Produces `IChatAnswerRepository.SaveAsync(ChatAnswer, CancellationToken)` for transactional answer/citation storage.

- [ ] Write failing SQLite tests for metadata defaults, answer/citation insertion, citation rank ordering, and cascade deletion.
- [ ] Run `dotnet test tests/InsightVault.Tests/InsightVault.Tests.csproj --filter "FullyQualifiedName~DocumentRepositoryTests|FullyQualifiedName~ChatAnswerRepositoryTests"`; expect missing entities/repository.
- [ ] Map version/source fields plus answer/citation tables with owner/created indexes and document/chunk foreign keys.
- [ ] Generate and inspect SQL Server/PostgreSQL migrations, ensuring existing rows get valid defaults.
- [ ] Register the repository through Infrastructure DI and keep answer/citation save atomic in one DbContext save.
- [ ] Re-run focused persistence tests and migration build checks; expect pass.
- [ ] Commit `feat: persist answer citation provenance`.

### Task 3: Add configurable hybrid ranking in Application

**Files:**
- Create: `src/InsightVault.Application/Features/Search/RetrievalOptions.cs`
- Create: `src/InsightVault.Application/Features/Search/HybridRetrievalService.cs`
- Modify: `src/InsightVault.Application/Features/Search/ISemanticSearchService.cs`
- Modify: `src/InsightVault.Application/Features/Search/SemanticSearchService.cs`
- Modify: `src/InsightVault.Application/Features/Search/DTOs/SearchResultDto.cs`
- Modify: `src/InsightVault.Application/Interfaces/IVectorSearchRepository.cs`
- Create: `src/InsightVault.Application/Interfaces/IFullTextSearchRepository.cs`
- Test: `tests/InsightVault.Tests/Application/HybridRetrievalServiceTests.cs`
- Test: `tests/InsightVault.Tests/Application/SemanticSearchServiceTests.cs`

**Interfaces:** Consumes embedding generation/provider candidates. Produces permission-safe fused search results with version/page/section metadata.

- [ ] Write failing tests for Top-K, threshold filtering, de-duplication by chunk ID, stable tie-breaks, reciprocal-rank fusion, and full-text-only scores.
- [ ] Run `dotnet test tests/InsightVault.Tests/InsightVault.Tests.csproj --filter "FullyQualifiedName~HybridRetrievalServiceTests|FullyQualifiedName~SemanticSearchServiceTests"`; expect missing hybrid contracts.
- [ ] Define validated `RetrievalOptions`: `TopK = 5`, `CandidateMultiplier = 4`, `MinimumSimilarity = 0.60`, `FullTextOnlyScore = 0.60`.
- [ ] Define candidate records containing document/chunk IDs, version, page/section, text, vector score, and provider rank.
- [ ] Implement reciprocal-rank fusion with `k = 60`; apply threshold and stable final ordering.
- [ ] Update semantic search to request bounded candidates and return final fused results.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit `feat: add configurable hybrid retrieval ranking`.

### Task 4: Implement permission-safe full-text and vector candidates

**Files:**
- Modify: `src/InsightVault.Infrastructure/Persistence/Repositories/PostgresVectorSearchRepository.cs`
- Modify: `src/InsightVault.Infrastructure/Persistence/Repositories/DocumentRepository.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Repositories/PostgresFullTextSearchRepository.cs`
- Create: `src/InsightVault.Infrastructure/Persistence/Repositories/DocumentFullTextSearchRepository.cs`
- Modify: `src/InsightVault.Infrastructure/DependencyInjection.cs`
- Modify: `src/InsightVault.Api/appsettings.json`
- Modify: `src/InsightVault.Api/appsettings.Development.json`
- Test: `tests/InsightVault.Tests/Infrastructure/PostgresVectorSearchTests.cs`
- Test: `tests/InsightVault.Tests/Infrastructure/FullTextSearchRepositoryTests.cs`

**Interfaces:** Consumes Task 3 repository contracts. Produces provider-specific vector and full-text candidate sets with the same permission rules.

- [ ] Write failing tests proving owners/viewers see permitted processed chunks and blocked users do not; prove text candidate ranking returns source metadata.
- [ ] Run `dotnet test tests/InsightVault.Tests/InsightVault.Tests.csproj --filter "FullyQualifiedName~PostgresVectorSearchTests|FullyQualifiedName~FullTextSearchRepositoryTests"`; expect missing full-text adapters/metadata projection.
- [ ] Extend PostgreSQL vector projection; add parameterised PostgreSQL `to_tsvector`/`plainto_tsquery` full-text candidates.
- [ ] Add safe SQL Server/local EF full-text candidate search, retaining permission and processed-document filters.
- [ ] Bind the `Retrieval` section and register provider-specific adapters.
- [ ] Re-run provider and existing pgvector tests; expect pass.
- [ ] Commit `feat: add permission-safe full-text retrieval`.

### Task 5: Persist chat provenance and add quality evaluation

**Files:**
- Modify: `src/InsightVault.Application/Features/Chat/ChatService.cs`
- Modify: `src/InsightVault.Application/Features/Chat/DTOs/ChatResponseDto.cs`
- Modify: `src/InsightVault.Application/Features/Chat/DTOs/SourceCitationDto.cs`
- Modify: `src/InsightVault.Application/Features/Chat/ChatCompletionContext.cs`
- Modify: `src/InsightVault.Api/Controllers/ChatController.cs`
- Modify: `src/InsightVault.Client/src/api/chat.ts`
- Create: `tests/InsightVault.Tests/TestData/retrieval-evaluation.json`
- Create: `tests/InsightVault.Tests/Application/RetrievalEvaluationTests.cs`
- Modify: `tests/InsightVault.Tests/Application/ChatServiceTests.cs`
- Modify: `README.md`

**Interfaces:** Consumes Task 2 persistence and Task 3 search results. Produces provenance only after successful answer generation and repeatable retrieval-quality evidence.

- [ ] Write failing tests: success saves ordered citations; no-result skips persistence; generation/save failures create no partial provenance.
- [ ] Write failing evaluation tests using JSON requiring recall@5 >= `0.80` and precision@5 >= `0.60` with deterministic doubles.
- [ ] Run `dotnet test tests/InsightVault.Tests/InsightVault.Tests.csproj --filter "FullyQualifiedName~ChatServiceTests|FullyQualifiedName~RetrievalEvaluationTests"`; expect missing persistence/evaluation data.
- [ ] Extend contexts/DTOs with metadata; save answer/citations after successful model output using configured settings and strategy version `1`.
- [ ] Keep API request shape bounded: question only, server retrieval defaults. Update client citation types for the new metadata.
- [ ] Document local evaluation and the audit-only provenance boundary.
- [ ] Run focused tests, full backend tests, `npm run build` in `src/InsightVault.Client`, and provider migration builds; expect pass.
- [ ] Commit `feat: add answer provenance and retrieval evaluation`.

## Plan Self-Review

- Task 1 covers extraction-to-chunk version/page/section metadata.
- Task 2 covers both-provider persistence/migration safety for metadata and provenance.
- Tasks 3 and 4 cover provider-neutral fusion plus provider-specific candidates.
- Task 5 covers answer provenance, no-result/failure behaviour, evaluation, and client/API contracts.
- The plan adds no answer-history UI, cloud deployment, static credentials, or unbounded client ranking controls.
