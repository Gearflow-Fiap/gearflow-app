using Microsoft.Data.SqlClient;
using Workshop.Application.Abstractions;

namespace Workshop.Infrastructure.CrossBc;

/// <summary>
/// Resolve nome/e-mail do cliente a partir do veículo, via SQL cru contra o schema do Customers
/// (ADR-002: leitura cross-BC no mesmo banco, sem referência de projeto).
/// </summary>
internal sealed class SqlCustomerContactReader : ICustomerContactReader
{
    private readonly string _connectionString;

    public SqlCustomerContactReader(string connectionString) => _connectionString = connectionString;

    public async Task<CustomerContact?> GetByVehicleAsync(Guid vehicleId, CancellationToken ct = default)
    {
        const string sql = @"SELECT c.name, c.email
                             FROM customers.vehicles v
                             JOIN customers.clients c ON c.id = v.client_id
                             WHERE v.id = @id";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", vehicleId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new CustomerContact(reader.GetString(0), reader.GetString(1));
    }
}
