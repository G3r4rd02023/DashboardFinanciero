using DashboardFinanciero.Components;
using DashboardFinanciero.Data;
using DashboardFinanciero.Data.Seeding;
using DashboardFinanciero.Services.Dashboard;
using DashboardFinanciero.Services.Transacciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnectionString = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("Finanzas"));
if (!Path.IsPathRooted(sqliteConnectionString.DataSource))
{
    sqliteConnectionString.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteConnectionString.DataSource);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<FinanzasDbContext>(options =>
    options.UseSqlite(sqliteConnectionString.ToString()));
builder.Services.AddScoped<ITransaccionesService, TransaccionesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FinanzasDbContext>>();
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        var seeded = await DemoDataSeeder.SeedAsync(dbContext);
        app.Logger.LogInformation(seeded
            ? "Demo seed inserted 8 categories and 240 synthetic transactions."
            : "Demo seed skipped because the database already contains data.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
