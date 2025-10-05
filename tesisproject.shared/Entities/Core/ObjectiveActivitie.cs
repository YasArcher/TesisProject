using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class ObjectiveActivitie
    {
        // ================================
        //              Keys
        // ================================
        public int ObjectiveActivitieId { get; set; } // id_actividad_objetivo

        // ================================
        //           Foreign Keys
        // ================================
        public int ObjetiveId { get; set; }           // id_objetivo (FK -> Objective)

        // ================================
        //        Core Information
        // ================================
        [Range(1, int.MaxValue)]
        public int ObjectiveNumber { get; set; }       // número_objetivo (1, 2, 3, ...)

        [Required, StringLength(500)]
        public string ActivityDescription { get; set; } = string.Empty; // descripción_actividad

        [Required, StringLength(500)]
        public string ActivityResult { get; set; } = string.Empty;      // resultado_actividad

        [Range(0, 100)]
        public decimal PercentajeValue { get; set; }   // valor_porcentual

        public bool IsCompleted { get; set; }          // está_completado
    }
}
