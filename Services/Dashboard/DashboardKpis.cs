namespace DashboardFinanciero.Services.Dashboard;

public enum PeriodoDashboard
{
    MesActual,
    TrimestreActual,
    AnioActual,
    Personalizado
}

public enum DireccionComparativa
{
    Igual,
    Superior,
    Inferior
}

public sealed record ComparativoKpi(
    decimal? CambioPorcentual,
    DireccionComparativa Direccion,
    bool Favorable,
    bool Calculable);

public sealed record ResumenKpis(
    DateOnly FechaDesde,
    DateOnly FechaHasta,
    DateOnly PeriodoAnteriorDesde,
    DateOnly PeriodoAnteriorHasta,
    decimal TotalIngresos,
    decimal TotalEgresos,
    decimal BalanceNeto,
    decimal? MargenUtilidad,
    bool ComparacionDisponible,
    ComparativoKpi ComparativoIngresos,
    ComparativoKpi ComparativoEgresos,
    ComparativoKpi ComparativoBalance,
    ComparativoKpi ComparativoMargen);

public sealed record BarraMensual(int Anio, int Mes, decimal Ingresos, decimal Egresos);

public sealed record PuntoFlujoCaja(DateOnly Fecha, decimal BalanceAcumulado);

public sealed record EgresoPorCategoria(int CategoriaId, string Categoria, decimal Total);

public sealed record GraficosDashboard(
    IReadOnlyList<BarraMensual> IngresosEgresosPorMes,
    IReadOnlyList<PuntoFlujoCaja> FlujoCajaAcumulado,
    IReadOnlyList<EgresoPorCategoria> EgresosPorCategoria,
    bool TieneMovimientos);