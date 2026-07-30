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

    private async Task<ReservationResult> RunAsync(IReadOnlyList<ReservationItem> items, bool reserve, CancellationToken ct)
    {
        if (items.Count == 0) return new ReservationResult(ReservationStatus.Ok);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        foreach (var item in items)
        {
            var (table, guard) = item.ItemType == "Part"
                ? ("inventory.parts", reserve ? "quantity >= @q" : "reserved_quantity >= @q")
                : ("inventory.consumables", reserve ? "quantity >= @q" : "reserved_quantity >= @q");

            var setClause = reserve
                ? "quantity = quantity - @q, reserved_quantity = reserved_quantity + @q"
                : "reserved_quantity = reserved_quantity - @q";

            var sql = $"UPDATE {table} SET {setClause} WHERE id = @id AND {guard}";

            await using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@q", item.Quantity);
            cmd.Parameters.AddWithValue("@id", item.ItemId);

            var affected = await cmd.ExecuteNonQueryAsync(ct);
            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                return new ReservationResult(ReservationStatus.Insufficient,
                    $"Estoque insuficiente para {item.ItemType} {item.ItemId} (qtd {item.Quantity}).");
            }
        }

        await tx.CommitAsync(ct);
        return new ReservationResult(ReservationStatus.Ok);
    }
}
