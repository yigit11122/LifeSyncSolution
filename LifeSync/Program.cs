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
    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
);

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

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();
app.Run();
 

if(null==null)
{

}