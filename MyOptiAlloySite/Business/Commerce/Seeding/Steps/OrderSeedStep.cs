using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Completed purchase orders spread over recent months, so the Commerce dashboard and
/// reporting views have a curve to draw rather than an empty state.
/// </summary>
public sealed class OrderSeedStep(
    IOrderRepository orderRepository,
    IOrderGroupFactory orderGroupFactory,
    SeedDataProvider dataProvider) : ISeedStep
{
    /// <summary>Marks carts and orders this step owns, so teardown never guesses.</summary>
    private const string OrderName = "AlloySeed";

    private const int OrdersPerCustomer = 4;

    public int Order => 850;

    public string Name => "Purchase orders";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var currency = new Currency(data.Market.Currency);
        var variants = data.Products.SelectMany(p => p.Variants).ToList();
        var outcome = SeedOutcome.Empty;

        if (variants.Count == 0)
        {
            return outcome;
        }

        var sequence = 0;

        foreach (var seedCustomer in data.Customers)
        {
            context.ThrowIfStopRequested();

            var contact = CustomerContext.Current
                .GetContactsByPattern(seedCustomer.Email)
                .FirstOrDefault(c => string.Equals(c.Email, seedCustomer.Email, StringComparison.OrdinalIgnoreCase));

            if (contact?.PrimaryKeyId is null)
            {
                throw new InvalidOperationException(
                    $"No contact for '{seedCustomer.Email}'. Run the 'Customer contacts' step first.");
            }

            var customerId = (Guid)contact.PrimaryKeyId.Value;

            // Already seeded for this customer? Purchase orders are not carts, so we look
            // for orders rather than relying on the cart name surviving checkout.
            var existing = orderRepository.Load<IPurchaseOrder>(customerId, OrderName).Count();
            if (existing >= OrdersPerCustomer)
            {
                outcome = outcome.WithUnchanged(); 
                sequence += OrdersPerCustomer;
                continue;
            }

            for (var i = existing; i < OrdersPerCustomer; i++)
            {
                context.ThrowIfStopRequested();

                // Deterministic rather than random, so a re-run after teardown produces
                // the same dataset.
                var variant = variants[sequence % variants.Count];
                var quantity = (sequence % 3) + 1;
                var placedOn = DateTime.UtcNow.Date.AddDays(-7 * (sequence % 26));
                sequence++;

                CreatePurchaseOrder(customerId, seedCustomer, variant.Code, variant.Price, quantity, currency, placedOn);
                outcome = outcome.WithCreated();
            }

            context.Report($"Created orders for '{seedCustomer.Email}'.");
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var outcome = SeedOutcome.Empty;

        foreach (var seedCustomer in data.Customers)
        {
            context.ThrowIfStopRequested();

            var contact = CustomerContext.Current
                .GetContactsByPattern(seedCustomer.Email)
                .FirstOrDefault(c => string.Equals(c.Email, seedCustomer.Email, StringComparison.OrdinalIgnoreCase));

            if (contact?.PrimaryKeyId is null)
            {
                continue;
            }

            var customerId = (Guid)contact.PrimaryKeyId.Value;

            foreach (var order in orderRepository.Load<IPurchaseOrder>(customerId, OrderName).ToList())
            {
                orderRepository.Delete(order.OrderLink);
                outcome = outcome.WithRemoved();
            }
        }

        if (outcome.Removed > 0)
        {
            context.Report($"Removed {outcome.Removed} purchase order(s).");
        }
        else
        {
            context.Report("No seeded purchase orders present.");
        }

        return outcome;
    }

    private void CreatePurchaseOrder(
        Guid customerId,
        Data.SeedCustomer seedCustomer,
        string code,
        decimal unitPrice,
        int quantity,
        Currency currency,
        DateTime placedOn)
    {
        var cart = orderRepository.Create<ICart>(customerId, OrderName);
        cart.MarketId = MarketId.Default;
        cart.Currency = currency;

        var lineItem = orderGroupFactory.CreateLineItem(code, cart);
        lineItem.Quantity = quantity;
        lineItem.PlacedPrice = unitPrice;
        cart.AddLineItem(lineItem, orderGroupFactory);

        var total = unitPrice * quantity;

        var address = orderGroupFactory.CreateOrderAddress(cart);
        address.Id = $"{seedCustomer.Email}-shipping";
        address.FirstName = seedCustomer.FirstName;
        address.LastName = seedCustomer.LastName;
        address.Organization = seedCustomer.Organization;
        address.Line1 = "1 Alloy Way";
        address.City = seedCustomer.City;
        address.CountryCode = seedCustomer.CountryCode;
        address.PostalCode = "111 22";
        address.Email = seedCustomer.Email;

        var shipment = cart.GetFirstShipment();
        shipment.ShippingAddress = address;

        var payment = cart.CreatePayment(orderGroupFactory);
        payment.Amount = total;
        payment.PaymentMethodName = "Seeded";
        payment.Status = PaymentStatus.Processed.ToString();
        payment.TransactionType = TransactionType.Sale.ToString();
        cart.AddPayment(payment, orderGroupFactory);

        orderRepository.Save(cart);

        var orderLink = orderRepository.SaveAsPurchaseOrder(cart);
        var purchaseOrder = orderRepository.Load<IPurchaseOrder>(orderLink.OrderGroupId);
        purchaseOrder.OrderStatus = OrderStatus.Completed;

        // IPurchaseOrder.Created is read-only; the concrete order carries a setter, which
        // is what lets the dashboard show a spread of dates rather than one spike today.
        if (purchaseOrder is PurchaseOrder concreteOrder)
        {
            concreteOrder.Created = placedOn;
        }

        orderRepository.Save(purchaseOrder);

        // The cart is consumed by checkout; drop it so it does not linger as an abandoned cart.
        orderRepository.Delete(cart.OrderLink);
    }
}
