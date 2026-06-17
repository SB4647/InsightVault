# InsightVault

InsightVault is an AI-powered document intelligence platform built with .NET, React, SQL Server, Azure Blob Storage, and Azure OpenAI.

The current implementation covers:

- Phase 1: document upload and document list
- Phase 2: PDF text extraction, document chunking, embedding generation, and vector persistence
- Phase 3: semantic search, similarity ranking, and search API
- Phase 4: RAG chat, grounded answers, and source citations
- Phase 5A: local user accounts, JWT authentication, and document ownership
- Phase 5B: shared document permissions for viewer access

Document versioning, background jobs, Azure AI Search, and agents are not implemented yet.

---

## Current Features

- Upload PDF documents from the React client
- Register and log in with local user accounts
- Protect document, search, and chat APIs with JWT bearer authentication
- Scope uploaded documents, search results, and chat answers to the current user
- Share owned documents with other registered users as viewers
- Include shared viewer documents in document lists, semantic search, and RAG chat
- Delete owned documents from the UI and API
- Store uploaded files in Azure Blob Storage
- Store document metadata in SQL Server
- List uploaded documents
- Trigger document processing from the document list
- Extract text from uploaded PDFs with PdfPig
- Split extracted text into overlapping chunks
- Generate embeddings through Azure OpenAI
- Persist chunks and embedding vectors in SQL Server
- Search processed document chunks semantically
- Rank search results by cosine similarity
- Ask questions against processed documents with RAG chat
- Generate grounded answers through Azure OpenAI chat completions
- Return source citations for chat answers
- Track document processing status: `Uploaded`, `Processing`, `Processed`, `Failed`
- Clean Architecture project structure
- xUnit tests for Domain and Application behavior

---

## Architecture

```text
React Client
     |
     v
ASP.NET Core API
     |
     v
Application
     |
     v
Domain

Infrastructure implements Application interfaces for:
- SQL Server / EF Core
- Azure Blob Storage
- PDF text extraction
- Azure OpenAI embeddings
```

### Project Responsibilities

#### `InsightVault.Api`

- HTTP endpoints
- Request/response handling
- JWT authentication and authorization
- Dependency injection composition
- CORS and API configuration

#### `InsightVault.Application`

- Document upload/list use cases
- Document processing workflow
- Semantic search workflow
- RAG chat workflow
- Owner-scoped document access contracts
- Viewer permission checks for shared document access
- Chunking service
- DTOs and commands
- Interfaces for storage, repositories, text extraction, embeddings, and chat completion

#### `InsightVault.Domain`

- `Document`
- `DocumentChunk`
- `Embedding`
- `DocumentProcessingStatus`
- Domain validation and processing state transitions

#### `InsightVault.Infrastructure`

- EF Core `ApplicationDbContext`
- ASP.NET Core Identity user persistence
- SQL Server repository implementation
- Azure Blob Storage implementation
- PdfPig PDF text extraction
- Azure OpenAI embedding adapter
- Azure OpenAI chat completion adapter
- EF Core migrations

#### `InsightVault.Client`

- React + TypeScript frontend built with Vite
- Upload form
- Login/register form
- Uploaded document list
- Share document form for owned documents
- Process document action
- Delete document action for owned documents
- Semantic search panel
- RAG chat panel
- Status and chunk count display

#### `InsightVault.Tests`

- xUnit tests for Domain and Application logic

---

## API Endpoints

Protected document, search, and chat endpoints require:

```http
Authorization: Bearer {token}
```

### Register

```http
POST /api/auth/register
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

Returns:

- `userId`
- `email`
- `token`

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

Returns:

- `userId`
- `email`
- `token`

### Upload Document

```http
POST /api/documents
Content-Type: multipart/form-data
```

Form field:

- `file`: PDF document

### List Documents

```http
GET /api/documents
```

Returns uploaded document metadata:

- `id`
- `originalFileName`
- `contentType`
- `sizeInBytes`
- `blobName`
- `uploadedAtUtc`
- `status`
- `chunkCount`
- `isOwner`
- `accessLevel`

The list includes documents owned by the current user and documents shared with the current user.

### Process Document

```http
POST /api/documents/{id}/process
```

Only the document owner can process a document.

Processing does the Phase 2 workflow:

1. Downloads the uploaded file from Azure Blob Storage.
2. Extracts PDF text.
3. Splits text into overlapping chunks.
4. Generates one embedding per chunk.
5. Stores chunks and embedding vectors in SQL Server.

### Share Document

```http
POST /api/documents/{id}/share
Content-Type: application/json
```

Only the document owner can share a document.

Request:

```json
{
  "email": "viewer@example.com"
}
```

Sharing grants viewer access. Viewers can list, search, and chat over shared processed documents, but they cannot process or re-share them.

Returns:

- `documentId`
- `sharedWithUserId`
- `sharedWithEmail`
- `accessLevel`

### Delete Document

```http
DELETE /api/documents/{id}
```

Only the document owner can delete a document.

Deleting removes the uploaded blob and the document metadata. Related chunks, embeddings, and permissions are removed through EF Core cascade behavior.

### Semantic Search

```http
GET /api/search?query={query}&maxResults=10
```

Search does the Phase 3 workflow:

1. Generates an embedding for the search query.
2. Loads processed document chunks and stored embeddings.
3. Calculates cosine similarity.
4. Returns ranked matching chunks.

Returns:

- `documentId`
- `documentName`
- `chunkId`
- `chunkIndex`
- `text`
- `score`

### RAG Chat

```http
POST /api/chat
Content-Type: application/json
```

Request:

```json
{
  "question": "What does this document say about the roadmap?",
  "maxSources": 5
}
```

Chat does the Phase 4 workflow:

1. Runs semantic search for the question.
2. Sends the top matching chunks to Azure OpenAI chat completions.
3. Returns a grounded answer.
4. Returns the chunks used as source citations.

Returns:

- `answer`
- `sources`
  - `documentId`
  - `documentName`
  - `chunkId`
  - `chunkIndex`
  - `text`
  - `score`

---

## Configuration

`src/InsightVault.Api/appsettings.json` contains default development settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InsightVault;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "AzureBlobStorage": {
    "ConnectionString": "",
    "ContainerName": "documents"
  },
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "EmbeddingDeploymentName": "",
    "ChatDeploymentName": "",
    "ApiVersion": "2024-10-21"
  },
  "Jwt": {
    "Issuer": "InsightVault",
    "Audience": "InsightVault.Client",
    "SigningKey": "development-only-signing-key-change-with-user-secrets",
    "ExpiresMinutes": 60
  }
}
```

Use user secrets or environment-specific configuration for local secrets:

```bash
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<blob-storage-connection-string>" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource-name>.openai.azure.com" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "<azure-openai-api-key>" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:EmbeddingDeploymentName" "<embedding-deployment-name>" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "<chat-deployment-name>" --project src/InsightVault.Api
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-character-signing-key>" --project src/InsightVault.Api
```

---

## Database

Create or update the local SQL Server database with:

```bash
dotnet ef database update --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api
```

Current migrations:

- `InitialCreate`: creates `Documents`
- `AddDocumentProcessing`: creates `DocumentChunks` and `Embeddings`
- `AddIdentityAndDocumentOwnership`: creates ASP.NET Core Identity tables and adds `Documents.OwnerUserId`
- `AddDocumentPermissions`: creates `DocumentPermissions`

---

## Docker

Docker support is intended for local production-shape development. It runs the API in a container and SQL Server in a container without creating paid Azure hosting resources.

Start the containers:

```bash
docker compose up --build
```

The API is available at:

```text
http://localhost:5089
```

SQL Server is available from the host at:

```text
localhost,14333
```

Run EF migrations against the Docker SQL Server database:

```bash
dotnet ef database update --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api --connection "Server=localhost,14333;Database=InsightVault;User Id=sa;Password=InsightVault-Local-Only-Password-123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Optional local secrets can be supplied through environment variables or by copying `docker-compose.override.example.yml` to `docker-compose.override.yml` and filling in local values. Do not commit `docker-compose.override.yml`.

Stop the containers:

```bash
docker compose down
```

Remove the local SQL Server volume if you want to reset the Docker database:

```bash
docker compose down --volumes
```

---

## Cloud Infrastructure

Terraform scaffolding lives in `infra/terraform`.

The current scaffold defaults to low-cost mode and manages/imports:

- Azure resource group, matching the existing `InsightVault-RG`
- Azure Storage for uploaded documents, matching the existing `insightvaultblobs`
- Azure AI Foundry / AI Services resource, matching the existing `insightvault-ai-resource`

Paid hosting resources are available behind `enable_paid_hosting = true`:

- Azure App Service Plan
- Azure Linux Web App for the API
- Azure SQL Server and database
- Azure Storage static website hosting for the React frontend
- Log Analytics workspace
- Application Insights

It includes commented placeholders for later production hardening such as Key Vault, managed identities, private networking, Azure AI Search, Foundry agents, multi-environment modules, and Kubernetes.

Start with:

```bash
cd infra/terraform
cp dev.tfvars.example dev.tfvars
terraform init
terraform plan -var-file="dev.tfvars"
```

Do not commit `dev.tfvars` or Terraform state files.

---

## Upcoming Work

This roadmap focuses on production practices, security, and deployment readiness before adding more product features.

### Stage 1: CI Quality Gates

Goal: prove every change builds, tests, and validates infrastructure before it is merged.

- [x] Add GitHub Actions workflow for backend build and tests
- [x] Add GitHub Actions workflow steps for frontend install, build, and lint
- [x] Add Terraform checks: `terraform fmt -check` and `terraform validate`
- [x] Run workflows on pull requests and pushes to the main branch
- [ ] Add status badges to the README after the workflow is stable

Portfolio value:

- Shows professional engineering discipline
- Makes the repo easier to review
- Demonstrates .NET, React, and Terraform validation in one pipeline

### Stage 2: Secret And Cost Safety

Goal: make it hard to leak secrets or accidentally create paid infrastructure.

- [x] Add Docker support for local API and SQL Server without Azure hosting cost
- [ ] Keep `dev.tfvars`, Terraform state, publish profiles, and local environment files ignored
- [ ] Document secret setup with user secrets locally and Azure app settings or Key Vault in production
- [ ] Keep `enable_paid_hosting = false` as the default Terraform mode
- [ ] Keep `prevent_destroy` on imported Azure resources
- [ ] Document monthly cost expectations before enabling paid hosting
- [ ] Add a short “rotate leaked secrets” checklist

Portfolio value:

- Shows security awareness
- Shows cost-aware cloud judgment
- Explains why production infrastructure is opt-in

### Stage 3: Application Security Hardening

Goal: tighten the current app without changing its architecture.

- [ ] Move CORS allowed origins into configuration instead of hardcoded values
- [ ] Strengthen upload validation for PDF extension, content type, empty files, and max size
- [ ] Avoid returning blob names to the frontend unless the UI needs them
- [ ] Improve frontend handling for expired JWTs and authorization failures
- [ ] Add tests for upload validation and authorization edge cases

Portfolio value:

- Shows practical API security
- Improves production readiness
- Creates good interview talking points around trust boundaries

### Stage 4: Deployment Documentation

Goal: make the production path understandable without requiring it to run all month.

- [ ] Add `docs/deployment.md`
- [ ] Document local development setup
- [ ] Document Terraform import-only mode
- [ ] Document optional paid hosting mode
- [ ] Document Azure resources required for Blob Storage and Foundry/OpenAI
- [ ] Document how to run EF migrations against Azure SQL when paid hosting is enabled
- [ ] Document how to tear down paid resources safely

Portfolio value:

- Shows you can communicate operational steps clearly
- Makes the repo easier for reviewers to understand
- Demonstrates production planning without unnecessary spend

### Stage 5: Optional Manual Cloud Deployment

Goal: support a full Azure deployment only when intentionally triggered.

- [ ] Add a `workflow_dispatch` GitHub Actions workflow for manual deployment
- [ ] Deploy the API to Azure App Service only when `enable_paid_hosting = true`
- [ ] Build the React app with the deployed API URL
- [ ] Upload React build artifacts to the configured frontend hosting target
- [ ] Run EF migrations as an explicit deployment step
- [ ] Keep automatic deploys disabled until costs and secrets are fully controlled

Portfolio value:

- Shows CI/CD knowledge
- Avoids surprise Azure costs
- Demonstrates a real production path that can be turned on when needed

Recommended next step: implement Stage 1 first. CI quality gates provide the highest portfolio value without increasing Azure cost.

---

## Getting Started

### Backend

```bash
dotnet restore
dotnet ef database update --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api
dotnet run --project src/InsightVault.Api
```

The API runs on the ports configured in `src/InsightVault.Api/Properties/launchSettings.json`.

### Frontend

```bash
cd src/InsightVault.Client
npm install
npm run dev
```

The client expects the API at:

```text
https://localhost:7227
```

Override it with:

```bash
VITE_API_BASE_URL=https://localhost:7227 npm run dev
```

---

## Verification

Backend build:

```bash
dotnet build InsightVault.slnx
```

Backend tests:

```bash
dotnet test InsightVault.slnx
```

Frontend build:

```bash
cd src/InsightVault.Client
npm run build
```

Frontend lint:

```bash
cd src/InsightVault.Client
npm run lint
```

---

## Development Roadmap

### Phase 1: Foundation

- [x] Create database schema
- [x] Implement document upload API
- [x] Save files to Azure Blob Storage
- [x] Store metadata in SQL Server
- [x] Display uploaded documents
- [x] Delete owned documents

### Phase 2: Document Processing

- [x] Extract text from uploaded PDFs
- [x] Implement document chunking
- [x] Generate embeddings
- [x] Store vectors

### Phase 3: Semantic Search

- [x] Semantic search
- [x] Similarity ranking
- [x] Search API

### Phase 4: RAG Chat

- [x] RAG implementation
- [x] Chat interface
- [x] Source citations

### Phase 5: Advanced Features

- [x] Authentication
- [x] User accounts
- [x] Document ownership
- [x] Shared document permissions
- [ ] Document versioning

---

## Purpose

InsightVault is a learning and portfolio project designed to demonstrate:

- Clean Architecture with ASP.NET Core
- Practical document ingestion and processing workflows
- SQL Server and Azure Blob Storage integration
- Azure OpenAI embedding integration
- Azure OpenAI chat completion integration
- ASP.NET Core Identity and JWT authentication
- Viewer-only document sharing
- React + TypeScript frontend development
- Future document versioning capabilities
