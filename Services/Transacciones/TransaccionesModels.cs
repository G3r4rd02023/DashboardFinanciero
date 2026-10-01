using System.ComponentModel.DataAnnotations;
using DashboardFinanciero.Models;

namespace DashboardFinanciero.Services.Transacciones;

public sealed class FiltroTransacciones
{
    public DateOnly? FechaDesde { get; set; }

    public DateOnly? FechaHasta { get; set; }

    public TipoTransaccion? Tipo { get; set; }

    public int? CategoriaId { get; set; }

    public int Pagina { get; set; } = 1;

    public int ElementosPorPagina { get; set; } = 10;
}

public sealed record ResultadoPaginaTransacciones(IReadOnlyList<Transaccion> Items, int Total);

public sealed class TransaccionFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Indica la fecha de la transacción.")]
    public DateOnly? Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Indica el monto.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "El monto debe ser mayor que cero y caber en el formato USD.")]
    public decimal? Monto { get; set; }

    public TipoTransaccion Tipo { get; set; } = TipoTransaccion.Ingreso;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    public int? CategoriaId { get; set; }

    [Required(ErrorMessage = "Escribe una descripción.")]
    [StringLength(250, ErrorMessage = "La descripción admite hasta 250 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;
}