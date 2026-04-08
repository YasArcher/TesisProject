namespace tesisproject.backend.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Author = "Author";
    public const string WorkflowReviewerUodide = "WorkflowReviewerUodide";
    public const string WorkflowReviewerAreaTecnica = "WorkflowReviewerAreaTecnica";
    public const string WorkflowProcessorAreaTecnica = "WorkflowProcessorAreaTecnica";

    public static readonly string[] All =
    [
        Admin,
        Analyst,
        Author,
        WorkflowReviewerUodide,
        WorkflowReviewerAreaTecnica,
        WorkflowProcessorAreaTecnica
    ];
}
