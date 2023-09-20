using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Configuration;
using Tgs.Promotion.MessageQueue;
using Tgs.Promotion.Models;
using Tgs.Promotion.Service.MessageQueue;
using Tgs.Promotion.Service.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("/config/appsettings.json", true)
	.AddEnvironmentVariables();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
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
	.AddOpenTelemetry()
	.ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterProviderBuilder => meterProviderBuilder
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddRuntimeInstrumentation()
		.AddOtlpExporter())
	.Services
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/health")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services
	.AddTgsRepository(builder.Configuration.GetConnectionString("Tgs")!)
	.AddRedis((redisOptions, _) => redisOptions.ConnectionString =
		builder.Configuration.GetConnectionString("Redis")!)
	.AddPromotionCore()
	.AddMemoryCache()
	.AddPromotionServiceCache()
	.AddPaymentServiceClient((clientBuilder) => clientBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Promotion:PaymentServiceBaseUrl")))
	.AddCashServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Promotion:CashBaseUrl")!;
	})
	.AddBrandServiceClient((options, _) =>
		options.BrandCertificateBaseUrl = builder.Configuration.GetValue<Uri>("Promotion:BrandServiceBaseUrl")!)
	.AddPlayerServiceClient((options, _) =>
		options.BaseUrl = builder.Configuration.GetValue<Uri>("Promotion:PlayerServiceBaseUrl")!)
	.AddPlayerBucketTypeClient()
	.AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient(httpClient => 
			httpClient.BaseAddress = builder.Configuration.GetValue<Uri>("Promotion:PlayerBucketBaseUrl")));

builder.Services
	.AddSingleton<DummyCampaignDetailProcessor>()
	.AddSingleton<TurnoverRebateDailyCampaignDetailProcessor>()
	.AddSingleton<TurnoverRebateWeeklyCampaignDetailProcessor>()
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats")!;
		})
		.AddHandler<DepositStatusChangedMessageHandler>(
			"tgs.payment.deposit.status",
			"tgs.payment.deposit.status"))
	.AddGlobPatternExchange<TurnoverRebateDailyCampaignDetailProcessor>("tgs.promotion.turnoverrebate.setting")
	.AddGlobPatternExchange<TurnoverRebateWeeklyCampaignDetailProcessor>("tgs.promotion.turnoverrebateweekly.setting")
	.AddGlobPatternExchange<DummyCampaignDetailProcessor>("tgs.promotion.dummy.setting")
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddHealthChecks();

builder.Services
	.AddDateOnlyTimeOnlyStringConverters()
	.AddControllers()
	.AddJsonOptions(jsonOptions 
		=> jsonOptions.JsonSerializerOptions.Converters.Add(new PromotionRuleViewModelJsonConverter()));

builder.Services
	.AddSwaggerGen(c => c.UseDateOnlyTimeOnlyStringConverters());

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
