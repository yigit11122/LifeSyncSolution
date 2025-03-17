using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;


using Microsoft.EntityFrameworkCore;
using backend.models; // DbContext sýnýfýmýzý ekledik

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL baðlantýsýný yapýlandýralým
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
builder.Services.AddDbContext<LifeSyncDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers(); // API desteði ekleyelim
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
