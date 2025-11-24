
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Sockets;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace OpenTelemetryDemo
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);
      //
      // Configure Serilog with OTLP exporter
      Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.WithProperty("service.name", "myservice")
        .WriteTo.OpenTelemetry(o =>
        {
          o.Protocol = OtlpProtocol.Grpc;
          o.Endpoint = "http://localhost:4317";
          o.ResourceAttributes = new Dictionary<string, object>
          {
            ["service.name"] = "myservice",
            ["service.version"] = "1.0.0"
          };
        })
        .CreateLogger();
      builder.Logging.ClearProviders();
      builder.Logging.AddSerilog();
      //
      // Add services to the container.

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      var serviceName = "CoffeeShop-api";
      var serviceVersion = "1.0.0";
     
      //builder.Logging.ClearProviders();

      //builder.Logging.AddOpenTelemetry(loggerOption =>
      //{
      //  loggerOption.IncludeScopes = true;
      //  loggerOption.ParseStateValues = true;
      //  loggerOption.IncludeFormattedMessage = true;
      //  loggerOption.AddOtlpExporter(option =>
      //  {
      //    option.Endpoint = new Uri("http://localhost:4317");
      //  });
      //});
      builder.Services.AddOpenTelemetry()
        .ConfigureResource(resourceBuilder => resourceBuilder.AddService(serviceName, serviceVersion: serviceVersion))
        
        .WithLogging(loggerOption => 
          loggerOption.AddOtlpExporter(option => { option.Endpoint = new Uri("http://localhost:4317"); }))
       
        .WithTracing(tracing => 
          tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
            .AddOtlpExporter(option => { option.Endpoint = new Uri("http://localhost:4317"); }))
       
        .WithMetrics(metrics =>
          {
            metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddHttpClientInstrumentation()
              .AddOtlpExporter(option => { option.Endpoint = new Uri("http://localhost:4317"); });
          }); 

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment())
      {
        app.UseSwagger();
        app.UseSwaggerUI();
      }

      app.UseHttpsRedirection();

      app.UseAuthorization();


      app.MapControllers();

      app.Run();
    }
  }
}
