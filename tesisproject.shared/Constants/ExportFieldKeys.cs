using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Constants
{
    /// <summary>
    /// Claves estándar soportadas por el motor de exportación (FieldKey).
    /// Evita "magic strings" en el switch y habilita refactor seguro.
    /// </summary>
    public static class ExportFieldKeys
    {
        // =========================
        //      PROJECT GRAIN
        // =========================
        public const string ProjectId = "PROJECT_ID";
        public const string ProjectCode = "PROJECT_CODE";
        public const string ProjectName = "PROJECT_NAME";
        public const string ProjectNumber = "PROJECT_NUMBER";

        public const string ProjectTypeId = "PROJECT_TYPE_ID";
        public const string ProjectTypeName = "PROJECT_TYPE_NAME";

        public const string ProjectStateId = "PROJECT_STATE_ID";
        public const string ProjectStateName = "PROJECT_STATE_NAME";

        public const string ProjectGroupId = "PROJECT_GROUP_ID";
        public const string GroupName = "GROUP_NAME";
        public const string GroupTypeName = "GROUP_TYPE_NAME";

        public const string ConvocationId = "CONVOCATION_ID";
        public const string ConvocationName = "CONVOCATION_NAME";

        public const string ApprovalDate = "APPROVAL_DATE";
        public const string StartDate = "START_DATE";
        public const string DurationMonths = "DURATION_MONTHS";
        public const string TentativeEndDate = "TENTATIVE_END_DATE";
        public const string RealEndDate = "REAL_END_DATE";
        public const string ExecutionPercentage = "EXECUTION_PERCENTAGE";

        public const string FacultyId = "FACULTY_ID";
        public const string FacultyName = "FACULTY_NAME";

        // =========================
        //  COORDINADOR / DIRECTOR
        // =========================
        public const string CoordinatorName = "COORDINATOR_NAME";
        public const string CoordinatorEmail = "COORDINATOR_EMAIL";
        public const string CoordinatorPhone = "COORDINATOR_PHONE";

        // =========================
        //        BUDGETS
        // =========================
        public const string BudgetInitialSummary = "BUDGET_INITIAL_SUMMARY";
        public const string BudgetCertifiedSummary = "BUDGET_CERTIFIED_SUMMARY";
        public const string BudgetExecutedSummary = "BUDGET_EXECUTED_SUMMARY";
        public const string BudgetFundingTypes = "BUDGET_FUNDING_TYPES";

        // =========================
        //        PRODUCTS
        // =========================
        public const string ProductTitles = "PRODUCT_TITLES";
        public const string ProductTypes = "PRODUCT_TYPES";

        // =========================
        //  INVESTIGADORES EXTERNOS
        // =========================
        public const string ExternalResearcherNames = "EXTERNAL_RESEARCHER_NAMES";
        public const string ExternalResearcherInstitutions = "EXTERNAL_RESEARCHER_INSTITUTIONS";

        // =========================
        //  INVESTIGADORES SENESCYT
        // =========================
        public const string SenescytMemberNames = "SENESCYT_MEMBER_NAMES";

        // =========================
        //     BUNDLES ESPECIALES
        // =========================
        public const string CasesObjetivos = "CASES_OBJETIVOS";
        public const string CasesResearchCategories = "CASES_RESEARCH_CATEGORIES";
    }
}