namespace Shared.Contracts;

/// <summary>
/// Tipo de item de estoque referenciado em reservas, preços e eventos cross-BC. Substitui a magic
/// string "Part"/"Consumable" que circulava entre Inventory, Workshop e Notifications.
/// </summary>
public enum InventoryItemType
{
    Part = 1,
    Consumable = 2,
}
