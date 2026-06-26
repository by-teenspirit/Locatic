using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Interfaces;
using Locatic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<LocaticDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IVoitureService, VoitureService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LocaticDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Une erreur est survenue lors du seed de la base : {ex.Message}");
    }
}

app.Run();

