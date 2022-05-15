using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue;
using Tgs.Promotion.Core.MessageQueue;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((_, builder) => builder
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables());

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddOptions<OtlpExporterOptions>()
    .Configure<IConfiguration>((options, conifg) =>
    {
        var nodeIp = conifg.GetValue<string>("NODE_IP");

        if (!string.IsNullOrWhiteSpace(nodeIp))
            options.Endpoint = new Uri($"http://{nodeIp}:4317");

        conifg.GetSection("Opentelemetry:Tracing").Bind(options);
    });

builder.Services
    .AddOpenTelemetryTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue
            || !httpContext.Request.Path.StartsWithSegments("/swagger")
            && !httpContext.Request.Path.StartsWithSegments("/health")
            && !httpContext.Request.Path.StartsWithSegments("/profiler/")
            && !new[] { "html", "ico", "png", "jpg" }.Any(
                ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services.AddProfileViewer();

builder.Services
	.AddTgsRepository(builder.Configuration.GetConnectionString("Tgs"))
	.AddPromotionCore()
	.AddPaymentServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.PaymentServiceBaseUrl = config.GetValue<Uri>("Promotion:PaymentServiceBaseUrl");
	})
	.AddCashServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Promotion:CashBaseUrl");
	})
	.AddTgsLog();

builder.Services
	.AddOptions<RedisOptions>()
	.Configure<IConfiguration>((options, config)
		=> options.ConnectionString = config.GetConnectionString("Redis"))
	.Services
	.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

builder.Services
.AddMessageQueue()
	.AddNatsMessageQueue(config =>
	{
		config
			.ConfigQueueOptions((options, sp) =>
			{
				var configuration = sp.GetRequiredService<IConfiguration>();

				options.Url = configuration.GetConnectionString("Nats");
			})
			.AddHandler<DepositStatusChangedMessageHandler>(
				"tgs.payment.deposit.status",
				"tgs.payment.deposit.status");
	});

builder.Services.AddHealthChecks();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
	ExceptionHandler = async context =>
	{
		var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
		if (exceptionFeature?.Error != null)
		{
			var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("ExceptionHandler");

			logger.LogError(
				exceptionFeature.Error,
				"{exceptionFeature.Error.Message}",
				exceptionFeature.Error.Message);

			await context.Response.WriteAsJsonAsync(new MessageResponse
			{
				Message = exceptionFeature.Error.Message,
				Valid = false
			});
		}
	}
});

// Configure the HTTP request pipeline.
app.UseSwaggerUI();

app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();
