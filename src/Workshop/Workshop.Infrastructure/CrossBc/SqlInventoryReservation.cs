using Microsoft.Data.SqlClient;
using Workshop.Application.Abstractions;

namespace Workshop.Infrastructure.CrossBc;

/// <summary>
/// Reserva/consome estoque nas tabelas do Inventory via SQL cru transacional (ADR-011). Preserva a
/// regra bifásica: <c>Reserve</c> move disponível→reservado (com guard de disponibilidade); se algum
/// item não tiver saldo, faz rollback e devolve Insufficient (reserva atômica). <c>Consume</c> baixa
/// a reserva. Espelha os métodos <c>Reserve</c>/<c>Consume</c> dos agregados Part/Consumable.
/// </summary>
internal sealed class SqlInventoryReservation : IInventoryReservation
{
    private readonly string _connectionString;

    public SqlInventoryReservation(string connectionString) => _connectionString = connectionString;

    public Task<ReservationResult> ReserveAsync(IReadOnlyList<ReservationItem> items, CancellationToken ct = default) =>
        RunAsync(items, reserve: true, ct);

    public Task<ReservationResult> ConsumeAsync(IReadOnlyList<ReservationItem> items, CancellationToken ct = default) =>
        RunAsync(items, reserve: false, ct);

    // Espelha as constantes Part.MinimumStockQuantity / Consumable.MinimumStockQuantity do Inventory.Domain.
    private const decimal MinimumStock = 5m;

    private async Task<ReservationResult> RunAsync(IReadOnlyList<ReservationItem> items, bool reserve, CancellationToken ct)
    {
        if (items.Count == 0) return new ReservationResult(ReservationStatus.Ok);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        var lowStock = new List<LowStockItem>();

        foreach (var item in items)
        {
            var table = item.ItemType == "Part" ? "inventory.parts" : "inventory.consumables";
            var guard = reserve ? "quantity >= @q" : "reserved_quantity >= @q";
            var setClause = reserve
                ? "quantity = quantity - @q, reserved_quantity = reserved_quantity + @q"
                : "reserved_quantity = reserved_quantity - @q";

            // OUTPUT devolve o estoque disponível resultante + nome, para checar o mínimo sem 2ª query.
            var sql = $@"UPDATE {table} SET {setClause}
                        OUTPUT INSERTED.quantity, INSERTED.name
                        WHERE id = @id AND {guard}";

            await using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@q", item.Quantity);
            cmd.Parameters.AddWithValue("@id", item.ItemId);

            decimal? remaining = null;
            string? name = null;
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (await reader.ReadAsync(ct))
                {
                    remaining = Convert.ToDecimal(reader.GetValue(0));
                    name = reader.GetString(1);
                }
            }

            if (remaining is null)
            {
                await tx.RollbackAsync(ct);
                return new ReservationResult(ReservationStatus.Insufficient,
                    $"Estoque insuficiente para {item.ItemType} {item.ItemId} (qtd {item.Quantity}).");
            }

            // Alerta de estoque baixo só faz sentido no consumo (finalização), como no GearFlow.
            if (!reserve && remaining.Value <= MinimumStock)
                lowStock.Add(new LowStockItem(item.ItemType, item.ItemId, name ?? string.Empty, remaining.Value, MinimumStock));
        }

        await tx.CommitAsync(ct);
        return new ReservationResult(ReservationStatus.Ok, LowStock: lowStock);
    }
}
