using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CST.CDN.Client;

public record CdnClientConfiguration(string ServiceAddress);
public record FileUpload(string Path, string FileName, Stream FileContent);
public record RandomNameFileUpload(string Path, Stream FileContent);

public static class ServiceCollectionExtensions
{
    private const string ConfigurationSectionName = "CdnClient";
    public static IServiceCollection AddCdnClient(this IServiceCollection services)
    {
        services.AddSingleton(sp => 
        {
            var configuration = sp.GetRequiredService<IConfiguration>();

            var cdnClientConfiguration = configuration.GetSection(ConfigurationSectionName).Get<CdnClientConfiguration>() 
                ?? throw new InvalidOperationException($"{ConfigurationSectionName} should be defined.");

            return cdnClientConfiguration;
        });

        services.AddScoped<ICdnClient, CdnClient>();

        return services;
    }
}


public interface ICdnClient
{
    Task UploadFileAsync(FileUpload fileUpload, CancellationToken cancellationToken);

    Task<string> UploadFileWithRandomNameAsync(RandomNameFileUpload fileUpload, CancellationToken cancellationToken);
}

internal class CdnClient(CdnClientConfiguration cdnClientConfiguration) : ICdnClient
{
    private const string uploadPathParamName = "uploadPath";
    private readonly CdnClientConfiguration _cdnClientConfiguration = cdnClientConfiguration;

    public async Task UploadFileAsync(FileUpload fileUpload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUpload.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUpload.FileName);
        ArgumentNullException.ThrowIfNull(fileUpload.FileContent);

        await UploadFileAsync(fileUpload.Path, fileUpload.FileName, fileUpload.FileContent, cancellationToken);
    }

    public async Task<string> UploadFileWithRandomNameAsync(RandomNameFileUpload fileUpload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUpload.Path);
        ArgumentNullException.ThrowIfNull(fileUpload.FileContent);

        var fileName = Guid.NewGuid().ToString();

        await UploadFileAsync(fileUpload.Path, fileName, fileUpload.FileContent, cancellationToken);

        return fileName;
    }

    private async Task UploadFileAsync(string path, string fileName, Stream fileContent, CancellationToken cancellationToken)
    {
        fileContent.Seek(0, SeekOrigin.Begin);

        await _cdnClientConfiguration.ServiceAddress
            .SetQueryParam(uploadPathParamName, path)
            .PostMultipartAsync(contentBuilder => 
            {
                contentBuilder.AddFile(fileName, fileContent, fileName);
            }, cancellationToken: cancellationToken);
    }
}
