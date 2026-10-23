namespace CafeManager.Models.Dtos.Products;
public sealed record ProductRow(string Code,string Name,string Category,decimal Price,string Status,byte[] Revision);
public sealed record RecipeLine(string Code,string Name,string Unit,decimal Quantity);
public sealed record RecipeInput(string Code,decimal Quantity);
