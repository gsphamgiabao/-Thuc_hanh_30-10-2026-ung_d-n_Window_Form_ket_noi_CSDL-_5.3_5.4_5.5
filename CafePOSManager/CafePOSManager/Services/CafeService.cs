using System.Data;
using CafePOSManager.Data;

namespace CafePOSManager.Services;

public sealed class CafeService
{
    private readonly CafeRepository _repository;

    public CafeService(CafeRepository? repository = null)
    {
        _repository = repository ?? new CafeRepository();
    }

    public DataTable GetTables() => _repository.GetTables();

    public DataTable GetCategories() => _repository.GetCategories();

    public DataTable GetMenu() => _repository.GetMenu();

    public DataTable GetOrderDetails(int tableId) => _repository.GetOrderDetails(tableId);

    public void AddItem(int tableId, int productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
        _repository.AddItem(tableId, productId, quantity);
    }

    public decimal Pay(int tableId) => _repository.Pay(tableId);

    public void MoveActiveInvoice(int sourceTableId, int destinationTableId) =>
        _repository.MoveActiveInvoice(sourceTableId, destinationTableId);

    public void SaveCategory(int? categoryId, string name)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0)
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }
        if (normalizedName.Length > 100)
        {
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));
        }
        _repository.SaveCategory(categoryId, normalizedName);
    }

    public void DeleteCategory(int categoryId) => _repository.DeleteCategory(categoryId);

    public void SaveMenuItem(int? productId, string name, int categoryId, decimal price)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0)
        {
            throw new ArgumentException("Menu item name cannot be empty.", nameof(name));
        }
        if (normalizedName.Length > 100)
        {
            throw new ArgumentException("Menu item name cannot exceed 100 characters.", nameof(name));
        }
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Menu item price cannot be negative.");
        }
        _repository.SaveMenuItem(productId, normalizedName, categoryId, price);
    }

    public void DeleteMenuItem(int productId) => _repository.DeleteMenuItem(productId);
}
