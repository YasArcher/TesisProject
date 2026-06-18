using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.External
{
    public sealed class ExternalTeacherDistributivoDTO
    {
        public int DistributivoId { get; set; }

        public int PeriodId { get; set; }
        public string PeriodName { get; set; } = string.Empty;

        public int FacultyId { get; set; }
        public int? CareerId { get; set; }

        public string FacultyName { get; set; } = string.Empty;
        public string? CareerName { get; set; }

        public int? AspId { get; set; }

        public string Document { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public decimal Hours { get; set; }
    }
}