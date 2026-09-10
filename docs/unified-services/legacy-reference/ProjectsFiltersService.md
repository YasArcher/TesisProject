# Referencia legacy — ProjectsFiltersService

## TODO UNIFIED: GetBootstrapAsync

El bootstrap expone IDs de facultades externos que se usan como filtros locales. Acordar traducción antes de entregar contrato Unified.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectsFiltersService.cs:29-78`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<ProjectsFilterBootstrapDTO>> GetBootstrapAsync(
            CancellationToken ct = default)
        {
            try
            {
                var dto = new ProjectsFilterBootstrapDTO();

                // =======================
                // ProjectStates
                // =======================
                var statesRes = await _catalogs.GetKeyValuesAsync<ProjectState>(ct: ct);
                dto.ProjectStates = GetDataOrEmpty(statesRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // ProjectTypes
                // =======================
                var typesRes = await _catalogs.GetKeyValuesAsync<ProjectType>(ct: ct);
                dto.ProjectTypes = GetDataOrEmpty(typesRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // Faculties (API externa)
                // =======================
                var facRes = await _extTypes.GetFacultiesKeyValuesAsync(ct);
                dto.Faculties = GetDataOrEmpty(facRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // Funding
                // =======================
                var fundingRes = await _catalogs.GetKeyValuesAsync<FundingType>(ct: ct);
                dto.Funding = GetDataOrEmpty(fundingRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // ResearchCategoryTypes dinámicos
                // =======================
                dto.ResearchCategoryTypes = await BuildResearchCategoryTypesAsync(ct);

                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Ok(dto, ProjectsFiltersBootstrapGeneratedMessage);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Fail(OperationCanceledMessage, ErrorType.Unexpected);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }
```
