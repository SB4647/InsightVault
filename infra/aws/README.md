# InsightVault AWS production design

This Terraform module defines the AWS production shape for InsightVault. It is **not deployed**: `enable_production_infrastructure` is `false` in every committed environment example, so validation creates no AWS resources and incurs no AWS cost.

## What a future approved deployment creates

- A two-AZ VPC with public ALB subnets, private application subnets, and isolated database subnets.
- A public Application Load Balancer that redirects HTTP to HTTPS and uses an **existing** ACM certificate. DNS and certificate requests are deliberately outside this module.
- Private ECS/Fargate API and worker services, ECR repositories with image scanning and a ten-image lifecycle rule, and 30-day CloudWatch log groups.
- A private S3 document bucket: public access blocked, bucket-owner enforced, S3-managed encryption, TLS-only policy, and incomplete-upload cleanup.
- An SQS document-processing queue and DLQ (three attempts, 20-second long polling, 15-minute visibility timeout).
- A private, encrypted PostgreSQL RDS instance with backups. It uses the AWS-owned storage key and RDS manages its master password in Secrets Manager. Terraform does not run `pgvector` setup or EF migrations.
- Separate runtime roles: the API may manage document objects and submit jobs; the worker may read objects and process jobs. Neither receives administrator permissions.

The API trusts forwarded headers only from the VPC CIDR and processes them before rate limiting. This lets the load balancer report the real client address while rejecting forged public `X-Forwarded-*` headers.

## Cost and deployment guardrails

`terraform init -backend=false`, `fmt`, and `validate` are local-only checks. Never run `apply` merely to demonstrate AWS knowledge.

```powershell
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws init -backend=false
terraform -chdir=infra/aws validate
```

An actual deployment can incur charges for ALB, Fargate, RDS, S3, SQS, CloudWatch logs, Secrets Manager, ECR storage, and public IPv4 addresses. A NAT Gateway is particularly costly; `enable_nat_gateway` stays false by default. A real private workload normally needs approved outbound access to Azure OpenAI, through a NAT Gateway, private endpoint, or an organisation-approved egress proxy.

Do not use a customer-managed KMS key for this portfolio setup unless there is a specific compliance need. S3, SQS, and RDS use AWS-managed/AWS-owned encryption here to avoid a recurring KMS key charge.

## Environments and secrets

Committed examples live in [environments](environments): `development`, `staging`, and `production`. Copy an example to an ignored `.tfvars` file only for a reviewed deployment. Leave the master switch false during local work.

The module never creates or accepts secret values. `api_task_secrets` and `worker_task_secrets` map application configuration keys (for example, `ConnectionStrings__DefaultConnection`) to exact existing Secrets Manager ARNs. ECS retrieves those values at task start; the application uses its task role, never static keys or your local SSO profile.

Use immutable ECR image digests, an existing ACM certificate ARN, a globally unique bucket name, and exact secret ARNs before a real plan. Run database migrations as a separate release operation after RDS is reachable; do not put migration credentials or commands in Terraform.

## Manual GitHub deployment gate

[aws-deploy.yml](../../.github/workflows/aws-deploy.yml) can only start through **Run workflow**. It first creates a saved plan using a read-only OIDC role in an unprotected `plan-development`, `plan-staging`, or `plan-production` Environment. For an `apply` run, the selected protected GitHub Environment then pauses for its required human reviewers and applies that exact saved plan—no long-lived AWS key in GitHub.

Before ever using `plan` or `apply`, create the three unprotected `plan-*` Environments and the three protected deployment Environments in repository settings. Each `plan-*` Environment needs `AWS_PLAN_ROLE_ARN`, `AWS_TF_STATE_BUCKET`, and `AWS_TF_STATE_LOCK_TABLE` variables plus an `AWS_TFVARS_JSON` secret containing the reviewed, non-secret Terraform configuration. Each protected deployment Environment needs `AWS_DEPLOY_ROLE_ARN`, `AWS_TF_STATE_BUCKET`, and `AWS_TF_STATE_LOCK_TABLE`; add required reviewers, especially for `production`. The workflow writes configuration only on its ephemeral runner; it must contain exact secret ARNs, never secret values. The deployment roles and remote-state bucket/table are intentionally manual infrastructure-administration tasks and are not created by this module.
