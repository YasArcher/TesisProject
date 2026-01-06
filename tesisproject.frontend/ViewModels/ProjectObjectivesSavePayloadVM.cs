namespace tesisproject.frontend.ViewModels
{
    public class ProjectObjectivesSavePayloadVM
    {
        public List<ProjectObjectiveEditVM> Current { get; set; } = new();
        public List<ProjectObjectiveEditVM> Original { get; set; } = new();
    }
}
