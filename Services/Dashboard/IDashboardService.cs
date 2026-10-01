namespace DashboardFinanciero.Services.Dashboard;

public interface IDashboardService
{
    Task<ResumenKpis> ObtenerKpisAsync(
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default);

    Task<GraficosDashboard> ObtenerGraficosAsync(
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default);
}