namespace tesisproject.shared.Errors;

public static partial class ErrorMessages
{
    // Existing legacy text reused by the parallel services. No new business rules/codes.
    // Kept separate to preserve current ServiceResult messages during the migration.
    public static class UnifiedLegacy
    {
        public const string ObjectiveActivityProgressBelowPreviousVisit = "You cannot set progress below {0}% because that was achieved before this visit.";
        public const string CatalogCrudService_NoItemsFoundMessage = "No items found for this catalog.";
        public const string CatalogQueryService_MsgNoItemsFound = "No items found.";
        public const string FacultyScopeService_FacultyScopeIdRequiredMessage = "FacultyScopeId is required.";
        public const string FacultyScopeService_FacultyScopeNotFoundMessage = "Faculty scope not found.";
        public const string FacultyScopeService_RequestRequiredMessage = "Request is required.";
        public const string FacultyScopeService_NameRequiredMessage = "Name is required.";
        public const string FacultyScopeService_ScopeNameAlreadyExistsMessage = "A scope with the same name already exists.";
        public const string FacultyScopeService_IdentityUserIdRequiredMessage = "IdentityUserId is required.";
        public const string FacultyScopeService_AssignmentNotFoundMessage = "Assignment not found (already unassigned).";
        public const string FacultyScopeService_UserNotFoundMessage = "User not found.";
        public const string FacultyScopeService_UserNotAuthenticatedMessage = "User not authenticated.";
        public const string GroupService_GroupNotFoundMessage = "Group not found.";
        public const string GroupService_NoGroupsFoundMessage = "No groups found.";
        public const string GroupService_ProjectNotFoundOrNoAssociatedGroupsMessage = "Project not found or it has no associated groups.";
        public const string GroupService_NoExternalUsersFoundMessage = "No external users found.";
        public const string GroupService_UnexpectedErrorMessage = "Unexpected error.";
        public const string GroupService_FailedLoadingExternalPeriodsDistributivosMessage = "Failed loading external periods/distributivos.";
        public const string GroupService_EmailIsRequiredMessage = "Email is required.";
        public const string GroupService_ExternalUserNotFoundMessage = "External user not found.";
        public const string GroupService_GroupNameRequiredMessage = "Group name is required.";
        public const string GroupService_GroupNameAlreadyExistsMessage = "A group with the same name already exists.";
        public const string GroupService_GroupMemberNotFoundMessage = "Group member not found.";
        public const string GroupService_NoAcademicPeriodsFoundMessage = "No academic periods found.";
        public const string IndexingSourceService_InvalidIdMessage = "Invalid id.";
        public const string IndexingSourceService_InvalidRequestMessage = "Invalid request.";
        public const string IndexingSourceService_NameAlreadyExistsMessage = "Name already exists.";
        public const string IndexingSourceService_IndexingSourceNotFoundMessage = "Indexing source not found.";
        public const string InstitutionService_InstitutionNotFoundMessage = "Institution not found.";
        public const string MatrixExcelExportService_EmptyPlaceholder = "-";
        public const string MemberRoleTypeService_NotFoundMessage = "MemberRoleType not found.";
        public const string ObjectiveActivityService_ObjectiveIdRequiredMessage = "ObjectiveId is required.";
        public const string ObjectiveActivityService_ProjectObjectiveNotFoundMessage = "ProjectObjective not found.";
        public const string ObjectiveActivityService_ObjectiveActivityNotFoundMessage = "ObjectiveActivity not found.";
        public const string ObjectiveActivityService_InvalidVisitIdMessage = "Invalid visitId.";
        public const string ObjectiveActivityService_InvalidActivityIdMessage = "Invalid activityId.";
        public const string ObjectiveActivityService_ProgressRangeMessage = "Progress must be between 0 and 100.";
        public const string ObjectiveActivityService_VisitNotFoundMessage = "Visit not found.";
        public const string ObjectiveActivityService_ActivityResultMaxLengthMessage = "ActivityResult cannot exceed 1000 characters.";
        public const string ObjectiveActivityService_ActionTextMaxLengthMessage = "ActionText cannot exceed 1000 characters.";
        public const string ProductAttributeDefinitionService_InvalidProductTypeIdMessage = "Invalid product type id.";
        public const string ProductAttributeDefinitionService_DefinitionNotFoundMessage = "Definition not found.";
        public const string ProductAttributeDefinitionService_ProductTypeAndAttributeRequiredMessage = "ProductTypeId and ProductAttributeId are required.";
        public const string ProductAttributeDefinitionService_AttributeAlreadyAssignedMessage = "This attribute is already assigned to the selected product type.";
        public const string ProductAttributeService_NotFoundMessage = "ProductAttribute not found.";
        public const string ProductAttributeService_LockedCannotModifyMessage = "This attribute is locked and cannot be modified.";
        public const string ProductAttributeService_LockedCannotDeleteMessage = "This attribute is locked and cannot be deleted.";
        public const string ProjectExtensionService_ProjectIdRequiredMessage = "ProjectId is required.";
        public const string ProjectExtensionService_ProjectExtensionTypeIdRequiredMessage = "ProjectExtensionTypeId is required.";
        public const string ProjectExtensionService_ProjectNotFoundMessage = "Project not found.";
        public const string ProjectExtensionService_ProjectExtensionTypeNotFoundMessage = "ProjectExtensionType not found.";
        public const string ProjectExtensionService_ProjectExtensionNotFoundMessage = "ProjectExtension not found.";
        public const string ProjectExtensionService_ProjectIdRequiredLowercaseMessage = "projectId is required.";
        public const string ProjectObjectiveService_ObjectiveTypeIdRequiredMessage = "ObjectiveTypeId is required.";
        public const string ProjectObjectiveService_ObjectiveRequiredMessage = "Objective is required.";
        public const string ProjectObjectiveService_ResultRequiredMessage = "Result is required.";
        public const string ProjectObjectiveService_ObjectiveTypeInvalidOrInactiveMessage = "ObjectiveType is invalid or inactive.";
        public const string ProjectResearchCategoryService_NotFoundMessage = "Not found.";
        public const string ProjectResearchCategoryService_InvalidDataMessage = "Invalid data.";
        public const string ResearchCategoryTypeService_MsgItemNotFound = "Item not found";
        public const string ResearchCategoryTypeService_MsgNameAlreadyExists = "Name already exists";
        public const string VisitObjectiveActivityProgressService_MsgVisitIdRequired = "VisitId is required.";
        public const string VisitObjectiveActivityProgressService_MsgObjectiveActivityIdRequired = "ObjectiveActivityId is required.";
        public const string VisitObjectiveActivityProgressService_MsgObjectiveActivityInvalidForProjectVisit = "ObjectiveActivityId is invalid for this project/visit.";
        public const string VisitObjectiveActivityProgressService_MsgIdRequired = "id is required.";
        public const string VisitObjectiveActivityProgressService_MsgProgressRecordNotFound = "Progress record not found.";
    }
}
