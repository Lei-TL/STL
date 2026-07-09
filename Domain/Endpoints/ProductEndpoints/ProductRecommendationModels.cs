namespace STL.Endpoints.ProductEndpoints;

public class ProductRecommendationRequest
{
    public string Id { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}

public record ProductRecommendationResponse(
    string Id,
    string Name,
    string? Description,
    string CategoryId,
    string CategoryName,
    decimal Score,
    string Reason);
