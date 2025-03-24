using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using backend.models;
using Npgsql.EntityFrameworkCore.PostgreSQL; // 🔥 Burası kritik

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🌐 PostgreSQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("LifeSyncDbContext")
    ?? throw new InvalidOperationException("Connection string 'LifeSyncDbContext' not found.");

builder.Services.AddDbContext<LifeSyncDbContext>(options =>
    options.UseNpgsql(connectionString));


// 🌐 CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LifeSyncDbContext>();

    try
    {
        var canConnect = context.Database.CanConnect();
        Console.WriteLine(canConnect ? "Veritabanı bağlantısı başarılı." : "Veritabanına bağlanılamadı.");

        var taskCount = context.Tasks.Count();
        Console.WriteLine($"Tasks tablosunda {taskCount} kayıt var.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Veritabanı bağlantı kontrolünde hata: {ex.Message}");
    }
}

app.Run();
 //aa

if(null==null)
{

}