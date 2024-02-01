namespace NimbusBridge.Core.Models;

public class Customer
{
    public Customer(int customerId, string firstName, string lastName, string city, string country)
    {
        CustomerId = customerId;
        FirstName = firstName;
        LastName = lastName;
        City = city;
        Country = country;
    }

    public int CustomerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}