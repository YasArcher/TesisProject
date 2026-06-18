using System;
using System.Collections.Generic;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.shared.DTOs.Imports
{
    public class BulkImportTemplateRequest
    {
        public string EntityName { get; set; } = "Article";
        public string SourceType { get; set; } = "Excel";
        public string? TemplateName { get; set; }
        public bool UseActiveFormsWhenEmpty { get; set; } = true;
        public List<int> ArticleFieldIds { get; set; } = new();
        public List<int> ParticipantFieldIds { get; set; } = new();
    }

    public class BulkImportTemplateFieldDto
    {
        public int FieldId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string HeaderKey { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsDynamic { get; set; }
        public string? HelpText { get; set; }
        public string? Placeholder { get; set; }
        public string? ReferenceTableName { get; set; }
    }

    public class BulkImportTemplateDescriptorDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string EntityName { get; set; } = "Article";
        public string SourceType { get; set; } = "Excel";
        public List<BulkImportTemplateFieldDto> ArticleFields { get; set; } = new();
        public List<BulkImportTemplateFieldDto> ParticipantFields { get; set; } = new();
    }

    public class BulkImportBatchSummaryDto
    {
        public int ImportBatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? SourceReference { get; set; }
        public int TotalRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int ErrorRows { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? Notes { get; set; }
        public int PendingRows { get; set; }
        public int ValidRows { get; set; }
        public int ProcessedRows { get; set; }
    }

    public class BulkImportErrorDto
    {
        public int ImportBatchErrorId { get; set; }
        public int? ImportBatchRowId { get; set; }
        public int? FieldId { get; set; }
        public string? FieldKey { get; set; }
        public string? FieldLabel { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BulkImportRowCellDto
    {
        public int ImportBatchRowValueId { get; set; }
        public int FieldId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string? RawValue { get; set; }
        public string? NormalizedValue { get; set; }
        public string? ValueType { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
    }

    public class BulkImportRowPreviewDto
    {
        public int ImportBatchRowId { get; set; }
        public int RowNumber { get; set; }
        public string RowStatus { get; set; } = string.Empty;
        public int? TargetArticleId { get; set; }
        public int? TargetParticipantId { get; set; }
        public string? RawJson { get; set; }
        public List<BulkImportRowCellDto> Cells { get; set; } = new();
        public List<BulkImportErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportBatchDetailDto
    {
        public BulkImportBatchSummaryDto Summary { get; set; } = new();
        public BulkImportTemplateDescriptorDto? Template { get; set; }
        public List<BulkImportRowPreviewDto> Rows { get; set; } = new();
        public List<BulkImportErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportActionResultDto
    {
        public string Message { get; set; } = string.Empty;
        public BulkImportBatchDetailDto Batch { get; set; } = new();
        public int InsertedRows { get; set; }
        public int DuplicateRows { get; set; }
        public int FailedRows { get; set; }
    }

    public class BulkImportRowCorrectionCellDto
    {
        public int FieldId { get; set; }
        public string? RawValue { get; set; }
    }

    public class BulkImportRowCorrectionRequest
    {
        public bool RevalidateBatch { get; set; } = true;
        public List<BulkImportRowCorrectionCellDto> Cells { get; set; } = new();
    }

    public class ExternalArticleImportRequest
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool ValidateAfterCreate { get; set; } = true;
        public ExternalArticlePreviewDto Article { get; set; } = new();
    }

    public class ExternalArticlesImportRequest
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool ValidateAfterCreate { get; set; } = true;
        public List<ExternalArticlePreviewDto> Articles { get; set; } = new();
    }
}
