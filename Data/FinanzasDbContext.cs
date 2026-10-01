using DashboardFinanciero.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanciero.Data;

public class FinanzasDbContext(DbContextOptions<FinanzasDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Transaccion> Transacciones => Set<Transaccion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias");
            entity.HasKey(categoria => categoria.Id);
            entity.Property(categoria => categoria.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(categoria => categoria.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.HasAlternateKey(categoria => new { categoria.Id, categoria.Tipo });
        });

        modelBuilder.Entity<Transaccion>(entity =>
        {
            entity.ToTable("Transacciones");
            entity.HasKey(transaccion => transaccion.Id);
            entity.Property(transaccion => transaccion.Fecha).IsRequired();
            entity.Property(transaccion => transaccion.Monto).HasPrecision(18, 2).IsRequired();
            entity.Property(transaccion => transaccion.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.Property(transaccion => transaccion.Descripcion).HasMaxLength(250).IsRequired();
            entity.HasIndex(transaccion => transaccion.Fecha);
            entity.HasIndex(transaccion => transaccion.CategoriaId);
            entity.HasOne(transaccion => transaccion.Categoria)
                .WithMany(categoria => categoria.Transacciones)
                .HasForeignKey(transaccion => new { transaccion.CategoriaId, transaccion.Tipo })
                .HasPrincipalKey(categoria => new { categoria.Id, categoria.Tipo })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}