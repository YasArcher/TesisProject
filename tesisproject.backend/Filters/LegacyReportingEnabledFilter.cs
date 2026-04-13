using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;

namespace tesisproject.backend.Filters;

public sealed class LegacyReportingEnabledFilter : IAsyncActionFilter
{
    private readonly LegacyReportingOptions _options;

    public LegacyReportingEnabledFilter(IOptions<LegacyReportingOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_options.Enabled)
        {
            context.Result = new ObjectResult(new
            {
                message = "La reportería heredada está deshabilitada.",
                detail = "Este endpoint pertenece al módulo BI/ETL anterior y será reemplazado por la reportería del flujo actual."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        await next();
    }
}
