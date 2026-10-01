using DashboardFinanciero.Models;

namespace DashboardFinanciero.Services.Transacciones;

public interface ITransaccionesService
{
    Task<ResultadoPaginaTransacciones> ListarAsync(FiltroTransacciones filtro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Categoria>> ListarCategoriasAsync(TipoTransaccion? tipo = null, CancellationToken cancellationToken = default);

    Task<Transaccion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task GuardarAsync(TransaccionFormModel modelo, CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}