# InsightVault AWS Terraform

This module declares the private S3 document-storage design while deliberately containing no remote state, `plan`, or `apply` automation. The checked-in configuration does not create an AWS resource by itself.

It also defines an opt-in SQS document-processing design: a standard processing queue, a private DLQ after three deliveries, 20-second long polling, a 15-minute visibility timeout, and SQS-managed encryption. `enable_document_processing_queues` defaults to `false`.

The S3 design includes:

- a private bucket with all public-access controls blocked
- bucket-owner-enforced object ownership
- S3-managed server-side encryption (`AES256`), avoiding a customer-managed KMS key
- a seven-day incomplete-upload cleanup rule without versioning, so application deletes remove the object rather than leaving billed historical versions
- a bucket policy that denies non-TLS requests
- `force_destroy = false`, so Terraform will not silently delete stored documents

## Future workload roles and secrets

`enable_workload_iam_roles` also defaults to `false`, and creates roles only when the SQS queues are explicitly enabled. It declares separate ECS task roles for a future deployment:

- The API can list/read/write/delete objects in this bucket and send jobs to the processing queue.
- The worker can read document objects and receive, acknowledge, or extend visibility for messages from that queue.

Neither role has administrator permissions, access to your `insightvault-dev` SSO profile, or permission to use another workload's queue operation.

`api_secret_arns` and `worker_secret_arns` accept only ARNs for **existing** Secrets Manager secrets. No `aws_secretsmanager_secret` resource exists in this module. Secrets Manager charges per stored secret, so leave both values empty during local work and add only exact secret ARNs for a deliberate deployment.

## Cost controls

- IAM roles and inline policies have no direct charge.
- S3 charges begin only after an approved apply creates a bucket and stores data or handles requests.
- Enabling the processing queue can incur SQS request charges; leaving `enable_document_processing_queues = false` avoids them.
- Referencing a secret does not create one; storing a secret in Secrets Manager is chargeable.
- `terraform fmt`, `init -backend=false`, and `validate` make no AWS resources or billable API calls.

Run these local checks:

```powershell
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws init -backend=false
terraform -chdir=infra/aws validate
```

These commands do not create AWS resources. Do not run `terraform plan` or `terraform apply` until a separate deployment step is explicitly approved. Future manual Terraform work can use the local IAM Identity Center profile with `$env:AWS_PROFILE="insightvault-dev"`; deployed application code must use workload roles and never static access keys.

`documents_bucket_name` is deliberately required and has no default. It must be supplied only during that future, approved deployment with a globally unique name; `terraform validate` does not need a value.
