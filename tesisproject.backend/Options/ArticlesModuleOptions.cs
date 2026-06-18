// [ARTICLES-MIGRATION] Origen: sistema de articulos. Bandera de activacion para una fusion progresiva y reversible.
namespace tesisproject.backend.Options
{
    public sealed class ArticlesModuleOptions
    {
        public const string SectionName = "ArticlesModule";
        public bool Enabled { get; set; }
    }
}
