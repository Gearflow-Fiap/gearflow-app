using Microsoft.Data.SqlClient;
using Workshop.Application.Abstractions;

namespace Workshop.Infrastructure.CrossBc;

/// <summary>
/// Lê preços atuais dos BCs Catalog/Inventory via SQL cru (ADR-011: leitura cross-BC no mesmo banco,
/// sem referência de projeto). Preços autoritativos para o snapshot do orçamento.
/// </summary>
internal sealed class SqlPricingReader : IPricingReader
{
    private readonly string _connectionString;

    public SqlPricingReader(string connectionString) => _connectionString = connectionString;

    public Task<IReadOnlyDictionary<Guid, int>> GetJobPricesAsync(IEnumerable<Guid> jobIds, CancellationToken ct = default) =>
        ReadPricesAsync("catalog.jobs", "id", "price_cents", jobIds, ct);

    public Task<IReadOnlyDictionary<Guid, int>> GetPartPricesAsync(IEnumerable<Guid> partIds, CancellationToken ct = default) =>
        ReadPricesAsync("inventory.parts", "id", "price_cents", partIds, ct);

    public Task<IReadOnlyDictionary<Guid, int>> GetConsumablePricesAsync(IEnumerable<Guid> consumableIds, CancellationToken ct = default) =>
        ReadPricesAsync("inventory.consumables", "id", "unit_price_cents", consumableIds, ct);

    private async Task<IReadOnlyDictionary<Guid, int>> ReadPricesAsync(
        string table, string idColumn, string priceColumn, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        var result = new Dictionary<Guid, int>();
        if (idList.Count == 0) return result;

        var paramNames = idList.Select((_, i) => $"@p{i}").ToList();
        var sql = $"SELECT {idColumn}, {priceColumn} FROM {table} WHERE {idColumn} IN ({string.Join(", ", paramNames)})";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        for (var i = 0; i < idList.Count; i++)
            cmd.Parameters.AddWithValue(paramNames[i], idList[i]);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result[reader.GetGuid(0)] = reader.GetInt32(1);

        return result;
    }
}
