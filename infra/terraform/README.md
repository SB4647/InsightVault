# InsightVault Terraform

This folder contains a Terraform scaffold for InsightVault.

By default, it runs in low-cost mode and manages/imports only the existing resources:

- Resource group, matching the existing `InsightVault-RG`
- Storage account, matching the existing `insightvaultblobs`
- Azure AI Foundry / AI Services resource, matching the existing `insightvault-ai-resource`

Paid cloud hosting resources are behind `enable_paid_hosting = true`:

- Azure App Service Plan
- Azure Linux Web App for the ASP.NET Core API
- Azure SQL Server and database
- Storage account with static website hosting for the React frontend
- Log Analytics workspace
- Application Insights

The default is intentionally low-cost for development and portfolio work. It shows the production path without creating always-on paid infrastructure.

## Usage

Copy the example variables file:

```bash
cp dev.tfvars.example dev.tfvars
```

Edit `dev.tfvars` with real values. Do not commit `dev.tfvars`.

For your existing Azure resources, keep these names exact:

```hcl
resource_group_name            = "InsightVault-RG"
documents_storage_account_name = "insightvaultblobs"
azure_ai_foundry_name          = "insightvault-ai-resource"
```

Initialize and preview:

```bash
terraform init
terraform plan -var-file="dev.tfvars"
```

Apply only after confirming the plan has `0 to destroy`:

```bash
terraform apply -var-file="dev.tfvars"
```

## Import Existing Azure Resources

The Azure resources shown in the portal already exist. Terraform needs to import them into state before it can manage them safely.

Copy the import template:

```bash
cp imports.tf.example imports.tf
```

Replace `<subscription-id>` in `imports.tf` with your Azure subscription ID.

Then run:

```bash
terraform plan -var-file="dev.tfvars"
terraform apply -var-file="dev.tfvars"
```

The import blocks cover:

- `InsightVault-RG`
- `insightvaultblobs`
- `insightvault-ai-resource`

With `enable_paid_hosting = false`, the first successful apply should import/manage these existing resources and should not create App Service, Azure SQL, frontend hosting, Log Analytics, or Application Insights.

## Enable Paid Hosting Later

When you want a full cloud deployment and accept the monthly cost, set this in `dev.tfvars`:

```hcl
enable_paid_hosting = true
```

Then run:

```bash
terraform plan -var-file="dev.tfvars"
```

Do not apply unless the plan still says `0 to destroy`.

## After Apply

1. Deploy the API to the App Service.
2. Run EF Core migrations against the Azure SQL connection string.
3. Build the React app with `VITE_API_BASE_URL` set to the `api_url` output.
4. Upload the React `dist` files to the frontend storage account static website container.
5. Add the frontend static website URL to CORS configuration and apply again.

## Security Notes

This scaffold keeps the first cloud deployment simple. Sensitive values passed through Terraform variables can still appear in Terraform state. For a more production-like setup, move secrets to Key Vault and use managed identity references.
