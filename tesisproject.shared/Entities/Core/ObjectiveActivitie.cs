using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class ObjectiveActivitie
    {
        public int ObjectiveActivitieId { get; set; } // id_actividad_objetivo
        public int ObjectiveActivitiesId { get; set; } // id_actividad_objetivo
        public int ObjetiveId { get; set; }             // id_proyecto (FK -> Project)
        public int ObjectiveNumber { get; set; }       // número_objetivo (1, 2, 3, ...)
        public string ActivityDescription { get; set; } = string.Empty; // descripción_actividad
        public string ActivityResult { get; set; } = string.Empty;      // resultado_actividad
        public decimal PercentajeValue { get; set; }   // valor_porcentual
        public bool IsCompleted { get; set; }          // está_completado

    }
}
