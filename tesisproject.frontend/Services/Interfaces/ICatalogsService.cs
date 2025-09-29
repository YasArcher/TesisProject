namespace tesisproject.frontend.Services.Interfaces;

public interface ICatalogsService
{
    string[] PeriodoOptions { get; }
    string[] OpenAccessOptions { get; }
    string[] EstadoOptions { get; }
    string[] BaseDatosOptions { get; }
    string[] QuartilOptions { get; }
    string[] CampoAmplioOptions { get; }
    string[] CampoEspecificoOptions { get; }
    string[] CampoDetalladoOptions { get; }
}
