using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Invocation;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureGeneratedFunctionMetadataProvider()
    .ConfigureServices(s =>
    {
        s.AddSingleton<IFunctionExecutor, DirectFunctionExecutor>();
    })
    
    .Build();

host.Run();
