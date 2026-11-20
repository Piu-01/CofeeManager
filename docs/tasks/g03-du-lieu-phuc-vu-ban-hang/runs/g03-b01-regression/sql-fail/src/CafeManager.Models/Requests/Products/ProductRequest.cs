using CafeManager.Models.Dtos.Products;
namespace CafeManager.Models.Requests.Products;
public sealed record ProductRequest(string Code,string Name,string Category,decimal Price,string Status,IReadOnlyList<RecipeInput> Recipe);
