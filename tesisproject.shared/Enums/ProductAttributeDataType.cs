using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// Enumerates all supported data types for ProductAttributeDefinition.
    /// Determines how each attribute is rendered and validated.
    /// </summary>
    public enum ProductAttributeDataType
    {
        [Description("Plain text value")]
        Text = 0,

        [Description("Numeric value (integer or decimal)")]
        Number = 1,

        [Description("Date or datetime value")]
        Date = 2,

        [Description("URL or link value")]
        Url = 3
    }
}
