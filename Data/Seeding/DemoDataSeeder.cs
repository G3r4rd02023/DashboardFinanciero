using Bogus;
using DashboardFinanciero.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanciero.Data.Seeding;

public static class DemoDataSeeder
{
    private const int Seed = 20260930;
    private const int TransactionCount = 240;

    public static async Task<bool> SeedAsync(FinanzasDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categorias.AnyAsync(cancellationToken)
            || await dbContext.Transacciones.AnyAsync(cancellationToken))
        {
            return false;
        }

        var categoryFaker = new Faker("es")
        {
            Random = new Randomizer(Seed)
        };

        var incomeNames = new[] { "Salario", "Ventas", "Servicios profesionales", "Inversiones" };
        var expenseNames = new[] { "Vivienda", "Alimentación", "Transporte", "Servicios" };
        categoryFaker.Random.Shuffle(incomeNames);
        categoryFaker.Random.Shuffle(expenseNames);

        var categories = incomeNames
            .Select((name, index) => new Categoria
            {
                Id = index + 1,
                Nombre = name,
                Tipo = TipoTransaccion.Ingreso
            })
            .Concat(expenseNames.Select((name, index) => new Categoria
            {
                Id = incomeNames.Length + index + 1,
                Nombre = name,
                Tipo = TipoTransaccion.Egreso
            }))
            .ToList();

        var transactionFaker = new Faker("es")
        {
            Random = new Randomizer(Seed)
        };
        var today = DateOnly.FromDateTime(DateTime.Today);
        var transactions = Enumerable.Range(0, TransactionCount)
            .Select(_ =>
            {
                var type = transactionFaker.Random.Bool(0.65f)
                    ? TipoTransaccion.Ingreso
                    : TipoTransaccion.Egreso;
                var matchingCategories = categories.Where(category => category.Tipo == type).ToArray();
                var category = transactionFaker.PickRandom(matchingCategories);

                return new Transaccion
                {
                    Fecha = today.AddDays(-transactionFaker.Random.Int(0, 364)),
                    Monto = decimal.Round(transactionFaker.Random.Decimal(15, 4800), 2),
                    Tipo = type,
                    CategoriaId = category.Id,
                    Categoria = category,
                    Descripcion = transactionFaker.Lorem.Sentence(4)
                };
            })
            .ToList();

        dbContext.Categorias.AddRange(categories);
        dbContext.Transacciones.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}