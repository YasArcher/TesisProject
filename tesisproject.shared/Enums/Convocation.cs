using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    public enum Quartile
    {
        None = 0, Q4 = 4, Q3 = 3, Q2 = 2, Q1 = 1
    }

    public enum RequirementUnit
    {
        PerProject = 0, // cantidad fija por proyecto
        PerYear = 1     // se multiplica por cada 12 meses (ceil)
    }
}