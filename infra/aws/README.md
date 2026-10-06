# InsightVault AWS Terraform Foundation

This module demonstrates the AWS provider, region, default tagging, and Terraform validation workflow. It intentionally contains no resources, remote state, `plan`, or `apply` automation.

Run these local checks:

```powershell
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws init -backend=false
terraform -chdir=infra/aws validate
```

These commands do not create AWS resources. Future manually authorised work can use the local IAM Identity Center profile with `$env:AWS_PROFILE="insightvault-dev"`; application code must use workload roles when deployed and never static access keys.
