namespace CarPlates.API.Interface;

public interface ILovService
{
    Task<List<Dictionary<string, object?>>> GetLovItemsAsync(int lovId, string? lang = "ar", string? whereClause = null);
}
