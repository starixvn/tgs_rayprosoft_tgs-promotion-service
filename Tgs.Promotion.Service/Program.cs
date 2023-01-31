using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Configuration;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using Tgs.Promotion.ApplicationServices.Promotion.Workers;
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
	.AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient(httpClient => 
			httpClient.BaseAddress = builder.Configuration.GetValue<Uri>("Promotion:PlayerBucketBaseUrl")));

builder.Services
	.AddSingleton<NoopCampaingDetailProcessor>()
	.AddSingleton<DummyCampaingDetailProcessor>()
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
	.AddNatsGlobPatternExchange("tgs.promotion.roulette.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.register.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.depositreward.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.depositaccumulateroulette.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.reliefmoneydaily.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.reliefmoneyweekly.setting")
	.AddNatsGlobPatternExchange("tgs.promotion.reliefmoneymonthly.setting")
	.AddGlobPatternExchange<TurnoverRebateDailyCampaignDetailProcessor>("tgs.promotion.turnoverrebate.setting")
	.AddGlobPatternExchange<TurnoverRebateWeeklyCampaignDetailProcessor>("tgs.promotion.turnoverrebateweekly.setting")
	.AddGlobPatternExchange<DummyCampaingDetailProcessor>("tgs.promotion.dummy.setting")
	.AddGlobPatternExchange<NoopCampaingDetailProcessor>("tgs.promotion.*.setting")
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddHealthChecks();

builder.Services
	.AddControllers()
	.AddJsonOptions(jsonOptions 
		=> jsonOptions.JsonSerializerOptions.Converters.Add(new PromotionRuleViewModelJsonConverter()));

builder.Services
	.AddDateOnlyTimeOnlyStringConverters();

builder.Services
	.AddSwaggerGen(c => c.UseDateOnlyTimeOnlyStringConverters());

builder.Services
	.AddHostedService<DistributionBackground>()
	.AddSingleton<IDistributionTaskQueue, DistributionTaskQueue>();

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
