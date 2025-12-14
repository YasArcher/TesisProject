using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Matrices.Response
{
    public class ProjectFlatReportDTO
    {
        // =========================
        //      PROJECT GRAIN
        // =========================
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int ProjectNumber { get; set; }

        public int ProjectTypeId { get; set; }
        public string? ProjectTypeName { get; set; }

        public int ProjectStateId { get; set; }
        public string? ProjectStateName { get; set; }

        public int ProjectGroupId { get; set; }
        public string? GroupName { get; set; }
        public string? GroupTypeName { get; set; }

        public int ConvocationId { get; set; }
        public string? ConvocationName { get; set; }

        public DateTime? ApprovalDate { get; set; }
        public DateTime? StartDate { get; set; }
        public int DurationInMonths { get; set; }
        public DateTime? TentativeEndDate { get; set; }
        public DateTime? RealEndDate { get; set; }
        public decimal? ExecutionPercentage { get; set; }

        // FacultyId se resuelve externamente vía API si hace falta
        public int FacultyId { get; set; }

        // =========================
        //      DETAIL LISTS
        // =========================
        public List<ProjectBudgetReportDTO> Budgets { get; set; } = new();
        public List<ProjectProductReportDTO> Products { get; set; } = new();
        public List<ProjectObjectiveReportDTO> Objectives { get; set; } = new();
        public List<ProjectResearchCategoryReportDTO> ResearchCategories { get; set; } = new();
        public List<ProjectExternalResearcherReportDTO> ExternalResearchers { get; set; } = new();
        public List<ProjectVisitReportDTO> Visits { get; set; } = new();
        public string? FacultyName { get; set; }
        // =========================
        //     PROJECT MEMBERS
        // =========================

        /// <summary>
        /// Todos los integrantes internos del proyecto
        /// (grupo tipo 1) enriquecidos con datos del directorio externo.
        /// </summary>
        public List<ProjectMemberReportDTO> InternalMembers { get; set; } = new();

        /// <summary>
        /// Subconjunto de InternalMembers que además pertenecen
        /// a grupos tipo 2 (investigadores acreditados SENESCYT).
        /// </summary>
        public List<ProjectMemberReportDTO> SenescytMembers { get; set; } = new();

        /// <summary>
        /// Coordinador principal o subrogante del proyecto
        /// (resuelto a partir de InternalMembers).
        /// </summary>
        public string? CoordinatorName { get; set; }
        public string? CoordinatorEmail { get; set; }
        public string? CoordinatorPhone { get; set; }
    }
    /// <summary>
    /// Integrante del proyecto enriquecido con datos del directorio externo.
    /// </summary>
    public class ProjectMemberReportDTO
    {
        public int UserId { get; set; }

        public int MemberRoleId { get; set; }
        public string? MemberRoleName { get; set; }

        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }


    // ============================================
    //               BUDGETS
    // ============================================
    public class ProjectBudgetReportDTO
    {
        public int BudgetId { get; set; }
        public int FundingTypeId { get; set; }
        public string? FundingTypeName { get; set; }

        public decimal InitialAmount { get; set; }
        public decimal CertifiedAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public List<BudgetTransactionReportDTO> Transactions { get; set; } = new();
    }

    public class BudgetTransactionReportDTO
    {
        public int BudgetTransactionId { get; set; }
        public int TransactionTypeId { get; set; }
        public string? TransactionTypeName { get; set; }

        public decimal CertifiedAmount { get; set; }
        public decimal? ExecutedAmount { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CertifiedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public string BudgetItem { get; set; } = string.Empty;
        public string? CURNumber { get; set; }
        public string CertificationDescription { get; set; } = string.Empty;
        public string? ExecutionDescription { get; set; }
    }

    // ============================================
    //               PRODUCTS
    // ============================================
    public class ProjectProductReportDTO
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int ProductTypeId { get; set; }
        public string? ProductTypeName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ProductAttributeValueDTO> Attributes { get; set; } = new();
    }

    public class ProductAttributeValueDTO
    {
        public int AttributeDefinitionId { get; set; }
        public int ProductAttributeId { get; set; }

        public string? AttributeName { get; set; }
        public string? Value { get; set; }
        public string? Unit { get; set; }

        /// <summary>
        /// DataType como texto (por ejemplo el valor del enum o del entero).
        /// </summary>
        public string? DataType { get; set; }
    }

    // ============================================
    //           OBJECTIVES & ACTIVITIES
    // ============================================
    public class ProjectObjectiveReportDTO
    {
        public int ObjectiveId { get; set; }
        public int ObjectiveTypeId { get; set; }
        public string? ObjectiveTypeName { get; set; }

        public string Objective { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int WeightedPercentage { get; set; }

        public List<ObjectiveActivityReportDTO> Activities { get; set; } = new();
    }

    public class ObjectiveActivityReportDTO
    {
        public int ObjectiveActivityId { get; set; }
        public string ActivityResult { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ============================================
    //         RESEARCH CATEGORIES (Dom/Linea)
    // ============================================
    public class ProjectResearchCategoryReportDTO
    {
        public int ResearchCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public int ResearchCategoryTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;

        public int ResearchCategoryGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }

    // ============================================
    //          EXTERNAL RESEARCHERS
    // ============================================
    public class ProjectExternalResearcherReportDTO
    {
        public int ExternalResearcherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public int? InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExitDate { get; set; }
    }

    // ============================================
    //                VISITS
    // ============================================
    public class ProjectVisitReportDTO
    {
        public int VisitId { get; set; }

        public DateTime? ScheduledDate { get; set; }
        public DateTime? PerformedDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public int VisitStateId { get; set; }
        public string? VisitStateName { get; set; }
    }
}