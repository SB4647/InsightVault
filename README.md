# InsightVault

InsightVault is an AI-powered document intelligence platform built with .NET, React, SQL Server, Azure Blob Storage, and planned Azure OpenAI integration.

The current implementation covers Phase 1: uploading documents, storing files in Azure Blob Storage, saving document metadata in SQL Server, and displaying uploaded documents in the React client.

---

## Current Features

- Document upload API
- Uploaded document list API
- Azure Blob Storage integration
- SQL Server metadata persistence with Entity Framework Core
- Initial EF Core migration for the `Documents` table
- React + TypeScript upload and document list UI
- Clean Architecture project structure
- xUnit tests for Domain and Application behavior

Not implemented yet:

- PDF text extraction
- Document chunking
- Embeddings
- Semantic search
- RAG chat
- Authentication
- Background processing
- Azure AI Search
- Agents

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
- SQL Server
- Azure Blob Storage
```

### Project Responsibilities

#### `InsightVault.Api`

- HTTP endpoints
- Request/response handling
- Dependency injection composition
- CORS and API configuration

#### `InsightVault.Application`

- Use cases and workflow orchestration
- DTOs
- Commands
- Interfaces for external dependencies
- Document upload/list application service

#### `InsightVault.Domain`

- Domain entities
- Domain enums
- Business validation

#### `InsightVault.Infrastructure`

- EF Core `ApplicationDbContext`
- SQL Server repository implementation
- Azure Blob Storage implementation
- Infrastructure dependency registration
- EF Core migrations

#### `InsightVault.Client`

- React + TypeScript frontend built with Vite
- Upload form
- Uploaded document list

#### `InsightVault.Tests`

- xUnit tests for Domain and Application logic

---

## Solution Structure

```text
InsightVault
|
|-- src
|   |-- InsightVault.Api
|   |-- InsightVault.Application
|   |-- InsightVault.Domain
|   |-- InsightVault.Infrastructure
|   |-- InsightVault.Client
|
|-- tests
|   |-- InsightVault.Tests
|
|-- docs
```

---

## API Endpoints

### Upload Document

```http
POST /api/documents
Content-Type: multipart/form-data
```

Form field:

- `file`: document file

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
  }
}
```

Set `AzureBlobStorage:ConnectionString` with user secrets or environment-specific configuration before using document upload.

Example:

```bash
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your-connection-string>" --project src/InsightVault.Api
```

---

## Database

Create or update the local SQL Server database with:

```bash
dotnet ef database update --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api
```

The initial migration creates the `Documents` table.

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

### Phase 2: Document Processing

- [ ] Extract text from uploaded PDFs
- [ ] Implement document chunking
- [ ] Generate embeddings
- [ ] Store vectors

### Phase 3: Semantic Search

- [ ] Semantic search
- [ ] Similarity ranking
- [ ] Search API

### Phase 4: RAG Chat

- [ ] RAG implementation
- [ ] Chat interface
- [ ] Source citations

### Phase 5: Advanced Features

- [ ] Authentication
- [ ] User accounts
- [ ] Document permissions
- [ ] Document versioning

---

## Purpose

InsightVault is a learning and portfolio project designed to demonstrate:

- Clean Architecture with ASP.NET Core
- Practical document ingestion workflows
- SQL Server and Azure Blob Storage integration
- React + TypeScript frontend development
- Future AI Engineering and RAG capabilities
