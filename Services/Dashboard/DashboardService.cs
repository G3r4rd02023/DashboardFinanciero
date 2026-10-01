using DashboardFinanciero.Data;
using DashboardFinanciero.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanciero.Services.Dashboard;

public sealed class DashboardService(IDbContextFactory<FinanzasDbContext> contextFactory) : IDashboardService
{
    private sealed record AgregadoDiario(DateOnly Fecha, TipoTransaccion Tipo, decimal Total);

    private sealed record AgregadoMensual(int Anio, int Mes, TipoTransaccion Tipo, decimal Total);

    private sealed record AgregadoCategoria(int CategoriaId, string Categoria, decimal Total);

    public async Task<ResumenKpis> ObtenerKpisAsync(
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default)
    {
        if (fechaDesde > fechaHasta)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var duracion = fechaHasta.DayNumber - fechaDesde.DayNumber + 1;
        var periodoAnteriorHasta = fechaDesde.AddDays(-1);
        var periodoAnteriorDesde = periodoAnteriorHasta.AddDays(-duracion + 1);

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var totalesActuales = await ObtenerTotalesAsync(dbContext, fechaDesde, fechaHasta, cancellationToken);
        var totalesAnteriores = await ObtenerTotalesAsync(
            dbContext,
            periodoAnteriorDesde,
            periodoAnteriorHasta,
            cancellationToken);

        var ingresos = ObtenerTotal(totalesActuales, TipoTransaccion.Ingreso);
        var egresos = ObtenerTotal(totalesActuales, TipoTransaccion.Egreso);
        var balance = ingresos - egresos;
        var ingresosAnteriores = ObtenerTotal(totalesAnteriores, TipoTransaccion.Ingreso);
        var egresosAnteriores = ObtenerTotal(totalesAnteriores, TipoTransaccion.Egreso);
        var balanceAnterior = ingresosAnteriores - egresosAnteriores;
        decimal? margen = ingresos == 0 ? null : balance / ingresos * 100;
        decimal? margenAnterior = ingresosAnteriores == 0 ? null : balanceAnterior / ingresosAnteriores * 100;
        var comparacionDisponible = ingresosAnteriores > 0;

        return new ResumenKpis(
            fechaDesde,
            fechaHasta,
            periodoAnteriorDesde,
            periodoAnteriorHasta,
            ingresos,
            egresos,
            balance,
            margen,
            comparacionDisponible,
            CrearComparativo(ingresos, ingresosAnteriores, true, comparacionDisponible),
            CrearComparativo(egresos, egresosAnteriores, false, comparacionDisponible),
            CrearComparativo(balance, balanceAnterior, true, comparacionDisponible),
            margen.HasValue && margenAnterior.HasValue
                ? CrearComparativo(margen.Value, margenAnterior.Value, true, comparacionDisponible)
                : new ComparativoKpi(null, DireccionComparativa.Igual, true, false));
    }

    public async Task<GraficosDashboard> ObtenerGraficosAsync(
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken = default)
    {
        if (fechaDesde > fechaHasta)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        }

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var movimientos = dbContext.Transacciones
            .AsNoTracking()
            .Where(transaccion => transaccion.Fecha >= fechaDesde && transaccion.Fecha <= fechaHasta);

        var agregadosMensuales = await movimientos
            .GroupBy(transaccion => new { transaccion.Fecha.Year, transaccion.Fecha.Month, transaccion.Tipo })
            .Select(grupo => new AgregadoMensual(
                grupo.Key.Year,
                grupo.Key.Month,
                grupo.Key.Tipo,
                grupo.Sum(transaccion => transaccion.Monto)))
            .ToListAsync(cancellationToken);

        var inicioMes = new DateOnly(fechaDesde.Year, fechaDesde.Month, 1);
        var finMes = new DateOnly(fechaHasta.Year, fechaHasta.Month, 1);
        var resumenMensual = new List<BarraMensual>();
        for (var mes = inicioMes; mes <= finMes; mes = mes.AddMonths(1))
        {
            var ingresos = agregadosMensuales
                .Where(agregado => agregado.Anio == mes.Year && agregado.Mes == mes.Month && agregado.Tipo == TipoTransaccion.Ingreso)
                .Select(agregado => agregado.Total)
                .FirstOrDefault();
            var egresos = agregadosMensuales
                .Where(agregado => agregado.Anio == mes.Year && agregado.Mes == mes.Month && agregado.Tipo == TipoTransaccion.Egreso)
                .Select(agregado => agregado.Total)
                .FirstOrDefault();
            resumenMensual.Add(new BarraMensual(mes.Year, mes.Month, ingresos, egresos));
        }

        var filasDiarias = await movimientos
            .GroupBy(transaccion => new { transaccion.Fecha, transaccion.Tipo })
            .Select(grupo => new
            {
                grupo.Key.Fecha,
                grupo.Key.Tipo,
                Total = grupo.Sum(transaccion => transaccion.Monto)
            })
            .OrderBy(agregado => agregado.Fecha)
            .ToListAsync(cancellationToken);
        var agregadosDiarios = filasDiarias
            .Select(fila => new AgregadoDiario(fila.Fecha, fila.Tipo, fila.Total))
            .ToList();

        var flujoAcumulado = new List<PuntoFlujoCaja>();
        var balanceAcumulado = 0m;
        flujoAcumulado.Add(new PuntoFlujoCaja(fechaDesde, balanceAcumulado));
        foreach (var agregadoDiario in agregadosDiarios.GroupBy(agregado => agregado.Fecha).OrderBy(grupo => grupo.Key))
        {
            var ingresosDiarios = agregadoDiario
                .Where(agregado => agregado.Tipo == TipoTransaccion.Ingreso)
                .Sum(agregado => agregado.Total);
            var egresosDiarios = agregadoDiario
                .Where(agregado => agregado.Tipo == TipoTransaccion.Egreso)
                .Sum(agregado => agregado.Total);
            balanceAcumulado += ingresosDiarios - egresosDiarios;
            flujoAcumulado.Add(new PuntoFlujoCaja(agregadoDiario.Key, balanceAcumulado));
        }

        if (fechaHasta > flujoAcumulado[^1].Fecha)
        {
            flujoAcumulado.Add(new PuntoFlujoCaja(fechaHasta, balanceAcumulado));
        }

        var totalesPorCategoria = await movimientos
            .Where(transaccion => transaccion.Tipo == TipoTransaccion.Egreso)
            .GroupBy(transaccion => transaccion.CategoriaId)
            .Select(grupo => new
            {
                CategoriaId = grupo.Key,
                Total = grupo.Sum(transaccion => transaccion.Monto)
            })
            .OrderByDescending(agregado => agregado.Total)
            .ToListAsync(cancellationToken);
        var idsCategoria = totalesPorCategoria.Select(agregado => agregado.CategoriaId).ToArray();
        var nombresCategoria = await dbContext.Categorias
            .AsNoTracking()
            .Where(categoria => idsCategoria.Contains(categoria.Id))
            .ToDictionaryAsync(categoria => categoria.Id, categoria => categoria.Nombre, cancellationToken);
        var egresosPorCategoria = totalesPorCategoria
            .Select(agregado => new AgregadoCategoria(
                agregado.CategoriaId,
                nombresCategoria[agregado.CategoriaId],
                agregado.Total))
            .ToList();

        return new GraficosDashboard(
            resumenMensual,
            flujoAcumulado,
            egresosPorCategoria
                .Select(agregado => new EgresoPorCategoria(agregado.CategoriaId, agregado.Categoria, agregado.Total))
                .ToList(),
            agregadosDiarios.Count > 0);
    }

    private static async Task<Dictionary<TipoTransaccion, decimal>> ObtenerTotalesAsync(
        FinanzasDbContext dbContext,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        CancellationToken cancellationToken)
    {
        var grupos = await dbContext.Transacciones
            .AsNoTracking()
            .Where(transaccion => transaccion.Fecha >= fechaDesde && transaccion.Fecha <= fechaHasta)
            .GroupBy(transaccion => transaccion.Tipo)
            .Select(grupo => new { Tipo = grupo.Key, Total = grupo.Sum(transaccion => transaccion.Monto) })
            .ToListAsync(cancellationToken);

        return grupos.ToDictionary(grupo => grupo.Tipo, grupo => grupo.Total);
    }

    private static decimal ObtenerTotal(
        IReadOnlyDictionary<TipoTransaccion, decimal> totales,
        TipoTransaccion tipo) => totales.GetValueOrDefault(tipo);

    private static ComparativoKpi CrearComparativo(
        decimal actual,
        decimal anterior,
        bool aumentoEsFavorable,
        bool disponible)
    {
        if (!disponible)
        {
            return new ComparativoKpi(null, DireccionComparativa.Igual, true, false);
        }

        var direccion = actual.CompareTo(anterior) switch
        {
            > 0 => DireccionComparativa.Superior,
            < 0 => DireccionComparativa.Inferior,
            _ => DireccionComparativa.Igual
        };
        decimal? cambio = anterior == 0 ? null : (actual - anterior) / Math.Abs(anterior) * 100;
        var favorable = direccion == DireccionComparativa.Igual
            || (direccion == DireccionComparativa.Superior) == aumentoEsFavorable;

        return new ComparativoKpi(cambio, direccion, favorable, true);
    }
}