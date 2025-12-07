using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// Enumerates all supported data types for ProductAttribute.
    /// Determines how each attribute is rendered and validated.
    /// </summary>
    public enum ProductAttributeDataType
    {
        [Description("Texto libre")]
        Text = 0,

        [Description("Valor numérico (entero o decimal)")]
        Number = 1,

        [Description("Fecha")]
        Date = 2,

        [Description("Enlace o URL")]
        Url = 3
    }
}