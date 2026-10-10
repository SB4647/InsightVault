# Step 7 Security and Cost Controls Design

## Goal

Protect InsightVault's API and AI workflow from abusive requests and obvious prompt-injection attempts, while documenting a least-privilege AWS deployment design that creates no cloud resources by default.

## Decisions

- The API applies named, partitioned rate-limit policies: authentication by IP address, and uploads, search, and chat by authenticated user ID.
- Uploads are limited per owner by document count and stored-byte total. Defaults are 100 documents and 500 MB, configurable under `UploadQuota`.
- Questions are capped and checked for a small set of high-confidence prompt-injection phrases. Retrieved document excerpts remain usable but are explicitly labelled untrusted in the Azure OpenAI system/user prompt boundary.
- Request logging records only trace ID, HTTP method, endpoint, status, duration, and route document ID. It never reads request or response bodies, headers, credentials, questions, or document text. Worker failures log the job document ID and exception type only.
- Terraform declares separately scoped API and worker task roles behind `enable_workload_iam_roles = false`. It does not create Secrets Manager secrets; optional existing secret ARNs only grant `GetSecretValue` to the relevant role.

## Non-goals

- No Terraform apply, AWS deployment, AWS secret, ECS service, database, or IAM role will be created.
- No answer-history screen, malware scanning, WAF, or full data-loss-prevention system is included.
- Prompt-injection matching is a defence-in-depth control, not a guarantee that an LLM can never return unsafe text.

## Verification

- Unit tests cover quota rejection, blocked question patterns, and the untrusted-document prompt boundary.
- API integration tests cover rate-limit responses and safe request logging fields.
- Terraform formatting and validation run without deployment.
- The existing full backend suite and client build remain green.
