using Core.CleanMap;

namespace API.Endpoints;
public static class CleanMapEndpoints
{
    public static IEndpointRouteBuilder MapCleanMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/cleanmap");

        group.MapGet("/ping", () => Results.Ok(new { status = "ok" }));

        group.MapGet("/health", async (
            ICleanMapHealthCheck healthCheck,
            CancellationToken cancellationToken) =>
        {
            var mongoAvailable = await healthCheck.CanConnectAsync(cancellationToken);

            return mongoAvailable
                ? Results.Ok(new { status = "ok", database = "connected" })
                : Results.Problem(
                    title: "MongoDB is not reachable.",
                    detail: "Start MongoDB or update CleanMapDatabase:ConnectionString.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        group.MapGet("/reports", async (
            ICleanMapReportService reports,
            CancellationToken cancellationToken) =>
        {
            var reportList = await reports.GetAllAsync(cancellationToken);
            return Results.Ok(reportList);
        });

        group.MapGet("/reports/{id}", async (
            string id,
            ICleanMapReportService reports,
            CancellationToken cancellationToken) =>
        {
            var report = await reports.GetByIdAsync(id, cancellationToken);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });

        group.MapPost("/reports", async (
            CreateCleanMapReportRequest input,
            ICleanMapReportService reports,
            CancellationToken cancellationToken) =>
        {
            var result = await reports.CreateAsync(input, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/cleanmap/reports/{result.Report!.Id}", result.Report)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/reports/{id}/clean", async (
            string id,
            MarkCleanRequest input,
            ICleanMapReportService reports,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.BadRequest(new { error = "Report id is required." });
            }

            var report = await reports.MarkCleanAsync(id, input, cancellationToken);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });

        return endpoints;
    }
}