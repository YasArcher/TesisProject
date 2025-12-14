using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Matrices.Response
{
    public class ProjectMatrixProjectRowDTO
    {
        public int DataIndex { get; set; }          // índice dentro de los datos (0, 1, 2, ...)
        public int ExcelRowNumber { get; set; }     // número de fila en Excel (por trazabilidad)

        // Clave natural
        public string? Convocation { get; set; }    // CONVOCATORIA (la “real”, la 2da)
        public string? Code { get; set; }           // CODIGO
        public string? Number { get; set; }         // Nro.

        public string? ProjectName { get; set; }    // PROYECTO
        public string? FacultyName { get; set; }    // Facultad

        public decimal? ExecutionPercentage { get; set; } // PORCENTAJE DE EJECUCION
        public string? FinalReportMemo { get; set; }      // MEMORANDO DEL INFORME FINAL

        public DateTime? EstimatedEndDate { get; set; }   // FECHA DE FINALIZACIÓN ESTIMADA
        public string? State { get; set; }                // ESTADO
        public string? TermText { get; set; }             // PLAZO
        public DateTime? StartDate { get; set; }          // FECHA DE INICIO
    }
}
