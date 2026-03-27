using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Auth
{
    /// <summary>
    /// Centraliza nombres de Roles y combinaciones para Authorize(Roles="...").
    /// Mantén aquí los strings para no regarlos por toda la solución.
    /// </summary>
    public static class AppRoles
    {
        // ===== Roles base (deben coincidir con los seeds en AspNetRoles) =====
        public const string SuperAdmin = "superadmin";
        public const string Admin = "admin";
        public const string Technical = "technical";
        public const string Coordinador = "coordinador";
        public const string User = "user";
        public const string Financial = "financial";

        // ===== Conjuntos compuestos típicos =====

        /// <summary>
        /// Lectura del “área técnica” (GETs): coordinador + technical + admin + superadmin.
        /// </summary>
        public const string ReadTechArea = Coordinador + "," + Technical + "," + Admin + "," + SuperAdmin;

        /// <summary>
        /// Escritura del “área técnica” (POST/PUT/DELETE): technical + admin + superadmin.
        /// </summary>
        public const string WriteTechArea = Technical + "," + Admin + "," + SuperAdmin;

        /// <summary>
        /// Zona admin: admin + superadmin.
        /// </summary>
        public const string AdminArea = Admin + "," + SuperAdmin;

        /// <summary>
        /// Solo superadmin.
        /// </summary>
        public const string SuperOnly = SuperAdmin;

        /// <summary>
        /// Operaciones financieras paralelas: financial + admin + superadmin.
        /// </summary>
        public const string FinancialOps = Financial + "," + Admin + "," + SuperAdmin;

        // ===== Extras útiles =====

        /// <summary>
        /// Cualquier usuario con sesión (si necesitas endpoints visibles para todos los logueados).
        /// </summary>
        public const string AnyAuthenticated = User + "," + Coordinador + "," + Technical + "," + Admin + "," + SuperAdmin + "," + Financial;
    }
}