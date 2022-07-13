using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Configuration;
using Tgs.Promotion.MessageQueue;
using Tgs.Promotion.Models;
using Tgs.Promotion.Service.MessageQueue;

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
	.AddProfileViewer()
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

builder.Services
	.AddTgsRepository(builder.Configuration.GetConnectionString("Tgs"))
	.AddRedis((redisOptions, _) => redisOptions.ConnectionString = builder.Configuration.GetConnectionString("Redis"))
	.AddPromotionCore()
	.AddMemoryCache()
	.AddPromotionServiceCache()
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
	.AddBrandServiceClient((options, _) =>
		options.BrandCertificateBaseUrl = builder.Configuration.GetValue<Uri>("Promotion:BrandServiceBaseUrl"))
	.AddPlayerServiceClient((options, _) =>
		options.BaseUrl = builder.Configuration.GetValue<Uri>("Promotion:PlayerServiceBaseUrl"));

builder.Services
	.AddSingleton<NoopCampaingDetailProcessor>()
	.AddSingleton<DummyCampaingDetailProcessor>()
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats");
		})
		.AddHandler<DepositStatusChangedMessageHandler>(
			"tgs.payment.deposit.status",
			"tgs.payment.deposit.status"))
	.AddNatsGlobPatternExchange("tgs.promotion.roulette.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.register.setting")
	.AddGlobPatternExchange<DummyCampaingDetailProcessor>("tgs.promotion.dummy.setting")
	.AddGlobPatternExchange<NoopCampaingDetailProcessor>("tgs.promotion.*.setting")
	.AddNatsGlobPatternExchange("*");

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
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

app.UseSwaggerUI();
app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();
