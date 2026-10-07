namespace InsightVault.Application.Interfaces;

/// <summary>
/// Represents source text extracted from one PDF page and its optional logical section heading.
/// </summary>
public sealed record ExtractedDocumentPage(int PageNumber, string? SectionTitle, string Text);
