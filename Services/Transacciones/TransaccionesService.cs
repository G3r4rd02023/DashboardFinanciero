using DashboardFinanciero.Data;
using DashboardFinanciero.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanciero.Services.Transacciones;

public sealed class TransaccionesService(IDbContextFactory<FinanzasDbContext> contextFactory) : ITransaccionesService
{
    public async Task<ResultadoPaginaTransacciones> ListarAsync(
        FiltroTransacciones filtro,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.Transacciones
            .AsNoTracking()
            .Include(transaccion => transaccion.Categoria)
            .AsQueryable();

        if (filtro.FechaDesde.HasValue)
        {
            query = query.Where(transaccion => transaccion.Fecha >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            query = query.Where(transaccion => transaccion.Fecha <= filtro.FechaHasta.Value);
        }

        if (filtro.Tipo.HasValue)
        {
            query = query.Where(transaccion => transaccion.Tipo == filtro.Tipo.Value);
        }

        if (filtro.CategoriaId.HasValue)
        {
            query = query.Where(transaccion => transaccion.CategoriaId == filtro.CategoriaId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var pagina = Math.Max(filtro.Pagina, 1);
        var elementosPorPagina = Math.Clamp(filtro.ElementosPorPagina, 1, 100);
        var items = await query
            .OrderByDescending(transaccion => transaccion.Fecha)
            .ThenByDescending(transaccion => transaccion.Id)
            .Skip((pagina - 1) * elementosPorPagina)
            .Take(elementosPorPagina)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginaTransacciones(items, total);
    }

    public async Task<IReadOnlyList<Categoria>> ListarCategoriasAsync(
        TipoTransaccion? tipo = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.Categorias.AsNoTracking();

        if (tipo.HasValue)
        {
            query = query.Where(categoria => categoria.Tipo == tipo.Value);
        }

        return await query.OrderBy(categoria => categoria.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Transaccion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Transacciones
            .AsNoTracking()
            .SingleOrDefaultAsync(transaccion => transaccion.Id == id, cancellationToken);
    }

    public async Task GuardarAsync(TransaccionFormModel modelo, CancellationToken cancellationToken = default)
    {
        if (!modelo.Fecha.HasValue || !modelo.Monto.HasValue || !modelo.CategoriaId.HasValue
            || modelo.Monto.Value <= 0 || modelo.Monto.Value > 9999999999999999.99m
            || string.IsNullOrWhiteSpace(modelo.Descripcion) || modelo.Descripcion.Trim().Length > 250)
        {
            throw new InvalidOperationException("Completa los campos obligatorios de la transacción.");
        }

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categoria = await dbContext.Categorias.SingleOrDefaultAsync(
            item => item.Id == modelo.CategoriaId.Value && item.Tipo == modelo.Tipo,
            cancellationToken);

        if (categoria is null)
        {
            throw new InvalidOperationException("La categoría seleccionada no corresponde al tipo de transacción.");
        }

        Transaccion transaccion;
        if (modelo.Id == 0)
        {
            transaccion = new Transaccion
            {
                CategoriaId = categoria.Id,
                Categoria = categoria,
                Descripcion = string.Empty
            };
            dbContext.Transacciones.Add(transaccion);
        }
        else
        {
            transaccion = await dbContext.Transacciones.FindAsync([modelo.Id], cancellationToken)
                ?? throw new KeyNotFoundException("La transacción ya no existe.");
            transaccion.Categoria = categoria;
            transaccion.CategoriaId = categoria.Id;
        }

        transaccion.Fecha = modelo.Fecha.Value;
        transaccion.Monto = modelo.Monto.Value;
        transaccion.Tipo = modelo.Tipo;
        transaccion.Descripcion = modelo.Descripcion.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transaccion = await dbContext.Transacciones.FindAsync([id], cancellationToken);
        if (transaccion is null)
        {
            return false;
        }

        dbContext.Transacciones.Remove(transaccion);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}