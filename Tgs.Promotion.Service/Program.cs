using Microsoft.AspNetCore.Diagnostics;
using NATS.Client.Core;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TGS.Core.Infrastructures.Nats;
using Tgs.Promotion.MessageQueue;
using Tgs.Promotion.Models;
using Tgs.Promotion.Service.MessageQueue;
using Tgs.Promotion.Service.ViewModels;
using Tgs.Promotion.Workers;
using Valhalla.MessageQueue.Configuration;

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

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
builder.Services
	.AddProfileViewer()
	.AddOpenTelemetry()
	.ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterProviderBuilder => meterProviderBuilder
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddRuntimeInstrumentation()
		.AddOtlpExporter());

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
	.AddVipLevelServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Promotion:VipLevelServiceBaseUrl")!))
	.AddPlayerServiceClient((options, _) =>
		options.BaseUrl = builder.Configuration.GetValue<Uri>("Promotion:PlayerServiceBaseUrl")!)
	.AddPlayerBucketTypeClient()
	.AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient(httpClient =>
			httpClient.BaseAddress = builder.Configuration.GetValue<Uri>("Promotion:PlayerBucketBaseUrl")));

builder.Services
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!
		}))
	.AddSingleton<DummyCampaignDetailProcessor>()
	.AddSingleton<TurnoverRebateDailyCampaignDetailProcessor>()
	.AddSingleton<TurnoverRebateWeeklyCampaignDetailProcessor>()
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
		.AddHandler<DepositStatusChangedMessageHandler>(
			"tgs.payment.deposit.status_ex",
			"tgs.promotion.deposit.status"))
	.AddGlobPatternExchange<TurnoverRebateDailyCampaignDetailProcessor>("tgs.promotion.turnoverrebate.setting")
	.AddGlobPatternExchange<TurnoverRebateWeeklyCampaignDetailProcessor>("tgs.promotion.turnoverrebateweekly.setting")
	.AddGlobPatternExchange<DummyCampaignDetailProcessor>("tgs.promotion.dummy.setting")
	.AddNatsGlobPatternExchange("*");

builder.Services.AddNatsMessage(builder.Configuration.GetConnectionString("Nats")!);
builder.Services.AddSwaggerGen();

builder.Services
	.AddHealthChecks();

builder.Services
	.AddControllers()
	.AddJsonOptions(jsonOptions
		=> jsonOptions.JsonSerializerOptions.Converters.Add(new PromotionRuleViewModelJsonConverter()));

builder.Services.AddHostedService<PromotionDispatchBackground>()
	.AddHostedService<PromotionDistributionBackground>()
	.AddSingleton<IPromotionDispatchTaskQueue, PromotionDispatchTaskQueue>()
	.AddSingleton<IPromotionDistributionTaskQueue, PromotionDistributionTaskQueue>();

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
