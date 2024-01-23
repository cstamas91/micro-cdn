using CST.CDN.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder();
configuration.AddJsonFile("appsettings.json");

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IConfiguration>(configuration.Build());

serviceCollection.AddCdnClient();

var serviceProvider = serviceCollection.BuildServiceProvider();

var cdnClient = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<ICdnClient>();

string path = "test";
string? fileName = null;
Stream content = new MemoryStream();
if (args.Length > 0)
{
    path = args[0];
}

if (args.Length > 1)
{
    fileName = args[1];
}

if (args.Length > 2)
{
    await File.OpenRead(args[2]).CopyToAsync(content);
}

if (fileName is null)
{
    fileName = await cdnClient.UploadFileWithRandomNameAsync(new RandomNameFileUpload(path, content), default);
}
else
{
    await cdnClient.UploadFileAsync(new FileUpload(path, fileName, content), default);
}

Console.WriteLine(fileName);