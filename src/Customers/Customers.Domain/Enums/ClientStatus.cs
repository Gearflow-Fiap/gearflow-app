namespace Customers.Domain.Enums;

/// <summary>Situação cadastral do cliente, consultada por sistemas externos (ex.: autenticação por CPF).</summary>
public enum ClientStatus
{
    Active = 1,
    Inactive = 2,
}
