namespace InsightVault.Application.Features.Documents.Commands;

public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    long SizeInBytes,
    Stream Content);
