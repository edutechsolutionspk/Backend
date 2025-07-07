using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using WebBackendProject.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure Large File Upload Limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500_000_000; // 500MB
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                "https://edutechsolutionpk.com",
                "https://backend.edutechsolutionpk.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
               .WithExposedHeaders("Content-Disposition"));
           
});


// Add Controllers
builder.Services.AddControllers();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebBackendProject API", Version = "v1" });
});

// Configure MySQL Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30)))); // Adjust MySQL version if needed

// Load Configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var app = builder.Build();

// Global Exception Handling Middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var response = new
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            ExceptionType = ex.GetType().Name
        };

        var json = JsonSerializer.Serialize(response);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsync(json);
    }
});

// Enable Swagger only in Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTPS Redirection
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Enable Authorization
app.UseAuthorization();

// Serve Static Files
app.UseStaticFiles();

// Configure File Upload Directory
var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploadedfiles");
if (!Directory.Exists(basePath))
{
    Directory.CreateDirectory(basePath);
}

// Serve Uploaded Files
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".apk"] = "application/vnd.android.package-archive";
provider.Mappings[".swf"] = "application/x-shockwave-flash";
// Example for custom file types

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(basePath),
    RequestPath = "/uploadedfiles",
    ContentTypeProvider = provider
});

// Map Controllers
app.MapControllers();

// Run the Application
app.Run();