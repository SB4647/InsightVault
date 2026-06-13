using InsightVault.Api.Auth;
using InsightVault.Application.Features.Documents;
using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;
using InsightVault.Application.Features.Documents.Processing;
using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.Features.Documents.Processing.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsightVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DocumentsController(
    IDocumentService documentService,
    IDocumentProcessingService documentProcessingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetDocuments(
        CancellationToken cancellationToken)
    {
        var documents = await documentService.GetDocumentsAsync(
            User.GetRequiredUserId(),
            cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> UploadDocument(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return BadRequest("A non-empty file is required.");
        }

        await using var stream = file.OpenReadStream();
        var document = await documentService.UploadAsync(
            new UploadDocumentCommand(
                file.FileName,
                file.ContentType,
                file.Length,
                stream,
                User.GetRequiredUserId()),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDocuments),
            new { id = document.Id },
            document);
    }

    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(DocumentProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentProcessingResultDto>> ProcessDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await documentProcessingService.ProcessAsync(
                new ProcessDocumentCommand(id, User.GetRequiredUserId()),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(DocumentShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentShareDto>> ShareDocument(
        Guid id,
        ShareDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        try
        {
            var result = await documentService.ShareDocumentAsync(
                new ShareDocumentCommand(id, User.GetRequiredUserId(), request.Email),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
    }

    public sealed record ShareDocumentRequest(string Email);
}
