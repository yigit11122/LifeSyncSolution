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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LifeSyncDbContext>();

    try
    {
        var taskCount = context.Tasks.Count();
        Console.WriteLine($"Database connection is successful. Tasks count: {taskCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}

app.Run();


