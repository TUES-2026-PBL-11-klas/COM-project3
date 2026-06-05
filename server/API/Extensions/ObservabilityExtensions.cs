using Core.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddCleanMapObservability(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var endpoint = config["Observability:OtlpEndpoint"]
            ?? config["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var headers = config["Observability:OtlpHeaders"]
            ?? config["OTEL_EXPORTER_OTLP_HEADERS"];
        var protocolValue = config["Observability:OtlpProtocol"]
            ?? config["OTEL_EXPORTER_OTLP_PROTOCOL"];

        var enabled = config.GetValue("Observability:Enabled", !string.IsNullOrWhiteSpace(endpoint));
        if (!enabled)
        {
            return builder;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "http://grafana-alloy:4317";
        }

        var serviceName = config.GetValue("Observability:ServiceName", "cleanmap-api");
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter(otlp =>
            {
                ConfigureOtlpExporter(otlp, endpoint, headers, protocolValue);
            });
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(opt =>
                    {
                        opt.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(CleanMapObservability.ActivitySourceName)
                    .AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(otlp, endpoint, headers, protocolValue);
                    });
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(CleanMapObservability.MeterName)
                    .AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(otlp, endpoint, headers, protocolValue);
                    });
            });

        return builder;
    }

    private static void ConfigureOtlpExporter(
        OtlpExporterOptions otlp,
        string endpoint,
        string? headers,
        string? protocolValue)
    {
        otlp.Endpoint = new Uri(endpoint);

        if (!string.IsNullOrWhiteSpace(headers))
        {
            otlp.Headers = headers;
        }

        var protocol = ParseOtlpProtocol(protocolValue);
        if (protocol.HasValue)
        {
            otlp.Protocol = protocol.Value;
        }
    }

    private static OtlpExportProtocol? ParseOtlpProtocol(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "grpc" => OtlpExportProtocol.Grpc,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            "http_protobuf" => OtlpExportProtocol.HttpProtobuf,
            "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => null
        };
    }
}
