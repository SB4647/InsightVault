using InsightVault.Application.Features.Documents;
using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InsightVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetDocuments(
        CancellationToken cancellationToken)
    {
        var documents = await documentService.GetDocumentsAsync(cancellationToken);
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
                stream),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDocuments),
            new { id = document.Id },
            document);
    }
}
