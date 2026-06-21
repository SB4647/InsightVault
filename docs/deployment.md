# InsightVault Deployment Notes

This document explains the supported ways to run InsightVault and the cloud path the repository is prepared for.

## Local Development

Use this mode when actively changing code.

Run the API from Visual Studio or the .NET CLI:

```powershell
dotnet run --project src/InsightVault.Api
```

Run the React client separately:

```powershell
cd src/InsightVault.Client
npm install
npm run dev
```

The client reads `VITE_API_BASE_URL` from `src/InsightVault.Client/.env.local`. For the Docker API, use:

```env
VITE_API_BASE_URL=http://localhost:5089
```

Do not commit `.env.local`.

## Docker API And SQL Server

This is the recommended repeatable local environment.

```powershell
docker compose up --build
```

This starts:

- API: `http://localhost:5089`
- SQL Server: `localhost,14333`

The API reads secrets from the root `.env` file. Required values:

```env
INSIGHTVAULT_SQL_PASSWORD=InsightVault-Local-Only-Password-123!
INSIGHTVAULT_JWT_SIGNING_KEY=replace-with-a-long-local-dev-signing-key

AZURE_BLOB_STORAGE_CONNECTION_STRING=replace-with-storage-connection-string
AZURE_BLOB_STORAGE_CONTAINER_NAME=documents

AZURE_OPENAI_ENDPOINT=https://insightvault-ai-resource.openai.azure.com
AZURE_OPENAI_API_KEY=replace-with-azure-openai-key
AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME=text-embedding-3-small
AZURE_OPENAI_CHAT_DEPLOYMENT_NAME=gpt-4.1-mini
AZURE_OPENAI_API_VERSION=2024-10-21
```

Do not commit `.env`.

If the SQL Server volume was created with an older `sa` password, changing `INSIGHTVAULT_SQL_PASSWORD` later will not automatically update SQL Server. Either keep the original password, change the password inside SQL Server, or recreate the Docker volume if you are willing to lose local data.

## Full Docker Mode

Use this when you want the API, SQL Server, and React client running through Docker Compose.

```powershell
docker compose --profile frontend up --build
```

This starts:

- React client: `http://localhost:56772`
- API: `http://localhost:5089`
- SQL Server: `localhost,14333`

The client container is optional. Local `npm run dev` is still faster for day-to-day frontend work.

## Database Migrations

Apply EF Core migrations to the Docker SQL Server with:

```powershell
dotnet ef database update --project src/InsightVault.Infrastructure --startup-project src/InsightVault.Api --connection "Server=localhost,14333;Database=InsightVault;User Id=sa;Password=InsightVault-Local-Only-Password-123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Use your actual `INSIGHTVAULT_SQL_PASSWORD` if you changed it before the SQL Server volume was created.

## Health Check

The API exposes a basic health endpoint:

```text
GET http://localhost:5089/health
```

This is useful for local smoke testing, Docker checks, and future deployment validation.

## Terraform Import Mode

Terraform lives in `infra/terraform`.

Use import mode when you already created low-cost Azure resources through the portal and want Terraform to represent them:

- Resource group
- Storage account
- Azure AI Foundry / Azure OpenAI resource
- Storage container

Typical flow:

```powershell
cd infra/terraform
Copy-Item dev.tfvars.example dev.tfvars
Copy-Item imports.tf.example imports.tf
terraform init
terraform plan -var-file="dev.tfvars"
terraform apply -var-file="dev.tfvars"
```

Do not commit:

- `dev.tfvars`
- `imports.tf`
- `.terraform/`
- `terraform.tfstate`
- `terraform.tfstate.*`

## Optional Paid Hosting

The Terraform scaffold includes optional paid hosting resources behind `enable_paid_hosting`.

Do not enable paid hosting unless you are comfortable with Azure charges.

Optional paid resources include:

- Azure App Service Plan
- Azure Linux Web App
- Azure SQL Database
- Static website hosting
- Application Insights
- Log Analytics

For this portfolio project, the recommended approach is local-first:

- run the app locally
- use Azure Blob Storage and Azure OpenAI only when needed
- keep CI validating the project
- document the production path without paying for always-on hosting

## Future Production Hardening

Do these only when they provide clear value:

- Azure Key Vault for managed secrets
- Managed identities for Azure resource access
- Private networking
- Azure AI Search or another vector index
- Manual deployment workflow with `workflow_dispatch`
- Agent-based repo automation for real tasks such as PR review or issue triage
