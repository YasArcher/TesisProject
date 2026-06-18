using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Services.Implementations
{
    public class WorkflowService : IWorkflowService
    {
        public const string AuthorArticleSubmissionWorkflowKey = "AuthorArticleSubmission";

        private const string SubmittedStatus = "Submitted";
        private const string PendingStatus = "Pending";
        private const string ApprovedStatus = "Approved";
        private const string ProcessedStatus = "Processed";
        private const string AssignedStatus = "Assigned";
        private const string InReviewStatus = "InReview";
        private const string ReturnedStatus = "Returned";
        private const string CancelledStatus = "Cancelled";
        private const string AdminRole = "Admin";
        private readonly AppDbContext _db;

        public WorkflowService(AppDbContext db)
        {
            _db = db;
        }

        public async Task EnsureSeedDataAsync(CancellationToken ct = default)
        {
            await EnsureWorkflowStageDefinitionDefaultsAsync(ct);

            var now = DateTime.UtcNow;
            var definition = await _db.WorkflowDefinitions
                .Include(x => x.Stages)
                .FirstOrDefaultAsync(x => x.Key == AuthorArticleSubmissionWorkflowKey, ct);

            if (definition is null)
            {
                definition = new WorkflowDefinition
                {
                    Key = AuthorArticleSubmissionWorkflowKey,
                    Name = "Registro de artículos por autor",
                    EntityName = "Article",
                    Description = "Flujo de revisión multinivel para registros enviados por autores docentes antes del procesamiento final.",
                    IsActive = true,
                    CreatedAt = now
                };

                _db.WorkflowDefinitions.Add(definition);
                await _db.SaveChangesAsync(ct);
                await _db.Entry(definition).Collection(x => x.Stages).LoadAsync(ct);
            }

            EnsureStageDefinition(definition, "uodide-validation", "Validación UODIDE", 1, "uodide", "UODIDE", "WorkflowReviewerUodide", false, false, now);
            EnsureStageDefinition(definition, "technical-validation", "Validación técnica final", 2, "area-tecnica", "Área Técnica", "WorkflowReviewerAreaTecnica", true, true, now);

            await _db.SaveChangesAsync(ct);
        }

        public async Task<WorkflowInstance?> CreateWorkflowForBatchAsync(int importBatchId, string workflowKey, string? submittedByUserId, CancellationToken ct = default)
        {
            var existing = await _db.WorkflowInstances
                .Include(x => x.CurrentStageDefinition)
                .FirstOrDefaultAsync(x => x.ImportBatchId == importBatchId, ct);

            if (existing is not null)
            {
                return existing;
            }

            var definition = await _db.WorkflowDefinitions
                .Include(x => x.Stages.OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Key == workflowKey && x.IsActive, ct);

            if (definition is null)
            {
                return null;
            }

            var stages = definition.Stages
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            if (stages.Count == 0)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var firstStage = stages[0];
            var submittedBy = NormalizeReference(submittedByUserId);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = definition.WorkflowDefinitionId,
                ImportBatchId = importBatchId,
                Status = SubmittedStatus,
                CurrentStageDefinitionId = firstStage.WorkflowStageDefinitionId,
                SubmittedByUserId = submittedBy,
                SubmittedAt = now,
                LastActionAt = now
            };

            _db.WorkflowInstances.Add(instance);
            await _db.SaveChangesAsync(ct);

            var stageInstances = stages.Select(stage => new WorkflowStageInstance
            {
                WorkflowInstanceId = instance.WorkflowInstanceId,
                WorkflowStageDefinitionId = stage.WorkflowStageDefinitionId,
                Status = stage.WorkflowStageDefinitionId == firstStage.WorkflowStageDefinitionId ? SubmittedStatus : PendingStatus,
                StartedAt = stage.WorkflowStageDefinitionId == firstStage.WorkflowStageDefinitionId ? now : null
            }).ToList();

            _db.WorkflowStageInstances.AddRange(stageInstances);
            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = instance.WorkflowInstanceId,
                ActionType = "submitted",
                ToStatus = SubmittedStatus,
                PerformedByUserId = submittedBy,
                PerformedAt = now,
                Comments = "Workflow creado automáticamente al enviar el registro del autor a staging."
            });

            await _db.SaveChangesAsync(ct);

            return await _db.WorkflowInstances
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.StageInstances)
                .FirstOrDefaultAsync(x => x.WorkflowInstanceId == instance.WorkflowInstanceId, ct);
        }

        public async Task<bool> CanProcessBatchAsync(int importBatchId, string? userId, CancellationToken ct = default)
        {
            var workflow = await _db.WorkflowInstances
                .AsNoTracking()
                .Include(x => x.CurrentStageDefinition)
                .FirstOrDefaultAsync(x => x.ImportBatchId == importBatchId, ct);

            if (workflow is null)
            {
                return true;
            }

            if (!string.Equals(workflow.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return workflow.CurrentStageDefinition is null || workflow.CurrentStageDefinition.CanProcessBatch;
        }

        public async Task<List<WorkflowInboxItemDto>> GetReviewInboxAsync(string? userId, IReadOnlyCollection<string> roleNames, int take = 50, CancellationToken ct = default)
        {
            var normalizedRoles = NormalizeRoles(roleNames);
            if (normalizedRoles.Count == 0)
            {
                return [];
            }

            var query = _db.WorkflowInstances
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.WorkflowDefinition)
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.Batch)
                .Include(x => x.StageInstances)
                    .ThenInclude(x => x.WorkflowStageDefinition)
                .Include(x => x.ActionLogs)
                .Where(x => x.CurrentStageDefinition != null);

            if (!normalizedRoles.Contains(AdminRole))
            {
                query = query.Where(x =>
                    x.CurrentStageDefinition!.ResponsibleRoleId != null &&
                    normalizedRoles.Contains(x.CurrentStageDefinition.ResponsibleRoleId));
            }

            var workflows = await query
                .OrderByDescending(x => x.LastActionAt ?? x.SubmittedAt)
                .ThenByDescending(x => x.WorkflowInstanceId)
                .Take(Math.Max(1, take))
                .ToListAsync(ct);

            var displayNames = await BuildWorkflowUserDisplayNamesAsync(workflows, ct);
            var rowCounts = await BuildWorkflowRowCountsAsync(workflows.Select(x => x.ImportBatchId), ct);

            return workflows.Select(x => MapInboxItem(x, userId, displayNames, rowCounts)).ToList();
        }

        public async Task<List<WorkflowInboxItemDto>> GetAuthorInboxAsync(string? userId, int take = 50, CancellationToken ct = default)
        {
            var normalizedUserId = NormalizeReference(userId);
            if (string.IsNullOrWhiteSpace(normalizedUserId))
            {
                return [];
            }

            var authorReferences = await ResolveAuthorReferencesAsync(normalizedUserId, ct);
            if (authorReferences.Count == 0)
            {
                authorReferences.Add(normalizedUserId);
            }

            var query = _db.WorkflowInstances
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.WorkflowDefinition)
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.Batch)
                .Include(x => x.StageInstances)
                    .ThenInclude(x => x.WorkflowStageDefinition)
                .Include(x => x.ActionLogs)
                .AsQueryable();

            query = query.Where(x => x.SubmittedByUserId != null
                && authorReferences.Contains(x.SubmittedByUserId)
                && x.Status != CancelledStatus);

            var workflows = await query
                .OrderByDescending(x => x.LastActionAt ?? x.SubmittedAt)
                .ThenByDescending(x => x.WorkflowInstanceId)
                .Take(Math.Max(1, take))
                .ToListAsync(ct);

            var displayNames = await BuildWorkflowUserDisplayNamesAsync(workflows, ct);
            var rowCounts = await BuildWorkflowRowCountsAsync(workflows.Select(x => x.ImportBatchId), ct);

            return workflows.Select(x => MapInboxItem(x, normalizedUserId, displayNames, rowCounts)).ToList();
        }

        public async Task<bool> CanAuthorAccessBatchAsync(string? userId, int importBatchId, bool requireReturnedStatus = false, CancellationToken ct = default)
        {
            var normalizedUserId = NormalizeReference(userId);
            if (string.IsNullOrWhiteSpace(normalizedUserId))
            {
                return false;
            }

            var authorReferences = await ResolveAuthorReferencesAsync(normalizedUserId, ct);
            if (authorReferences.Count == 0)
            {
                authorReferences.Add(normalizedUserId);
            }

            var query = _db.WorkflowInstances
                .AsNoTracking()
                .Where(x => x.ImportBatchId == importBatchId
                    && x.SubmittedByUserId != null
                    && authorReferences.Contains(x.SubmittedByUserId)
                    && x.Status != CancelledStatus);

            if (requireReturnedStatus)
            {
                query = query.Where(x => x.Status == ReturnedStatus
                    || x.StageInstances.Any(stage => stage.Status == ReturnedStatus));
            }

            return await query.AnyAsync(ct);
        }

        private async Task<HashSet<string>> ResolveAuthorReferencesAsync(string normalizedUserId, CancellationToken ct)
        {
            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                normalizedUserId
            };

            var user = await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == normalizedUserId || x.UserName == normalizedUserId || x.Email == normalizedUserId)
                .Select(x => new { x.Id, x.UserName, x.Email })
                .FirstOrDefaultAsync(ct);

            if (user is null)
            {
                return references;
            }

            if (!string.IsNullOrWhiteSpace(user.Id))
            {
                references.Add(user.Id);
            }

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                references.Add(user.UserName);
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                references.Add(user.Email);
            }

            return references;
        }

        public async Task<WorkflowBatchDetailDto?> GetBatchWorkflowAsync(int importBatchId, CancellationToken ct = default)
        {
            var workflow = await LoadWorkflowAggregateAsync(importBatchId, ct);
            if (workflow is null)
            {
                return null;
            }

            var displayNames = await BuildWorkflowUserDisplayNamesAsync([workflow], ct);
            return MapWorkflow(workflow, displayNames);
        }

        public async Task<WorkflowBatchDetailDto> ClaimCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default)
        {
            var context = await GetActionContextAsync(importBatchId, userId, roleNames, ct);
            var now = DateTime.UtcNow;
            var currentStatus = NormalizeStatus(context.StageInstance.Status);

            if (currentStatus == ApprovedStatus)
            {
                throw new InvalidOperationException("La etapa actual ya fue aprobada.");
            }

            if (currentStatus == ReturnedStatus)
            {
                throw new InvalidOperationException("La etapa actual fue devuelta. El autor debe reenviar el registro antes de retomarla.");
            }

            if (!string.IsNullOrWhiteSpace(context.StageInstance.AssignedToUserId) &&
                !string.Equals(context.StageInstance.AssignedToUserId, context.UserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La etapa actual ya fue tomada por otro revisor.");
            }

            var fromStatus = context.StageInstance.Status;
            context.StageInstance.AssignedToUserId = context.UserId;
            context.StageInstance.StartedAt ??= now;
            context.StageInstance.Status = string.Equals(currentStatus, PendingStatus, StringComparison.OrdinalIgnoreCase)
                ? AssignedStatus
                : InReviewStatus;
            context.Workflow.Status = InReviewStatus;
            context.Workflow.LastActionAt = now;

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = context.Workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = context.StageInstance.WorkflowStageInstanceId,
                ActionType = "claimed",
                FromStatus = fromStatus,
                ToStatus = context.StageInstance.Status,
                PerformedByUserId = context.UserId,
                PerformedAt = now,
                Comments = comments
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> ReturnCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                throw new InvalidOperationException("Para devolver el envío al autor debes registrar una observación.");
            }

            var context = await GetActionContextAsync(importBatchId, userId, roleNames, ct);
            EnsureAssignedReviewer(context);

            var now = DateTime.UtcNow;
            var fromStatus = context.StageInstance.Status;
            var authorComments = EnsureAuthorVisibleComment(comments);
            context.StageInstance.Status = ReturnedStatus;
            context.StageInstance.ReturnedAt = now;
            context.StageInstance.CompletedAt = null;
            context.StageInstance.Notes = authorComments;
            context.Workflow.Status = ReturnedStatus;
            context.Workflow.LastActionAt = now;

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = context.Workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = context.StageInstance.WorkflowStageInstanceId,
                ActionType = "returned",
                FromStatus = fromStatus,
                ToStatus = ReturnedStatus,
                PerformedByUserId = context.UserId,
                PerformedAt = now,
                Comments = authorComments
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> ReturnCurrentStageToPreviousStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                throw new InvalidOperationException("Para devolver el envío a UODIDE debes registrar una observación.");
            }

            var context = await GetActionContextAsync(importBatchId, userId, roleNames, ct);
            EnsureAssignedReviewer(context);

            if (!string.Equals(context.StageDefinition.StageGroupKey, "area-tecnica", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Solo Área Técnica puede devolver una revisión a UODIDE.");
            }

            var previousStageDefinition = context.Workflow.WorkflowDefinition!.Stages
                .Where(x => x.IsActive && x.DisplayOrder < context.StageDefinition.DisplayOrder)
                .OrderByDescending(x => x.DisplayOrder)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("No se pudo resolver la etapa UODIDE anterior.");

            var previousStageInstance = context.Workflow.StageInstances
                .FirstOrDefault(x => x.WorkflowStageDefinitionId == previousStageDefinition.WorkflowStageDefinitionId)
                ?? throw new InvalidOperationException("No se pudo resolver la instancia UODIDE anterior.");

            var now = DateTime.UtcNow;
            var fromStatus = context.StageInstance.Status;
            var reviewerComments = EnsureUodideVisibleComment(comments);

            previousStageInstance.Status = PendingStatus;
            previousStageInstance.AssignedToUserId = null;
            previousStageInstance.ApprovedByUserId = null;
            previousStageInstance.StartedAt = null;
            previousStageInstance.CompletedAt = null;
            previousStageInstance.ReturnedAt = null;
            previousStageInstance.Notes = reviewerComments;

            context.StageInstance.Status = PendingStatus;
            context.StageInstance.AssignedToUserId = null;
            context.StageInstance.ApprovedByUserId = null;
            context.StageInstance.StartedAt = null;
            context.StageInstance.CompletedAt = null;
            context.StageInstance.ReturnedAt = null;
            context.StageInstance.Notes = null;

            context.Workflow.Status = SubmittedStatus;
            context.Workflow.CurrentStageDefinitionId = previousStageDefinition.WorkflowStageDefinitionId;
            context.Workflow.CompletedAt = null;
            context.Workflow.LastActionAt = now;

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = context.Workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = context.StageInstance.WorkflowStageInstanceId,
                ActionType = "returned_to_uodide",
                FromStatus = fromStatus,
                ToStatus = SubmittedStatus,
                PerformedByUserId = context.UserId,
                PerformedAt = now,
                Comments = reviewerComments
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> DeclineCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                throw new InvalidOperationException("Para no tomar el caso debes registrar una observación para el autor.");
            }

            var context = await GetActionContextAsync(importBatchId, userId, roleNames, ct);
            var normalizedAssigned = NormalizeReference(context.StageInstance.AssignedToUserId);
            var normalizedUser = NormalizeReference(context.UserId);

            if (!string.IsNullOrWhiteSpace(normalizedAssigned) &&
                !string.Equals(normalizedAssigned, normalizedUser, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El caso ya fue tomado por otro revisor y no puede ser rechazado desde esta cuenta.");
            }

            var currentStatus = NormalizeStatus(context.StageInstance.Status);
            if (currentStatus == ApprovedStatus)
            {
                throw new InvalidOperationException("La etapa actual ya fue aprobada.");
            }

            if (currentStatus == ReturnedStatus)
            {
                throw new InvalidOperationException("La etapa actual ya fue devuelta al autor.");
            }

            var now = DateTime.UtcNow;
            var fromStatus = context.StageInstance.Status;
            context.StageInstance.Status = ReturnedStatus;
            context.StageInstance.ReturnedAt = now;
            context.StageInstance.CompletedAt = null;
            context.StageInstance.AssignedToUserId = null;
            context.StageInstance.Notes = comments.Trim();
            context.Workflow.Status = ReturnedStatus;
            context.Workflow.LastActionAt = now;

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = context.Workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = context.StageInstance.WorkflowStageInstanceId,
                ActionType = "declined",
                FromStatus = fromStatus,
                ToStatus = ReturnedStatus,
                PerformedByUserId = context.UserId,
                PerformedAt = now,
                Comments = comments.Trim()
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> ApproveCurrentStageAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, string? comments, CancellationToken ct = default)
        {
            var context = await GetActionContextAsync(importBatchId, userId, roleNames, ct);
            EnsureAssignedReviewer(context);

            if (!context.StageDefinition.CanApprove)
            {
                throw new InvalidOperationException("La etapa actual no permite aprobación.");
            }

            await EnsureBatchIsReadyForStageApprovalAsync(importBatchId, ct);

            var orderedDefinitions = context.Workflow.WorkflowDefinition!.Stages
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var now = DateTime.UtcNow;
            var fromStatus = context.StageInstance.Status;
            context.StageInstance.Status = ApprovedStatus;
            context.StageInstance.CompletedAt = now;
            context.StageInstance.ApprovedByUserId = context.UserId;
            context.StageInstance.Notes = comments?.Trim();
            context.Workflow.LastActionAt = now;

            var nextStageDefinition = orderedDefinitions
                .FirstOrDefault(x => x.DisplayOrder > context.StageDefinition.DisplayOrder);

            if (nextStageDefinition is null || context.StageDefinition.IsFinalStage)
            {
                context.Workflow.Status = ApprovedStatus;
                context.Workflow.CompletedAt = now;
            }
            else
            {
                var nextStageInstance = context.Workflow.StageInstances
                    .FirstOrDefault(x => x.WorkflowStageDefinitionId == nextStageDefinition.WorkflowStageDefinitionId)
                    ?? throw new InvalidOperationException("No se pudo resolver la siguiente etapa del workflow.");

                nextStageInstance.Status = PendingStatus;
                nextStageInstance.AssignedToUserId = null;
                nextStageInstance.ApprovedByUserId = null;
                nextStageInstance.StartedAt = null;
                nextStageInstance.CompletedAt = null;
                nextStageInstance.ReturnedAt = null;
                context.Workflow.Status = SubmittedStatus;
                context.Workflow.CurrentStageDefinitionId = nextStageDefinition.WorkflowStageDefinitionId;
            }

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = context.Workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = context.StageInstance.WorkflowStageInstanceId,
                ActionType = "approved",
                FromStatus = fromStatus,
                ToStatus = ApprovedStatus,
                PerformedByUserId = context.UserId,
                PerformedAt = now,
                Comments = comments
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> ResubmitReturnedBatchAsync(int importBatchId, string? userId, string? comments, CancellationToken ct = default)
        {
            var workflow = await LoadMutableAuthorWorkflowAsync(importBatchId, userId, ct);
            if (!string.Equals(workflow.Status, ReturnedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Solo se pueden reenviar envíos devueltos con observaciones.");
            }

            if (workflow.Batch is null)
            {
                throw new InvalidOperationException("No se encontró el lote asociado al envío devuelto.");
            }

            var errorRows = workflow.Batch.Rows.Count(x => string.Equals(x.RowStatus, "Error", StringComparison.OrdinalIgnoreCase));
            if (errorRows > 0)
            {
                throw new InvalidOperationException($"El envío todavía tiene {errorRows} fila(s) con error. Corrige la matriz y vuelve a validar antes de reenviar.");
            }

            var validRows = workflow.Batch.Rows.Count(x => string.Equals(x.RowStatus, "Valid", StringComparison.OrdinalIgnoreCase));
            if (validRows <= 0)
            {
                throw new InvalidOperationException("Antes de reenviar debes validar la matriz corregida y dejar al menos una fila lista para revisión.");
            }

            var now = DateTime.UtcNow;
            workflow.Status = SubmittedStatus;
            workflow.LastActionAt = now;
            workflow.CompletedAt = null;

            var currentStageDefinition = workflow.CurrentStageDefinition
                ?? workflow.WorkflowDefinition?.Stages
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .FirstOrDefault()
                ?? throw new InvalidOperationException("No se pudo resolver la etapa UODIDE del workflow.");

            workflow.CurrentStageDefinitionId = currentStageDefinition.WorkflowStageDefinitionId;
            var currentStageDisplayOrder = currentStageDefinition.DisplayOrder;
            var stageDefinitionsById = workflow.WorkflowDefinition?.Stages
                .Where(x => x.IsActive)
                .ToDictionary(x => x.WorkflowStageDefinitionId)
                ?? new Dictionary<int, WorkflowStageDefinition>();

            foreach (var stage in workflow.StageInstances)
            {
                if (!stageDefinitionsById.TryGetValue(stage.WorkflowStageDefinitionId, out var stageDefinition))
                {
                    continue;
                }

                if (stageDefinition.DisplayOrder < currentStageDisplayOrder)
                {
                    continue;
                }

                if (stage.WorkflowStageDefinitionId == currentStageDefinition.WorkflowStageDefinitionId)
                {
                    stage.Status = SubmittedStatus;
                    stage.AssignedToUserId = null;
                    stage.ApprovedByUserId = null;
                    stage.StartedAt = null;
                    stage.CompletedAt = null;
                    stage.ReturnedAt = null;
                    stage.Notes = null;
                }
                else
                {
                    stage.Status = PendingStatus;
                    stage.AssignedToUserId = null;
                    stage.ApprovedByUserId = null;
                    stage.StartedAt = null;
                    stage.CompletedAt = null;
                    stage.ReturnedAt = null;
                    stage.Notes = null;
                }
            }

            if (workflow.Batch is not null)
            {
                workflow.Batch.Status = "Submitted";
                workflow.Batch.FinishedAt = null;
            }

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = workflow.WorkflowInstanceId,
                WorkflowStageInstanceId = workflow.StageInstances.FirstOrDefault(x => x.WorkflowStageDefinitionId == currentStageDefinition.WorkflowStageDefinitionId)?.WorkflowStageInstanceId,
                ActionType = "resubmitted",
                FromStatus = ReturnedStatus,
                ToStatus = SubmittedStatus,
                PerformedByUserId = NormalizeReference(userId),
                PerformedAt = now,
                Comments = string.IsNullOrWhiteSpace(comments) ? "[Para UODIDE] Envío corregido y reenviado por el autor." : comments.Trim()
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        public async Task<WorkflowBatchDetailDto> CancelReturnedBatchAsync(int importBatchId, string? userId, string? comments, CancellationToken ct = default)
        {
            var workflow = await LoadMutableAuthorWorkflowAsync(importBatchId, userId, ct);
            if (!string.Equals(workflow.Status, ReturnedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Solo se pueden eliminar envíos devueltos con observaciones.");
            }

            var now = DateTime.UtcNow;
            workflow.Status = CancelledStatus;
            workflow.CurrentStageDefinitionId = null;
            workflow.CompletedAt = now;
            workflow.LastActionAt = now;

            foreach (var stage in workflow.StageInstances)
            {
                if (!string.Equals(stage.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
                {
                    stage.Status = CancelledStatus;
                    stage.CompletedAt = now;
                    stage.AssignedToUserId = null;
                }
            }

            if (workflow.Batch is not null)
            {
                workflow.Batch.Status = CancelledStatus;
                workflow.Batch.FinishedAt = now;
            }

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = workflow.WorkflowInstanceId,
                ActionType = "cancelled",
                FromStatus = ReturnedStatus,
                ToStatus = CancelledStatus,
                PerformedByUserId = NormalizeReference(userId),
                PerformedAt = now,
                Comments = string.IsNullOrWhiteSpace(comments) ? "[Nota interna] Envío eliminado por el autor." : comments.Trim()
            });

            await _db.SaveChangesAsync(ct);
            return (await GetBatchWorkflowAsync(importBatchId, ct))!;
        }

        private async Task EnsureBatchIsReadyForStageApprovalAsync(int importBatchId, CancellationToken ct)
        {
            var batchState = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == importBatchId)
                .Select(x => new
                {
                    x.BatchCode,
                    x.TotalRows,
                    PendingRows = x.Rows.Count(row => row.RowStatus == "Pending"),
                    ValidRows = x.Rows.Count(row => row.RowStatus == "Valid"),
                    ErrorRows = x.Rows.Count(row => row.RowStatus == "Error"),
                    ProcessedRows = x.Rows.Count(row => row.RowStatus == "Processed")
                })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("El lote asociado al workflow no existe.");

            if (batchState.TotalRows <= 0)
            {
                throw new InvalidOperationException($"El lote {batchState.BatchCode} no contiene filas para revisar.");
            }

            if (batchState.ErrorRows > 0)
            {
                throw new InvalidOperationException($"El lote {batchState.BatchCode} todavía tiene {batchState.ErrorRows} fila(s) con error. Corrige el staging y vuelve a validar antes de aprobar la etapa.");
            }

            if (batchState.PendingRows > 0)
            {
                throw new InvalidOperationException($"El lote {batchState.BatchCode} todavía tiene {batchState.PendingRows} fila(s) pendiente(s). Ejecuta la validación del staging antes de aprobar la etapa.");
            }

            var reviewedRows = batchState.ValidRows + batchState.ProcessedRows;
            if (reviewedRows < batchState.TotalRows)
            {
                throw new InvalidOperationException($"El lote {batchState.BatchCode} no tiene todas sus filas listas para continuar. Valida el staging y confirma que no queden observaciones.");
            }
        }

        private void EnsureStageDefinition(
            WorkflowDefinition definition,
            string stageKey,
            string stageName,
            int displayOrder,
            string stageGroupKey,
            string stageGroupName,
            string responsibleRoleId,
            bool canProcessBatch,
            bool isFinalStage,
            DateTime now)
        {
            var stage = definition.Stages.FirstOrDefault(x => x.StageKey == stageKey);
            if (stage is null)
            {
                definition.Stages.Add(new WorkflowStageDefinition
                {
                    WorkflowDefinitionId = definition.WorkflowDefinitionId,
                    StageKey = stageKey,
                    StageName = stageName,
                    DisplayOrder = displayOrder,
                    StageGroupKey = stageGroupKey,
                    StageGroupName = stageGroupName,
                    ResponsibleRoleId = responsibleRoleId,
                    CanEditData = true,
                    CanReturn = true,
                    CanApprove = true,
                    CanProcessBatch = canProcessBatch,
                    IsFinalStage = isFinalStage,
                    IsActive = true,
                    CreatedAt = now
                });
                return;
            }

            stage.StageName = stageName;
            stage.DisplayOrder = displayOrder;
            stage.StageGroupKey = stageGroupKey;
            stage.StageGroupName = stageGroupName;
            stage.ResponsibleRoleId = responsibleRoleId;
            stage.CanEditData = true;
            stage.CanReturn = true;
            stage.CanApprove = true;
            stage.CanProcessBatch = canProcessBatch;
            stage.IsFinalStage = isFinalStage;
            stage.IsActive = true;
            stage.UpdatedAt = now;
        }

        private async Task EnsureWorkflowStageDefinitionDefaultsAsync(CancellationToken ct)
        {
            await _db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.WorkflowStageDefinition', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.object_id = dc.parent_object_id
                           AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.WorkflowStageDefinition')
                          AND c.name = N'CanEditData'
                    )
                    BEGIN
                        ALTER TABLE dbo.WorkflowStageDefinition
                        ADD CONSTRAINT DF_WorkflowStageDefinition_CanEditData DEFAULT (0) FOR CanEditData;
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.object_id = dc.parent_object_id
                           AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.WorkflowStageDefinition')
                          AND c.name = N'CanReturn'
                    )
                    BEGIN
                        ALTER TABLE dbo.WorkflowStageDefinition
                        ADD CONSTRAINT DF_WorkflowStageDefinition_CanReturn DEFAULT (1) FOR CanReturn;
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.object_id = dc.parent_object_id
                           AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.WorkflowStageDefinition')
                          AND c.name = N'CanApprove'
                    )
                    BEGIN
                        ALTER TABLE dbo.WorkflowStageDefinition
                        ADD CONSTRAINT DF_WorkflowStageDefinition_CanApprove DEFAULT (1) FOR CanApprove;
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.object_id = dc.parent_object_id
                           AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.WorkflowStageDefinition')
                          AND c.name = N'CanProcessBatch'
                    )
                    BEGIN
                        ALTER TABLE dbo.WorkflowStageDefinition
                        ADD CONSTRAINT DF_WorkflowStageDefinition_CanProcessBatch DEFAULT (0) FOR CanProcessBatch;
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        INNER JOIN sys.columns c
                            ON c.object_id = dc.parent_object_id
                           AND c.column_id = dc.parent_column_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.WorkflowStageDefinition')
                          AND c.name = N'IsFinalStage'
                    )
                    BEGIN
                        ALTER TABLE dbo.WorkflowStageDefinition
                        ADD CONSTRAINT DF_WorkflowStageDefinition_IsFinalStage DEFAULT (0) FOR IsFinalStage;
                    END;
                END;
                """, ct);
        }

        private async Task<WorkflowInstance?> LoadWorkflowAggregateAsync(int importBatchId, CancellationToken ct)
        {
            return await _db.WorkflowInstances
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.WorkflowDefinition)
                    .ThenInclude(x => x!.Stages)
                .Include(x => x.Batch)
                    .ThenInclude(x => x!.Rows)
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.StageInstances)
                    .ThenInclude(x => x.WorkflowStageDefinition)
                .Include(x => x.ActionLogs.OrderByDescending(log => log.PerformedAt))
                .FirstOrDefaultAsync(x => x.ImportBatchId == importBatchId, ct);
        }

        private WorkflowBatchDetailDto MapWorkflow(WorkflowInstance workflow, IReadOnlyDictionary<string, string> displayNames)
        {
            var processedRows = workflow.Batch?.Rows.Count(x => x.RowStatus == "Processed") ?? 0;
            var effectiveStatus = ResolveEffectiveWorkflowStatus(workflow.Status, workflow.Batch?.Status, processedRows);

            return new WorkflowBatchDetailDto
            {
                WorkflowInstanceId = workflow.WorkflowInstanceId,
                ImportBatchId = workflow.ImportBatchId,
                WorkflowKey = workflow.WorkflowDefinition?.Key ?? string.Empty,
                WorkflowName = workflow.WorkflowDefinition?.Name ?? string.Empty,
                Status = effectiveStatus,
                CurrentStageDefinitionId = workflow.CurrentStageDefinitionId,
                CurrentStageKey = workflow.CurrentStageDefinition?.StageKey,
                CurrentStageName = workflow.CurrentStageDefinition?.StageName,
                CurrentStageGroupKey = workflow.CurrentStageDefinition?.StageGroupKey,
                CurrentStageGroupName = workflow.CurrentStageDefinition?.StageGroupName,
                SubmittedByUserId = workflow.SubmittedByUserId,
                SubmittedByUserName = BuildReferenceDisplayName(workflow.SubmittedByUserId, displayNames),
                SubmittedAt = workflow.SubmittedAt,
                LastActionAt = workflow.LastActionAt,
                CompletedAt = workflow.CompletedAt,
                Stages = workflow.StageInstances
                    .OrderBy(x => x.WorkflowStageDefinition?.DisplayOrder ?? int.MaxValue)
                    .Select(x => new WorkflowStageInstanceDto
                    {
                        WorkflowStageInstanceId = x.WorkflowStageInstanceId,
                        WorkflowStageDefinitionId = x.WorkflowStageDefinitionId,
                        StageKey = x.WorkflowStageDefinition?.StageKey ?? string.Empty,
                        StageName = x.WorkflowStageDefinition?.StageName ?? string.Empty,
                        DisplayOrder = x.WorkflowStageDefinition?.DisplayOrder ?? 0,
                        Status = x.Status,
                        StageGroupKey = x.WorkflowStageDefinition?.StageGroupKey,
                        StageGroupName = x.WorkflowStageDefinition?.StageGroupName,
                        ResponsibleRoleId = x.WorkflowStageDefinition?.ResponsibleRoleId,
                        CanEditData = x.WorkflowStageDefinition?.CanEditData ?? false,
                        CanReturn = x.WorkflowStageDefinition?.CanReturn ?? false,
                        CanApprove = x.WorkflowStageDefinition?.CanApprove ?? false,
                        CanProcessBatch = x.WorkflowStageDefinition?.CanProcessBatch ?? false,
                        IsFinalStage = x.WorkflowStageDefinition?.IsFinalStage ?? false,
                        AssignedToUserId = x.AssignedToUserId,
                        AssignedToUserName = BuildReferenceDisplayName(x.AssignedToUserId, displayNames),
                        ApprovedByUserId = x.ApprovedByUserId,
                        ApprovedByUserName = BuildReferenceDisplayName(x.ApprovedByUserId, displayNames),
                        StartedAt = x.StartedAt,
                        CompletedAt = x.CompletedAt,
                        ReturnedAt = x.ReturnedAt,
                        Notes = x.Notes
                    })
                    .ToList(),
                Actions = workflow.ActionLogs
                    .OrderByDescending(x => x.PerformedAt)
                    .Select(x => new WorkflowActionLogDto
                    {
                        WorkflowActionLogId = x.WorkflowActionLogId,
                        WorkflowStageInstanceId = x.WorkflowStageInstanceId,
                        ActionType = x.ActionType,
                        FromStatus = x.FromStatus,
                        ToStatus = x.ToStatus,
                        PerformedByUserId = x.PerformedByUserId,
                        PerformedByUserName = BuildReferenceDisplayName(x.PerformedByUserId, displayNames),
                        PerformedAt = x.PerformedAt,
                        Comments = x.Comments
                    })
                    .ToList()
            };
        }

        private WorkflowInboxItemDto MapInboxItem(
            WorkflowInstance workflow,
            string? currentUserId,
            IReadOnlyDictionary<string, string> displayNames,
            IReadOnlyDictionary<int, WorkflowRowCounts>? rowCountsByBatch = null)
        {
            var currentStage = workflow.CurrentStageDefinition;
            var currentStageInstance = currentStage is null
                ? null
                : workflow.StageInstances.FirstOrDefault(x => x.WorkflowStageDefinitionId == currentStage.WorkflowStageDefinitionId);

            WorkflowRowCounts? rowCounts = null;
            rowCountsByBatch?.TryGetValue(workflow.ImportBatchId, out rowCounts);
            var validRows = rowCounts?.ValidRows ?? workflow.Batch?.Rows.Count(x => x.RowStatus == "Valid") ?? 0;
            var errorRows = rowCounts?.ErrorRows ?? workflow.Batch?.Rows.Count(x => x.RowStatus == "Error") ?? 0;
            var processedRows = rowCounts?.ProcessedRows ?? workflow.Batch?.Rows.Count(x => x.RowStatus == "Processed") ?? 0;
            var effectiveStatus = ResolveEffectiveWorkflowStatus(workflow.Status, workflow.Batch?.Status, processedRows);
            var latestAction = workflow.ActionLogs
                .OrderByDescending(x => x.PerformedAt)
                .FirstOrDefault();
            var latestAuthorObservation = workflow.ActionLogs
                .OrderByDescending(x => x.PerformedAt)
                .Select(x => x.Comments)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("[Para autor]", StringComparison.OrdinalIgnoreCase));
            var assignedToUserId = currentStageInstance?.AssignedToUserId;
            var normalizedCurrentUserId = NormalizeReference(currentUserId);
            var isAssignedToCurrentUser = !string.IsNullOrWhiteSpace(assignedToUserId) &&
                                          string.Equals(assignedToUserId, normalizedCurrentUserId, StringComparison.OrdinalIgnoreCase);
            var canOperateCurrentStage = currentStageInstance is not null &&
                                         !string.Equals(currentStageInstance.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase) &&
                                         !string.Equals(currentStageInstance.Status, ReturnedStatus, StringComparison.OrdinalIgnoreCase) &&
                                         !string.Equals(effectiveStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase) &&
                                         !string.Equals(effectiveStatus, ProcessedStatus, StringComparison.OrdinalIgnoreCase);

            return new WorkflowInboxItemDto
            {
                WorkflowInstanceId = workflow.WorkflowInstanceId,
                ImportBatchId = workflow.ImportBatchId,
                BatchCode = workflow.Batch?.BatchCode ?? string.Empty,
                SourceType = workflow.Batch?.SourceType ?? string.Empty,
                WorkflowKey = workflow.WorkflowDefinition?.Key ?? string.Empty,
                WorkflowName = workflow.WorkflowDefinition?.Name ?? string.Empty,
                WorkflowStatus = effectiveStatus,
                CurrentStageKey = currentStage?.StageKey,
                CurrentStageName = currentStage?.StageName,
                CurrentStageGroupName = currentStage?.StageGroupName,
                ResponsibleRoleName = currentStage?.ResponsibleRoleId,
                SubmittedByUserId = workflow.SubmittedByUserId,
                SubmittedByUserName = BuildReferenceDisplayName(workflow.SubmittedByUserId, displayNames),
                AssignedToUserId = currentStageInstance?.AssignedToUserId,
                AssignedToUserName = BuildReferenceDisplayName(currentStageInstance?.AssignedToUserId, displayNames),
                TotalRows = workflow.Batch?.TotalRows ?? 0,
                ErrorRows = errorRows,
                ValidRows = validRows,
                ProcessedRows = processedRows,
                SubmittedAt = workflow.SubmittedAt,
                LastActionAt = workflow.LastActionAt,
                LastActionType = latestAction?.ActionType,
                LatestAuthorObservation = CleanAudiencePrefix(latestAuthorObservation),
                CanClaim = canOperateCurrentStage &&
                           string.IsNullOrWhiteSpace(assignedToUserId),
                CanReturn = canOperateCurrentStage &&
                            currentStage?.CanReturn == true &&
                            isAssignedToCurrentUser,
                CanApprove = canOperateCurrentStage &&
                             currentStage?.CanApprove == true &&
                             isAssignedToCurrentUser,
                CanProcessBatch = currentStage?.CanProcessBatch == true &&
                                  string.Equals(effectiveStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase) &&
                                  processedRows < (workflow.Batch?.TotalRows ?? 0)
            };
        }

        private static string ResolveEffectiveWorkflowStatus(string? workflowStatus, string? batchStatus, int processedRows)
        {
            if (processedRows > 0 || string.Equals(batchStatus, ProcessedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return ProcessedStatus;
            }

            return string.IsNullOrWhiteSpace(workflowStatus)
                ? string.Empty
                : workflowStatus;
        }

        private async Task<WorkflowActionContext> GetActionContextAsync(int importBatchId, string? userId, IReadOnlyCollection<string> roleNames, CancellationToken ct)
        {
            var workflow = await _db.WorkflowInstances
                .Include(x => x.WorkflowDefinition)
                    .ThenInclude(x => x!.Stages)
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.StageInstances)
                .FirstOrDefaultAsync(x => x.ImportBatchId == importBatchId, ct)
                ?? throw new InvalidOperationException("El lote no tiene un workflow configurado.");

            if (workflow.CurrentStageDefinitionId is null)
            {
                throw new InvalidOperationException("El workflow no tiene una etapa activa.");
            }

            var stageDefinition = workflow.WorkflowDefinition!.Stages
                .FirstOrDefault(x => x.WorkflowStageDefinitionId == workflow.CurrentStageDefinitionId.Value)
                ?? throw new InvalidOperationException("No se pudo resolver la definición de la etapa actual.");

            var stageInstance = workflow.StageInstances
                .FirstOrDefault(x => x.WorkflowStageDefinitionId == stageDefinition.WorkflowStageDefinitionId)
                ?? throw new InvalidOperationException("No se pudo resolver la etapa activa del workflow.");

            var normalizedUserId = NormalizeReference(userId);
            if (string.IsNullOrWhiteSpace(normalizedUserId))
            {
                throw new InvalidOperationException("No se pudo resolver el usuario autenticado para esta acción del workflow.");
            }

            var normalizedRoles = NormalizeRoles(roleNames);
            var isAdmin = normalizedRoles.Contains(AdminRole);
            if (!string.IsNullOrWhiteSpace(stageDefinition.ResponsibleRoleId) &&
                !isAdmin &&
                !normalizedRoles.Contains(stageDefinition.ResponsibleRoleId))
            {
                throw new InvalidOperationException("El usuario actual no tiene permisos para actuar sobre esta etapa del workflow.");
            }

            return new WorkflowActionContext
            {
                Workflow = workflow,
                StageDefinition = stageDefinition,
                StageInstance = stageInstance,
                UserId = normalizedUserId
            };
        }

        private async Task<WorkflowInstance> LoadMutableAuthorWorkflowAsync(int importBatchId, string? userId, CancellationToken ct)
        {
            var normalizedUserId = NormalizeReference(userId);
            if (string.IsNullOrWhiteSpace(normalizedUserId))
            {
                throw new InvalidOperationException("No se pudo resolver el usuario autor.");
            }

            var authorReferences = await ResolveAuthorReferencesAsync(normalizedUserId, ct);
            var workflow = await _db.WorkflowInstances
                .Include(x => x.WorkflowDefinition)
                    .ThenInclude(x => x!.Stages)
                .Include(x => x.CurrentStageDefinition)
                .Include(x => x.StageInstances)
                .Include(x => x.Batch)
                    .ThenInclude(x => x!.Rows)
                .FirstOrDefaultAsync(x => x.ImportBatchId == importBatchId, ct)
                ?? throw new InvalidOperationException("El envío no tiene un workflow configurado.");

            if (workflow.SubmittedByUserId is null || !authorReferences.Contains(workflow.SubmittedByUserId))
            {
                throw new InvalidOperationException("Solo el autor que realizó el envío puede modificar este caso devuelto.");
            }

            return workflow;
        }

        private static HashSet<string> NormalizeRoles(IReadOnlyCollection<string> roleNames)
        {
            return (roleNames ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string? NormalizeReference(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string EnsureAuthorVisibleComment(string comments)
        {
            var trimmed = comments.Trim();
            return trimmed.StartsWith("[Para autor]", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"[Para autor] {CleanAudiencePrefix(trimmed)}";
        }

        private static string EnsureUodideVisibleComment(string comments)
        {
            var trimmed = comments.Trim();
            return trimmed.StartsWith("[Para UODIDE]", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"[Para UODIDE] {CleanAudiencePrefix(trimmed)}";
        }

        private static string NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        private static void EnsureAssignedReviewer(WorkflowActionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.StageInstance.AssignedToUserId) &&
                !string.Equals(context.StageInstance.AssignedToUserId, context.UserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La etapa actual está asignada a otro revisor.");
            }

            if (string.IsNullOrWhiteSpace(context.StageInstance.AssignedToUserId))
            {
                context.StageInstance.AssignedToUserId = context.UserId;
            }
        }

        private async Task<Dictionary<string, string>> BuildWorkflowUserDisplayNamesAsync(IEnumerable<WorkflowInstance> workflows, CancellationToken ct)
        {
            var references = workflows
                .SelectMany(workflow =>
                {
                    var values = new List<string?>();
                    values.Add(workflow.SubmittedByUserId);
                    values.AddRange(workflow.StageInstances.Select(x => x.AssignedToUserId));
                    values.AddRange(workflow.StageInstances.Select(x => x.ApprovedByUserId));
                    values.AddRange(workflow.ActionLogs.Select(x => x.PerformedByUserId));
                    return values;
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (references.Count == 0)
            {
                return displayNames;
            }

            var users = await _db.Users
                .AsNoTracking()
                .Where(x => references.Contains(x.Id)
                            || (x.UserName != null && references.Contains(x.UserName))
                            || (x.Email != null && references.Contains(x.Email)))
                .Select(x => new
                {
                    x.Id,
                    x.UserName,
                    x.Email,
                    x.FullName
                })
                .ToListAsync(ct);

            foreach (var user in users)
            {
                var displayName = !string.IsNullOrWhiteSpace(user.FullName)
                    ? user.FullName.Trim()
                    : !string.IsNullOrWhiteSpace(user.Email)
                        ? user.Email.Trim()
                        : !string.IsNullOrWhiteSpace(user.UserName)
                            ? user.UserName.Trim()
                            : user.Id;

                AddDisplayName(displayNames, user.Id, displayName);
                AddDisplayName(displayNames, user.UserName, displayName);
                AddDisplayName(displayNames, user.Email, displayName);
            }

            return displayNames;
        }

        private async Task<IReadOnlyDictionary<int, WorkflowRowCounts>> BuildWorkflowRowCountsAsync(IEnumerable<int> importBatchIds, CancellationToken ct)
        {
            var batchIds = importBatchIds
                .Distinct()
                .ToList();

            if (batchIds.Count == 0)
            {
                return new Dictionary<int, WorkflowRowCounts>();
            }

            var rows = await _db.ImportBatchRows
                .AsNoTracking()
                .Where(x => batchIds.Contains(x.ImportBatchId))
                .GroupBy(x => new { x.ImportBatchId, x.RowStatus })
                .Select(x => new
                {
                    x.Key.ImportBatchId,
                    x.Key.RowStatus,
                    Count = x.Count()
                })
                .ToListAsync(ct);

            return rows
                .GroupBy(x => x.ImportBatchId)
                .ToDictionary(
                    group => group.Key,
                    group => new WorkflowRowCounts(
                        ValidRows: group.Where(x => x.RowStatus == "Valid").Sum(x => x.Count),
                        ErrorRows: group.Where(x => x.RowStatus == "Error").Sum(x => x.Count),
                        ProcessedRows: group.Where(x => x.RowStatus == "Processed").Sum(x => x.Count)));
        }

        private static void AddDisplayName(IDictionary<string, string> displayNames, string? reference, string displayName)
        {
            if (!string.IsNullOrWhiteSpace(reference) && !displayNames.ContainsKey(reference.Trim()))
            {
                displayNames[reference.Trim()] = displayName;
            }
        }

        private static string? BuildReferenceDisplayName(string? value, IReadOnlyDictionary<string, string> displayNames)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var reference = value.Trim();
            return displayNames.TryGetValue(reference, out var displayName)
                ? displayName
                : reference;
        }

        private static string? CleanAudiencePrefix(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return null;
            }

            return comment
                .Replace("[Para autor]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("[Para UODIDE]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("[Para siguiente revisor]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("[Nota interna]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private sealed class WorkflowActionContext
        {
            public WorkflowInstance Workflow { get; set; } = null!;
            public WorkflowStageDefinition StageDefinition { get; set; } = null!;
            public WorkflowStageInstance StageInstance { get; set; } = null!;
            public string UserId { get; set; } = string.Empty;
        }

        private sealed record WorkflowRowCounts(int ValidRows, int ErrorRows, int ProcessedRows);
    }
}
