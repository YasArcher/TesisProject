using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class ObjectiveActivityUser
    {
        // ================================
        //              Keys
        // ================================
        public int Id { get; set; }

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int ObjectiveActivityId { get; set; }   // FK → ObjectiveActivity

        [Required]
        public int UserId { get; set; }                // FK → IdentityUser<int>
        [Required]
        public int VisitId { get; set; }               // FK → Visit (existing entity in Core)

        // ================================
        //        Core Information
        // ================================
        [Range(0, double.MaxValue)]
        public double? WeeklyHours { get; set; }       // Horas semanales asignadas (opcional)

        [StringLength(500)]
        public string? RoleDescription { get; set; }   // Rol o aporte (ej: "análisis de resultados", "supervisión", etc.)

        [StringLength(1000)]
        public string? ReportNotes { get; set; }       // Observaciones o reporte del periodo

        // ================================
        //      Navigation Properties
        // ================================
        public ObjectiveActivity ObjectiveActivity { get; set; } = null!;
        public Visit Visit { get; set; } = null!;
    }
}