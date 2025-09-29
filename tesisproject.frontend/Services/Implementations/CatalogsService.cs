using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations;

public class CatalogsService : ICatalogsService
{
    public string[] PeriodoOptions => new[] { "2025-I", "2025-II", "2026-I" };
    public string[] OpenAccessOptions => new[] { "SÍ", "NO" };
    public string[] EstadoOptions => new[] { "PUBLICADO", "ACEPTADO", "EN REVISIÓN", "RECHAZADO" };
    public string[] BaseDatosOptions => new[] { "SCOPUS", "WoS", "DOAJ", "Otro" };
    public string[] QuartilOptions => new[] { "Q1", "Q2", "Q3", "Q4" };

    public string[] CampoAmplioOptions => new[]
    {
        "Artes y humanidades",
        "Ciencias sociales, periodismo e información y derecho",
        "Administración",
        "Ciencias naturales, matemáticas y estadística",
        "Tecnologías de la información y la comunicación (TIC)",
        "Salud y bienestar",
        "Servicios"
    };

    public string[] CampoEspecificoOptions => new[]
    {
        "Artes","Humanidades","Idiomas",
        "Ciencias sociales y del comportamiento","Periodismo e información",
        "Ciencias biológicas y afines","Medio ambiente","Ciencias físicas","Matemáticas y estadística",
        "TIC","Ingeniería y profesiones afines","Industria y producción","Arquitectura y construcción",
        "Agricultura","Silvicultura","Pesca","Veterinaria",
        "Salud","Bienestar",
        "Servicios personales","Servicios de protección","Servicios de seguridad","Servicio de transporte"
    };

    public string[] CampoDetalladoOptions => new[]
    {
        "1-12A Técnicas audiovisuales y producción para medios de comunicación",
        "2-12A Diseño","3-12A Artes","5-12A Música y artes escénicas",
        "1-22A Religión y Teología","2-22A Historia y Arqueología","3-22A Filosofía",
        "1-32A Idiomas","2-32A Literatura y lingüística",
        "1-13A Economía","2-13A Ciencias políticas","3-13A Psicología","4-13A Estudios sociales y culturales",
        "1-15A Biología","2-15A Bioquímica","83-15A Biomedicina","84-15A Genética","86-15A Neurociencias",
        "1-35A Química","2-35A Ciencias de la Tierra",
        "1-45A Matemáticas","2-45A Estadísticas",
        "1-16A Computación","3-16A Desarrollo y análisis de software","81-16A Sistemas de Información",
        "1-17A Química aplicada","3-17A Electricidad y energía","4-17A Electrónica, automatización y sonido",
        "5-17A Mecánica","6-17A Vehículos, barcos y aeronaves","84-17A Telecomunicaciones","85-17A Nanotecnología",
        "1-27A Procesamiento de alimentos","3-27A Productos textiles","4-27A Minería y extracción",
        "5-27A Producción industrial","7-27A Diseño industrial y de procesos",
        "1-37A Arquitectura, urbanismo y restauración","2-37A Construcción e ingeniería civil",
        "1-18A Producción agrícola y ganadera","2-28A Silvicultura","1-38A Pesca","1-48A Veterinaria",
        "1-19A Odontología","3-19A Enfermería y obstetricia","4-19A Diagnóstico y tratamiento","6-19A Farmacia","8-19A Salud Pública",
        "2-110A Peluquería y belleza","3-110A Hotelería y gastronomía","5-110A Turismo",
        "1-210A Prevención y gestión de riesgos","2-210A Salud y seguridad ocupacional",
        "1-310A Educación policial, militar y defensa","2-310A Seguridad ciudadana",
        "1-410A Gestión del transporte","81-45A Logística y transporte"
    };
}
