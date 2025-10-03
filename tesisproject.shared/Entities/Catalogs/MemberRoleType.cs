using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    public class MemberRoleType :  CatalogEntityBase
    {
        public int flag { get; set; } = 1; //1: Roles de Integrantes de Proyecto, 2: Roles de Integrantes de Grupo
    }
}
