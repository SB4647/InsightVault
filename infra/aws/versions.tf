terraform {
  required_version = ">= 1.15.0"

  # Values are supplied only by the protected GitHub deployment workflow.
  backend "s3" {}

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}
