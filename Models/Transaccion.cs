namespace DashboardFinanciero.Models;

public class Transaccion
{
    public int Id { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal Monto { get; set; }

    public TipoTransaccion Tipo { get; set; }

    public int CategoriaId { get; set; }

    public required string Descripcion { get; set; }

    public Categoria Categoria { get; set; } = null!;
}