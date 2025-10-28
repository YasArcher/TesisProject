using System.Globalization;
using tesisproject.frontend.Models.Articles;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Utils;

public static class ArticlesMapper
{
    // ===== Helpers de parseo (invariantes) =====
    private static int? ToInt(string? s)
        => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static decimal? ToDec(string? s)
        => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static DateTime? ToDate(string? s)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d) ? d : (DateTime?)null;

    // ===== UI -> Create =====
    public static CreateArticleRequest ToCreateRequest(ArticleViewModel m)
    {
        var participantes = new List<ArticleParticipantRequest>();
        void AddIf(int idx, string? id, string? nombre, string? part)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
                participantes.Add(new ArticleParticipantRequest
                {
                    Index = idx,
                    Identificacion = id,
                    Nombre = nombre,
                    Participacion = part
                });
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

    // ===== UI -> Update =====
    public static UpdateArticleRequest ToUpdateRequest(ArticleViewModel m, int id)
    {
        var create = ToCreateRequest(m);
        return new UpdateArticleRequest
        {
            Id = id,
            PeriodoAcademico = create.PeriodoAcademico,
            Anio = create.Anio,
            CodigoPublicacion = create.CodigoPublicacion,
            CodigoISSN = create.CodigoISSN,
            Titulo = create.Titulo,
            NombreRevista = create.NombreRevista,
            VolumenRevista = create.VolumenRevista,
            NumeroRevista = create.NumeroRevista,
            NumeroPaginas = create.NumeroPaginas,
            SJR = create.SJR,
            FechaPublicacion = create.FechaPublicacion,
            BaseDatos = create.BaseDatos,
            CampoAmplio = create.CampoAmplio,
            CampoEspecifico = create.CampoEspecifico,
            CampoDetallado = create.CampoDetallado,
            Quartil = create.Quartil,
            Filiacion = create.Filiacion,
            Estado = create.Estado,
            AccesoAbierto = create.AccesoAbierto,
            LinkPublicacion = create.LinkPublicacion,
            EnlaceRevista = create.EnlaceRevista,
            CodigoProyectoArticulado = create.CodigoProyectoArticulado,
            ProyectoArticulado = create.ProyectoArticulado,
            LineaInvestigacionArticulada = create.LineaInvestigacionArticulada,
            GrupoInvestigacionArticulado = create.GrupoInvestigacionArticulado,
            Participantes = create.Participantes
        };
    }

    // ===== DTO -> UI (precarga para edición) =====
    public static ArticleViewModel ToViewModel(ArticleDto a)
    {
        var vm = new ArticleViewModel
        {
            PeriodoAcademico = a.PeriodoAcademico,
            Anio = a.Anio?.ToString(CultureInfo.InvariantCulture),
            CodigoPublicacion = a.CodigoPublicacion,
            CodigoISSN = a.CodigoISSN,
            Titulo = a.Titulo,
            NombreRevista = a.NombreRevista,
            NumeroRevista = a.NumeroRevista,
            VolumenRevista = a.VolumenRevista,
            NumeroPaginas = a.NumeroPaginas?.ToString(CultureInfo.InvariantCulture),
            SJR = a.SJR?.ToString(CultureInfo.InvariantCulture),
            FechaPublicacion = a.FechaPublicacion?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            BaseDatos = a.BaseDatos,
            CampoAmplio = a.CampoAmplio,
            CampoEspecifico = a.CampoEspecifico,
            CampoDetallado = a.CampoDetallado,
            Quartil = a.Quartil,
            Filiacion = a.Filiacion,
            Estado = a.Estado,
            AccesoAbierto = a.AccesoAbierto,
            LinkPublicacion = a.LinkPublicacion,
            EnlaceRevista = a.EnlaceRevista,
            CodigoProyectoArticulado = a.CodigoProyectoArticulado,
            ProyectoArticulado = a.ProyectoArticulado,
            LineaInvestigacionArticulada = a.LineaInvestigacionArticulada,
            GrupoInvestigacionArticulado = a.GrupoInvestigacionArticulado
        };

        var ps = (a.Participantes ?? new List<ArticleParticipantDto>())
                 .OrderBy(p => p.Index)
                 .ToList();

        vm.Identificacion1 = ps.ElementAtOrDefault(0)?.Identificacion;
        vm.Nombre1 = ps.ElementAtOrDefault(0)?.Nombre;
        vm.Participacion1 = ps.ElementAtOrDefault(0)?.Participacion;

        vm.Identificacion2 = ps.ElementAtOrDefault(1)?.Identificacion;
        vm.Nombre2 = ps.ElementAtOrDefault(1)?.Nombre;
        vm.Participacion2 = ps.ElementAtOrDefault(1)?.Participacion;

        vm.Identificacion3 = ps.ElementAtOrDefault(2)?.Identificacion;
        vm.Nombre3 = ps.ElementAtOrDefault(2)?.Nombre;
        vm.Participacion3 = ps.ElementAtOrDefault(2)?.Participacion;

        vm.Identificacion4 = ps.ElementAtOrDefault(3)?.Identificacion;
        vm.Nombre4 = ps.ElementAtOrDefault(3)?.Nombre;
        vm.Participacion4 = ps.ElementAtOrDefault(3)?.Participacion;

        vm.Identificacion5 = ps.ElementAtOrDefault(4)?.Identificacion;
        vm.Nombre5 = ps.ElementAtOrDefault(4)?.Nombre;
        vm.Participacion5 = ps.ElementAtOrDefault(4)?.Participacion;

        return vm;
    }
}
