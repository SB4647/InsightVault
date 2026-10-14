#!/bin/sh
set -eu

awslocal sqs create-queue --queue-name insightvault-document-processing-dlq >/dev/null
DLQ_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/insightvault-document-processing-dlq \
  --attribute-names QueueArn \
  --query 'Attributes.QueueArn' \
  --output text)

awslocal sqs create-queue \
  --queue-name insightvault-document-processing \
  --attributes "RedrivePolicy={\"deadLetterTargetArn\":\"${DLQ_ARN}\",\"maxReceiveCount\":\"3\"}" >/dev/null

