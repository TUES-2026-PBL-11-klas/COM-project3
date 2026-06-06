using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.CleanMap.Interfaces;
using Microsoft.Extensions.Logging;

namespace Core.CleanMap.Services;

public class AiDetectionService(HttpClient httpClient, ILogger<AiDetectionService> logger) : IAiDetectionService
{
    public async Task<IReadOnlyList<TrashDetection>> DetectTrashAsync(string base64Image, CancellationToken cancellationToken = default)
    {
        if (httpClient.BaseAddress == null)
        {
            logger.LogWarning("AI Detection Service is disabled (BaseUrl is missing). Skipping trash detection.");
            return [];
        }

        try
        {
            // Remove data URI scheme prefix if present
            var base64Data = base64Image.Contains(',') ? base64Image.Split(',')[1] : base64Image;
            var imageBytes = Convert.FromBase64String(base64Data);

            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "file", "image.jpg");

            var response = await httpClient.PostAsync("/detect", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AiDetectionResponse>(cancellationToken: cancellationToken);
            
            if (result?.Detections == null) return [];

            return result.Detections.Select(d => new TrashDetection
            {
                Label = d.Label,
                Confidence = d.Confidence,
                X1 = d.Bbox.X1,
                Y1 = d.Bbox.Y1,
                X2 = d.Bbox.X2,
                Y2 = d.Bbox.Y2,
                ClsId = d.ClsId
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call AI detection service.");
            return [];
        }
    }

    private class AiDetectionResponse
    {
        [JsonPropertyName("detections")]
        public List<AiDetectionItem>? Detections { get; set; }
    }

    private class AiDetectionItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("bbox")]
        public AiBoundingBox Bbox { get; set; } = new();

        [JsonPropertyName("cls_id")]
        public int ClsId { get; set; }
    }

    private class AiBoundingBox
    {
        [JsonPropertyName("x1")]
        public int X1 { get; set; }

        [JsonPropertyName("y1")]
        public int Y1 { get; set; }

        [JsonPropertyName("x2")]
        public int X2 { get; set; }

        [JsonPropertyName("y2")]
        public int Y2 { get; set; }
    }
}
