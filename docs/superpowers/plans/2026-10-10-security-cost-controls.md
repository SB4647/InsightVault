# Security and Cost Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add local-first API protection and a deployment-safe AWS permissions design for InsightVault.

**Architecture:** Application services own document-quota and question-safety decisions. The API supplies rate limiting and metadata-only request logging. Infrastructure owns the Azure OpenAI prompt boundary and Terraform owns opt-in workload policies for future ECS tasks.

**Tech Stack:** ASP.NET Core rate limiting, .NET 10, xUnit, Terraform AWS provider 6.x.

**Spec:** `docs/superpowers/specs/2026-10-10-security-cost-controls-design.md`

## Global Constraints

- Domain has no Application or Infrastructure dependency; Application has no Infrastructure dependency.
- Controllers remain thin, async, and dependency-injected.
- New public methods include XML summary comments.
- AWS deployment remains opt-in; no `terraform apply`, secrets, or cloud resources are created.

## Review Focus

- An authenticated user must not evade quota by accessing shared documents; quota counts only owned documents.
- Authentication requests must be partitioned before an identity exists, while user operations use the authenticated user ID.
- Unsafe questions must be rejected without their text reaching logs or the chat provider.
- Document excerpts can contain hostile instructions but must remain reference data, not a command source.
- IAM policy resources must not expand to wildcard secrets or cross-role SQS permissions.

### Task 1: Upload quota and question safety

**Files:** application quota/safety services and options; `DocumentService`, `ChatService`, `IDocumentRepository`; EF repository; application tests.

- [ ] Write failing tests for owner-only storage quota and high-confidence prompt-injection rejection.
- [ ] Add `UploadQuotaOptions`, `DocumentUsage`, `IQuestionSafetyService`, and implementations through the Application boundary.
- [ ] Extend the document repository with an owned-document usage query and enforce the quota before blob upload.
- [ ] Validate questions before retrieval or chat completion, without logging question content.
- [ ] Run focused application tests and commit `feat: add upload quota and question safety`.

### Task 2: Prompt boundary and safe operational logging

**Files:** Azure OpenAI chat adapter; API middleware/options/startup; worker; API and infrastructure tests.

- [ ] Write failing tests for untrusted-source prompt markers and metadata-only request logging.
- [ ] Make the system message declare excerpts untrusted, and delimit each source in the user message.
- [ ] Add safe request logging middleware and configure it without reading bodies or headers.
- [ ] Replace worker exception logging with document ID plus exception type.
- [ ] Run focused tests and commit `feat: add safe logging and prompt boundaries`.

### Task 3: API rate limits and configuration

**Files:** API startup/controllers/configuration; API tests; README.

- [ ] Write failing integration tests for authentication and chat/upload rate-limit policies.
- [ ] Register fixed-window policies using IP for anonymous authentication and user identity for authenticated operations.
- [ ] Apply named policies to auth, upload, chat, and search endpoints; return a safe 429 response.
- [ ] Document defaults and local configuration overrides without recording credentials.
- [ ] Run API tests and commit `feat: add API abuse controls`.

### Task 4: Future workload IAM and cost documentation

**Files:** `infra/aws/main.tf`, `variables.tf`, `outputs.tf`, `README.md`, root README; Terraform validation evidence.

- [ ] Add failing Terraform policy assertions through structural tests or validation-focused checks.
- [ ] Declare opt-in API and worker ECS task roles with separate S3, SQS, and supplied-secret permissions.
- [ ] Add optional API/worker secret ARN variables; never declare a Secrets Manager secret resource.
- [ ] Document IAM as free, S3/SQS/Secrets Manager cost triggers, defaults, and no-deploy checks.
- [ ] Run `terraform fmt -check -recursive`, `init -backend=false`, and `validate`; commit `feat: add deployment security guardrails`.

### Task 5: Whole-feature verification

**Files:** README evidence only if commands or outcomes change.

- [ ] Run the complete backend suite, API build, client build, and Terraform checks.
- [ ] Review the complete branch against the spec, address important findings test-first, and commit any fix.
