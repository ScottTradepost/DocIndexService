using System.ComponentModel.DataAnnotations;
using DocIndexService.Application.Abstractions.SourceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocIndexService.Admin.Pages.Sources;

[Authorize(Policy = "CanManageSources")]
public sealed class IndexModel : PageModel
{
    private readonly IDocumentSourceService _documentSourceService;

    public IndexModel(IDocumentSourceService documentSourceService)
    {
        _documentSourceService = documentSourceService;
    }

    [BindProperty]
    public CreateSourceInput NewSource { get; set; } = new();

    public IReadOnlyList<DocumentSourceDto> Sources { get; private set; } = Array.Empty<DocumentSourceDto>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Sources = await _documentSourceService.ListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Sources = await _documentSourceService.ListAsync(cancellationToken);
            return Page();
        }

        var request = new CreateDocumentSourceRequest(
            NewSource.Name,
            NewSource.RootPath,
            NewSource.IsRecursive,
            NewSource.IncludePatterns,
            NewSource.ExcludePatterns,
            NewSource.ScanIntervalMinutes,
            NewSource.IsEnabled);

        var result = await _documentSourceService.CreateAsync(request, cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetEnabledAsync(Guid sourceId, bool isEnabled, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.SetEnabledAsync(sourceId, isEnabled, cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid sourceId,
        string name,
        string rootPath,
        string includePatterns,
        string? excludePatterns,
        int scanIntervalMinutes,
        bool isRecursive,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        var request = new UpdateDocumentSourceRequest(
            name,
            rootPath,
            isRecursive,
            includePatterns,
            excludePatterns ?? string.Empty,
            scanIntervalMinutes,
            isEnabled);

        var result = await _documentSourceService.UpdateAsync(sourceId, request, cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.DeleteAsync(sourceId, cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostScanNowAsync(Guid sourceId, bool fullReconciliation, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.TriggerManualScanAsync(
            new ManualScanRequest(sourceId, fullReconciliation),
            cancellationToken);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public sealed class CreateSourceInput
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string RootPath { get; set; } = string.Empty;

        public bool IsRecursive { get; set; } = true;

        [Required]
        [MaxLength(500)]
        public string IncludePatterns { get; set; } = "*";

        [MaxLength(500)]
        public string ExcludePatterns { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ScanIntervalMinutes { get; set; } = 15;

        public bool IsEnabled { get; set; } = true;
    }
}
