using Microsoft.AspNetCore.Components;

namespace tesisproject.frontend.SharedUI
{
    public partial class Container<TItem>
    {
        #region Parámetros principales

        /// <summary>
        /// Colección de elementos a renderizar
        /// </summary>
        [Parameter]
        public IEnumerable<TItem> Items { get; set; } = Enumerable.Empty<TItem>();

        /// <summary>
        /// Template para renderizar cada elemento
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> ChildContent { get; set; } = default!;

        #endregion

        #region Parámetros de personalización del layout

        /// <summary>
        /// Clases CSS para el layout del grid. Por defecto: lista vertical
        /// </summary>
        [Parameter]
        public string GridClasses { get; set; } = "space-y-4";

        /// <summary>
        /// Título opcional para mostrar en el header
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "";

        /// <summary>
        /// Indica si mostrar el contador de elementos
        /// </summary>
        [Parameter]
        public bool ShowItemCount { get; set; } = false;

        #endregion

        #region Parámetros del estado vacío

        /// <summary>
        /// Título para el estado vacío
        /// </summary>
        [Parameter]
        public string EmptyStateTitle { get; set; } = "No hay datos disponibles";

        /// <summary>
        /// Descripción para el estado vacío
        /// </summary>
        [Parameter]
        public string EmptyStateDescription { get; set; } = "No se encontraron registros para mostrar en este momento.";

        /// <summary>
        /// Icono personalizado para el estado vacío
        /// </summary>
        [Parameter]
        public RenderFragment? EmptyStateIcon { get; set; }

        /// <summary>
        /// Acciones personalizadas para el estado vacío (botones, enlaces, etc.)
        /// </summary>
        [Parameter]
        public RenderFragment? EmptyStateAction { get; set; }

        #endregion

        #region Parámetros de estado

        /// <summary>
        /// Indica si el contenedor está en estado de carga
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; } = false;

        #endregion

        #region Layouts predefinidos

        /// <summary>
        /// Obtiene clases CSS para un layout de grid responsivo
        /// </summary>
        /// <param name="columns">Número de columnas en desktop (por defecto 3)</param>
        /// <returns>String con las clases CSS del grid</returns>
        public static string GetGridLayout(int columns = 3)
        {
            return columns switch
            {
                1 => "grid grid-cols-1 gap-4",
                2 => "grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6",
                3 => "grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 md:gap-6",
                4 => "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-6",
                _ => "grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 md:gap-6"
            };
        }

        /// <summary>
        /// Obtiene clases CSS para un layout de lista vertical
        /// </summary>
        /// <param name="spacing">Espaciado entre elementos</param>
        /// <returns>String con las clases CSS de la lista</returns>
        public static string GetListLayout(string spacing = "space-y-4")
        {
            return spacing;
        }

        /// <summary>
        /// Obtiene clases CSS para un layout de masonry
        /// </summary>
        /// <returns>String con las clases CSS del masonry</returns>
        public static string GetMasonryLayout()
        {
            return "columns-1 sm:columns-2 lg:columns-3 xl:columns-4 gap-4 md:gap-6 space-y-4";
        }

        #endregion
    }
}