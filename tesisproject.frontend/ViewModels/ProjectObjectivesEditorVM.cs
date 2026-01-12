namespace tesisproject.frontend.ViewModels
{
    public sealed class ProjectObjectiveEditVM
    {
        public int ObjectiveId { get; set; }
        public int ProjectId { get; set; }
        public int ObjectiveTypeId { get; set; }

        public string ObjectiveText { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;

        public int Percentage { get; set; } = 0;

        public List<ObjectiveActivityEditVM> Activities { get; set; } = new();
    }

    public sealed class ObjectiveActivityEditVM
    {
        public int ActivityId { get; set; } // 0 = nueva
        public int ObjectiveId { get; set; }

        public string ActionText { get; set; } = string.Empty;
        public string? ActivityResult { get; set; }
    }
}