# InsightVault

InsightVault is an AI-powered document intelligence platform built with .NET, React, SQL Server, Azure Blob Storage, and Azure OpenAI.

The current implementation covers:

- Phase 1: document upload and document list
- Phase 2: PDF text extraction, document chunking, embedding generation, and vector persistence
- Phase 3: semantic search, similarity ranking, and search API

RAG chat, authentication, background jobs, Azure AI Search, and agents are not implemented yet.

---

## Current Features

- Upload PDF documents from the React client
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
- Dependency injection composition
- CORS and API configuration

#### `InsightVault.Application`

- Document upload/list use cases
- Document processing workflow
- Semantic search workflow
- Chunking service
- DTOs and commands
- Interfaces for storage, repositories, text extraction, and embeddings

#### `InsightVault.Domain`

- `Document`
- `DocumentChunk`
- `Embedding`
- `DocumentProcessingStatus`
- Domain validation and processing state transitions

#### `InsightVault.Infrastructure`

- EF Core `ApplicationDbContext`
- SQL Server repository implementation
- Azure Blob Storage implementation
- PdfPig PDF text extraction
- Azure OpenAI embedding adapter
- EF Core migrations

#### `InsightVault.Client`

- React + TypeScript frontend built with Vite
- Upload form
- Uploaded document list
- Process document action
- Semantic search panel
- Status and chunk count display

#### `InsightVault.Tests`

- xUnit tests for Domain and Application logic

---

## API Endpoints

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

### Process Document

```http
POST /api/documents/{id}/process
```

Processing does the Phase 2 workflow:

1. Downloads the uploaded file from Azure Blob Storage.
2. Extracts PDF text.
3. Splits text into overlapping chunks.
4. Generates one embedding per chunk.
5. Stores chunks and embedding vectors in SQL Server.

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
    "ApiVersion": "2024-02-01"
  }
}
```

Use user secrets or environment-specific configuration for local secrets:

```bash
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<blob-storage-connection-string>" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource-name>.openai.azure.com" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "<azure-openai-api-key>" --project src/InsightVault.Api
dotnet user-secrets set "AzureOpenAI:EmbeddingDeploymentName" "<embedding-deployment-name>" --project src/InsightVault.Api
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

- [x] Extract text from uploaded PDFs
- [x] Implement document chunking
- [x] Generate embeddings
- [x] Store vectors

### Phase 3: Semantic Search

- [x] Semantic search
- [x] Similarity ranking
- [x] Search API

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
- Practical document ingestion and processing workflows
- SQL Server and Azure Blob Storage integration
- Azure OpenAI embedding integration
- React + TypeScript frontend development
- Future semantic search and RAG capabilities
