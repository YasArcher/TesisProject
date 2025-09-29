using tesisproject.frontend.Models.Articles;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Utils;

public static class ArticlesMapper
{
    public static CreateArticleRequest ToCreateRequest(ArticleFormModel m)
    {
        int? ToInt(string? s) => int.TryParse(s, out var v) ? v : null;
        decimal? ToDec(string? s) => decimal.TryParse(s, out var v) ? v : null;
        DateTime? ToDate(string? s) => DateTime.TryParse(s, out var d) ? d : null;

        var participantes = new List<ArticleParticipantRequest>();
        void AddIf(int idx, string? id, string? nombre, string? part)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
                participantes.Add(new ArticleParticipantRequest { Index = idx, Identificacion = id, Nombre = nombre, Participacion = part });
        }

        AddIf(1, m.Identificacion1, m.Nombre1, m.Participacion1);
        AddIf(2, m.Identificacion2, m.Nombre2, m.Participacion2);
        AddIf(3, m.Identificacion3, m.Nombre3, m.Participacion3);
        AddIf(4, m.Identificacion4, m.Nombre4, m.Participacion4);
        AddIf(5, m.Identificacion5, m.Nombre5, m.Participacion5);

        return new CreateArticleRequest
        {
            PeriodoAcademico = m.PeriodoAcademico,
            Anio = ToInt(m.Anio),
            CodigoPublicacion = m.CodigoPublicacion,
            CodigoISSN = m.CodigoISSN,
            Titulo = m.Titulo,
            NombreRevista = m.NombreRevista,
            VolumenRevista = m.VolumenRevista,
            NumeroRevista = m.NumeroRevista,
            NumeroPaginas = ToInt(m.NumeroPaginas),
            SJR = ToDec(m.SJR),
            FechaPublicacion = ToDate(m.FechaPublicacion),
            BaseDatos = m.BaseDatos,
            CampoAmplio = m.CampoAmplio,
            CampoEspecifico = m.CampoEspecifico,
            CampoDetallado = m.CampoDetallado,
            Quartil = m.Quartil,
            Filiacion = m.Filiacion,
            Estado = m.Estado,
            AccesoAbierto = m.AccesoAbierto,
            LinkPublicacion = m.LinkPublicacion,
            EnlaceRevista = m.EnlaceRevista,
            CodigoProyectoArticulado = m.CodigoProyectoArticulado,
            ProyectoArticulado = m.ProyectoArticulado,
            LineaInvestigacionArticulada = m.LineaInvestigacionArticulada,
            GrupoInvestigacionArticulado = m.GrupoInvestigacionArticulado,
            Participantes = participantes
        };
    }
}
