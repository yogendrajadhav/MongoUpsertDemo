
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace APILoggingDemo
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();
      
      //Serilog
      builder.Host.UseSerilog((context, config) =>
      {
      config.Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
      {
          AutoRegisterTemplate = true,
          IndexFormat= $"myapp-logs-{DateTime.UtcNow:yyyy.MM.dd}"
      });
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
