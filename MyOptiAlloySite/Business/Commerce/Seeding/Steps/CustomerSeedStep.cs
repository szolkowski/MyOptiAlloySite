using Mediachase.Commerce.Customers;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Customer contacts with a preferred address, so the Customers UI and any order seeding
/// have real people to point at.
/// </summary>
public sealed class CustomerSeedStep(SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 700;

    public string Name => "Customer contacts";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var outcome = SeedOutcome.Empty;

        foreach (var seed in data.Customers)
        {
            context.ThrowIfStopRequested();

            if (FindByEmail(seed.Email) is not null)
            {
                outcome = outcome.WithUnchanged();
                continue;
            }

            var contact = CustomerContact.CreateInstance();
            contact.FirstName = seed.FirstName;
            contact.LastName = seed.LastName;
            contact.FullName = $"{seed.FirstName} {seed.LastName}";
            contact.Email = seed.Email;
            contact.SaveChanges();

            var address = CustomerAddress.CreateInstance();
            address.Name = $"{seed.FirstName} {seed.LastName} - default";
            address.FirstName = seed.FirstName;
            address.LastName = seed.LastName;
            address.OrganizationName = seed.Organization;
            address.Line1 = "1 Alloy Way";
            address.City = seed.City;
            address.CountryCode = seed.CountryCode;
            address.PostalCode = "111 22";
            address.Email = seed.Email;

            contact.AddContactAddress(address);
            contact.SaveChanges();

            contact.PreferredBillingAddress = address;
            contact.PreferredShippingAddress = address;
            contact.SaveChanges();

            context.Report($"Created contact '{seed.Email}'.");
            outcome = outcome.WithCreated();
        }

        return outcome;
    }

    private static CustomerContact FindByEmail(string email) =>
        CustomerContext.Current
            .GetContactsByPattern(email)
            .FirstOrDefault(c => string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var outcome = SeedOutcome.Empty;

        foreach (var seed in data.Customers)
        {
            context.ThrowIfStopRequested();

            var contact = FindByEmail(seed.Email);
            if (contact is null)
            {
                continue;
            }

            contact.DeleteWithAllDependents();
            context.Report($"Removed contact '{seed.Email}'.");
            outcome = outcome.WithRemoved();
        }

        return outcome;
    }
}
