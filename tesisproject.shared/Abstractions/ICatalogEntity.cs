using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Abstractions
{
    public interface ICatalogEntity
    {
        int Id { get; set; }
        string Name { get; set; }
    }
}
