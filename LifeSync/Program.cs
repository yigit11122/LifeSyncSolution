using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using backend.models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// PostgreSQL baðlantý þemasý ekle
var connectionString = builder.Configuration.GetConnectionString("LifeSyncDbContext") ??
    throw new InvalidOperationException("Connection string 'LifeSyncDbContext' not found.");
builder.Services.AddDbContext<LifeSyncDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// CORS ayarlarý (OAuth için gerekli olabilir)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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