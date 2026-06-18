// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System;
using System.Collections.Generic;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.shared.DTOs.MassRegistration
{
    public class RegistrationMatrixSummaryDto
    {
        public int RegistrationMatrixId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EntityName { get; set; } = "Article";
        public string Status { get; set; } = string.Empty;
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
        public int? LastImportBatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RegistrationMatrixColumnDto
    {
        public int RegistrationMatrixColumnId { get; set; }
        public int FieldId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsDynamic { get; set; }
        public int DisplayOrder { get; set; }
        public int WidthUnits { get; set; } = 1;
    }

    public class RegistrationMatrixCellDto
    {
        public int FieldId { get; set; }
        public string? RawValue { get; set; }
    }

    public class RegistrationMatrixRowDto
    {
        public int RegistrationMatrixRowId { get; set; }
        public int RowNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<RegistrationMatrixCellDto> Cells { get; set; } = new();
    }

    public class RegistrationMatrixDetailDto
    {
        public RegistrationMatrixSummaryDto Summary { get; set; } = new();
        public string? Notes { get; set; }
        public List<RegistrationMatrixColumnDto> Columns { get; set; } = new();
        public List<RegistrationMatrixRowDto> Rows { get; set; } = new();
    }

    public class CreateRegistrationMatrixRequest
    {
        public string Name { get; set; } = string.Empty;
        public string EntityName { get; set; } = "Article";
        public string? Notes { get; set; }
        public List<int> FieldIds { get; set; } = new();
    }

    public class UpdateRegistrationMatrixRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft";
    }

    public class AddRegistrationMatrixColumnsRequest
    {
        public List<int> FieldIds { get; set; } = new();
    }

    public class UpdateRegistrationMatrixColumnOrderRequest
    {
        public int DisplayOrder { get; set; }
        public int WidthUnits { get; set; } = 1;
    }

    public class UpdateRegistrationMatrixCellRequest
    {
        public int FieldId { get; set; }
        public string? RawValue { get; set; }
    }

    public class SubmitRegistrationMatrixRequest
    {
        public bool ValidateAfterCreate { get; set; } = true;
        public bool UseAuthorWorkflow { get; set; }
        public List<RegistrationMatrixRowParticipantDto> RowParticipants { get; set; } = new();
    }

    public class RegistrationMatrixRowParticipantDto
    {
        public int RegistrationMatrixRowId { get; set; }
        public List<RegistrationMatrixParticipantDto> Participants { get; set; } = new();
    }

    public class RegistrationMatrixParticipantDto
    {
        public int Index { get; set; }
        public List<RegistrationMatrixCellDto> Cells { get; set; } = new();
    }

    public class RegistrationMatrixSubmissionResultDto
    {
        public string Message { get; set; } = string.Empty;
        public RegistrationMatrixDetailDto Matrix { get; set; } = new();
        public BulkImportActionResultDto? BatchResult { get; set; }
    }

    public class RegistrationMatrixDeleteResultDto
    {
        public bool Deleted { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

