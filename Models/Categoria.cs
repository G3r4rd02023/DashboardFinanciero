namespace DashboardFinanciero.Models;

public class Categoria
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public TipoTransaccion Tipo { get; set; }

    public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
}