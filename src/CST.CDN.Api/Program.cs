using Microsoft.AspNetCore.Mvc;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
try
{
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Host.UseSerilog((ctx, logging) => 
    {
        logging.WriteTo.Console();
    });
    
    builder.Configuration.AddJsonFile(
        "appsettings.json", 
        optional: false, 
        reloadOnChange: false);
    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: false);
    
    builder.Configuration.AddEnvironmentVariables();
    
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    var storageConfiguration = builder.Configuration
        .GetSection("StorageConfiguration")
        .Get<StorageConfiguration>() ?? throw new InvalidOperationException("StorageConfiguration must be defined.");
    
    builder.Services.AddSingleton(storageConfiguration);
    
    var app = builder.Build();

    app.UseSerilogRequestLogging();
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.MapPost(
        "/", 
        async (
            HttpRequest request,
            string uploadPath,  
            StorageConfiguration storageConfiguration,
            CancellationToken cancellationToken) =>
        {
            var logger = request.HttpContext.RequestServices.GetRequiredService<Serilog.ILogger>();

            if (!request.HasFormContentType)
            {
                logger.Information("Request was not a Form, returning BadRequest");
                return Results.BadRequest("Request has to be Form");
            }
    
            if (request.Form.Files.Count == 0)
            {
                logger.Information("Request did not contain a file, returning BadRequest");
                return Results.BadRequest("Request has to contain a file");
            }

            if (string.IsNullOrWhiteSpace(uploadPath))
            {
                logger.Information("UploadPath is null or whitespace, returning BadRequest");
                return Results.BadRequest("UploadPath has to be defined and non-empty");
            }
    
            var formFile = request.Form.Files[0];
    
            var directoryPath = Path.Combine(
                storageConfiguration.Path,
                uploadPath);
    
            var path = Path.Combine(directoryPath, formFile.FileName);
    
            if (File.Exists(path))
            {
                logger.Information("Path to be used already exists, returning BadRequset");
                return Results.BadRequest();
            }
    
            if (!Directory.Exists(directoryPath))
            {
                logger.Information("Directory does not exist, creating it");
                Directory.CreateDirectory(directoryPath);
            }
    
            using var targetStream = new FileStream(path, FileMode.CreateNew);
    
            await formFile.CopyToAsync(targetStream, cancellationToken);

            logger.Information("File saved, returning Ok");
            
            return Results.Ok();
        })
    .DisableAntiforgery()
    .WithName("Upload")
    .WithOpenApi();
    
    app.Run();
    
}
catch (System.Exception)
{
    
    throw;
}
record StorageConfiguration(
    string Path
);