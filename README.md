# InsightVault

InsightVault is an AI-powered document intelligence platform built with .NET, React, SQL Server, Azure OpenAI, and Azure Blob Storage.

The application allows users to upload documents, extract and store content, generate embeddings, perform semantic search, and interact with documents using Retrieval-Augmented Generation (RAG).

---

## Features

### Current

- Document upload
- Azure Blob Storage integration
- Document metadata storage
- REST API
- React frontend
- Clean Architecture structure

### Planned

- PDF text extraction
- Document chunking
- Azure OpenAI embeddings
- Semantic search
- RAG chat experience
- User authentication
- Document versioning
- Multi-user support
- Citation-based responses
- Agent integration through Azure AI Foundry

---

## Architecture

```text
React Client
     │
     ▼
ASP.NET Core API
     │
     ├── SQL Server
     │
     ├── Azure Blob Storage
     │
     └── Azure OpenAI
```

### Solution Structure

```text
InsightVault
│
├── src
│   ├── InsightVault.Api
│   ├── InsightVault.Application
│   ├── InsightVault.Domain
│   ├── InsightVault.Infrastructure
│   └── insightvault.client
│
├── tests
│   └── InsightVault.Tests
│
└── docs
```

---

## Projects

### InsightVault.Api

ASP.NET Core Web API responsible for:

- Controllers
- Dependency Injection
- Authentication
- API endpoints

### InsightVault.Application

Contains:

- Use cases
- Business workflows
- DTOs
- Interfaces
- Commands and queries

### InsightVault.Domain

Contains:

- Entities
- Value objects
- Business rules
- Domain models

### InsightVault.Infrastructure

Contains integrations with:

- SQL Server
- Azure Blob Storage
- Azure OpenAI
- External services

### insightvault.client

React + TypeScript frontend built with Vite.

### InsightVault.Tests

Unit and integration tests.

---

## Technology Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

### Frontend

- React
- TypeScript
- Vite

### Cloud

- Azure Blob Storage
- Azure OpenAI
- Azure AI Foundry

### Testing

- xUnit

---

## Development Roadmap

### Phase 1

- [ ] Create database schema
- [ ] Implement document upload API
- [ ] Save files to Azure Blob Storage
- [ ] Store metadata in SQL Server
- [ ] Display uploaded documents

### Phase 2

- [ ] Extract text from uploaded PDFs
- [ ] Implement document chunking
- [ ] Generate embeddings
- [ ] Store vectors

### Phase 3

- [ ] Semantic search
- [ ] Similarity ranking
- [ ] Search API

### Phase 4

- [ ] RAG implementation
- [ ] Chat interface
- [ ] Source citations

### Phase 5

- [ ] Authentication
- [ ] User accounts
- [ ] Document permissions

---

## Getting Started

### Clone Repository

```bash
git clone https://github.com/your-username/InsightVault.git
```

### Backend

```bash
cd src/InsightVault.Api
dotnet run
```

### Frontend

```bash
cd src/insightvault.client

npm install
npm run dev
```

---

## Inspiration

InsightVault is a learning and portfolio project designed to explore:

- AI Engineering
- Retrieval-Augmented Generation (RAG)
- Azure OpenAI
- Document Intelligence
- Semantic Search
- Modern .NET Architecture