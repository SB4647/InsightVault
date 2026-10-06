# InsightVault AWS Terraform

This module declares the private S3 document-storage design while deliberately containing no remote state, `plan`, or `apply` automation. The checked-in configuration does not create an AWS resource by itself.

The S3 design includes:

- a private bucket with all public-access controls blocked
- bucket-owner-enforced object ownership
- S3-managed server-side encryption (`AES256`), avoiding a customer-managed KMS key
- a seven-day incomplete-upload cleanup rule without versioning, so application deletes remove the object rather than leaving billed historical versions
- a bucket policy that denies non-TLS requests
- `force_destroy = false`, so Terraform will not silently delete stored documents

Application workload permissions are intentionally not included yet. A later deployment milestone will add a least-privilege workload role; it must not use a developer SSO profile or static access keys.

Run these local checks:

```powershell
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws init -backend=false
terraform -chdir=infra/aws validate
```

These commands do not create AWS resources. Do not run `terraform plan` or `terraform apply` until a separate deployment step is explicitly approved. Future manual Terraform work can use the local IAM Identity Center profile with `$env:AWS_PROFILE="insightvault-dev"`; deployed application code must use workload roles and never static access keys.

`documents_bucket_name` is deliberately required and has no default. It must be supplied only during that future, approved deployment with a globally unique name; `terraform validate` does not need a value.
