using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Options
{
    public sealed class DocumentRecognitionOptions
    {
        public const string SectionName = "DocumentRecognition";

        [Range(0, 100)]
        public double ProjectTypeSimilarityThreshold { get; set; } = 70.0;

        [Range(0, 100)]
        public double MemberRoleSimilarityThreshold { get; set; } = 50.0;
    }
}