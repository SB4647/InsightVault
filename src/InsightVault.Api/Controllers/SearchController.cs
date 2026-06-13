using InsightVault.Api.Auth;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsightVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SearchController(ISemanticSearchService semanticSearchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await semanticSearchService.SearchAsync(
                new SearchDocumentsQuery(query, User.GetRequiredUserId(), maxResults),
                cancellationToken);

            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
