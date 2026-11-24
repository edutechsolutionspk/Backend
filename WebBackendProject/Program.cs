using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebBackendProject.business.assessment.services;
using WebBackendProject.business.practice_files.services;
using WebBackendProject.Data;

[CompilerGenerated]
internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.Configure((FormOptions options) =>
        {
            options.MultipartBodyLengthLimit = 500000000L;
        });
        builder.Services.AddCors((CorsOptions options) =>
        {
            options.AddPolicy("AllowAll", (CorsPolicyBuilder policy) =>
            {
                policy.WithOrigins("https://edutechsolutionpk.com", "https://backend.edutechsolutionpk.com", "http://127.0.0.1:5500").AllowAnyMethod().AllowAnyHeader()
                    .WithExposedHeaders("Content-Disposition");
            });
        });
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen((SwaggerGenOptions c) =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WebBackendProject API",
                Version = "v1"
            });
        });
        builder.Services.AddDbContext<ApplicationDbContext>((DbContextOptionsBuilder options) =>
        {
            options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 30)));
        });
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddEnvironmentVariables();
        builder.Services.AddScoped<AddPracticeFilesService>();
        builder.Services.AddScoped<ListPracticeFiles>();
        builder.Services.AddScoped<DeletePracticeFileService>();
        builder.Services.AddScoped<AddAssessmentService>();
        builder.Services.AddScoped<ListAssessmentService>();
        builder.Services.AddScoped<DeleteAssessmentService>();

        WebApplication app = builder.Build();
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var response = new
                {
                    StatusCode = 500,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    ExceptionType = ex.GetType().Name
                };
                string json = JsonSerializer.Serialize(response);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(json);
            }
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.UseStaticFiles();
        string basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploadedfiles");
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
        FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".apk"] = "application/vnd.android.package-archive";
        provider.Mappings[".swf"] = "application/x-shockwave-flash";
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(basePath),
            RequestPath = "/uploadedfiles",
            ContentTypeProvider = provider
        });
        app.MapControllers();
        app.Run();
    }
}
