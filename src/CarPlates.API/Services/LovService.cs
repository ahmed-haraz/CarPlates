using System.Data;
using System.Text;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class LovService : ILovService
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public LovService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("HexaConnection")
            ?? throw new InvalidOperationException("HexaConnection string is not configured.");
    }

    public async Task<List<Dictionary<string, object?>>> GetLovItemsAsync(int lovId, string? lang = "ar", string? whereClause = null)
    {
        var lov = await _context.LovStatments
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ID == lovId);

        if (lov == null)
        {
            return [];
        }

        var sql = ResolveLangPlaceholders(lov.SQLString, lang);

        if (string.IsNullOrEmpty(lov.TableName))
        {
            return ParseHardcodedValues(sql, whereClause);
        }

        return await ExecuteSqlQueryAsync(sql, whereClause);
    }

    private static string? ResolveLangPlaceholders(string? sql, string? lang)
    {
        if (string.IsNullOrWhiteSpace(sql) || !sql.Contains('$'))
        {
            return sql;
        }

        var suffix = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "_en" : "_ar";
        return sql.Replace("$", suffix);
    }

    private static List<Dictionary<string, object?>> ParseHardcodedValues(string? rawValues, string? filter)
    {
        if (string.IsNullOrWhiteSpace(rawValues))
        {
            return [];
        }

        var items = new List<Dictionary<string, object?>>();

        foreach (var segment in rawValues.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split(':', 3);
            if (parts.Length < 3) continue;

            items.Add(new Dictionary<string, object?>
            {
                ["ID"] = parts[0].Trim(),
                ["Name_Ar"] = parts[1].Trim(),
                ["Name_En"] = parts[2].Trim()
            });
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            items = items.Where(item =>
            {
                try
                {
                    return MatchesFilter(item, filter);
                }
                catch
                {
                    return false;
                }
            }).ToList();
        }

        return items;
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string? sql, string? whereClause)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        var sqlBuilder = new StringBuilder(sql);

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            var upperSql = sql.ToUpperInvariant();
            var orderIdx = upperSql.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);

            if (orderIdx >= 0)
            {
                sqlBuilder.Insert(orderIdx, $" WHERE {whereClause} ");
            }
            else
            {
                sqlBuilder.Append($" WHERE {whereClause}");
            }
        }

        var result = new List<Dictionary<string, object?>>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
        command.CommandType = CommandType.Text;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value == DBNull.Value ? null : value;
            }
            result.Add(row);
        }

        return result;
    }

    private static bool MatchesFilter(Dictionary<string, object?> item, string filter)
    {
        var parts = filter.Split(new[] { " AND ", " OR " }, StringSplitOptions.None);
        var isOr = filter.Contains(" OR ", StringComparison.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            var trimmed = part.Trim().TrimStart('(').TrimEnd(')');
            var match = EvaluateCondition(item, trimmed);
            if (isOr)
            {
                if (match) return true;
            }
            else
            {
                if (!match) return false;
            }
        }

        return !isOr;
    }

    private static bool EvaluateCondition(Dictionary<string, object?> item, string condition)
    {
        var operators = new[] { " LIKE ", " = ", " != ", " <> ", " > ", " >= ", " < ", " <= " };

        foreach (var op in operators)
        {
            var idx = condition.IndexOf(op, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            var col = condition[..idx].Trim();
            var val = condition[(idx + op.Length)..].Trim().Trim('\'', '"');

            if (!item.TryGetValue(col, out var raw)) return false;

            var itemVal = raw?.ToString() ?? "";

            if (op.Trim() == "LIKE")
            {
                var pattern = val.Replace("%", "").Replace("_", "?");
                return itemVal.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }

            return op.Trim() switch
            {
                "=" => string.Equals(itemVal, val, StringComparison.OrdinalIgnoreCase),
                "!=" or "<>" => !string.Equals(itemVal, val, StringComparison.OrdinalIgnoreCase),
                ">" => string.Compare(itemVal, val, StringComparison.OrdinalIgnoreCase) > 0,
                ">=" => string.Compare(itemVal, val, StringComparison.OrdinalIgnoreCase) >= 0,
                "<" => string.Compare(itemVal, val, StringComparison.OrdinalIgnoreCase) < 0,
                "<=" => string.Compare(itemVal, val, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => false
            };
        }

        return false;
    }
}
