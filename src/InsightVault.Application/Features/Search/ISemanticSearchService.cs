using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;

namespace InsightVault.Application.Features.Search;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        SearchDocumentsQuery query,
        CancellationToken cancellationToken = default);
}
