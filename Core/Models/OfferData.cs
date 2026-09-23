#if MONO
using ScheduleOne.Economy;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Product;
#endif

namespace HighBaller.Core.Models;

public class OfferData(Customer customer, ProductDefinition product, int quantity, float price)
{
    public Customer Customer { get; } = customer;

    public ProductDefinition Product { get; } = product;

    public int Quantity { get; } = quantity;

    public float Price { get; } = price;
}