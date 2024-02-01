namespace NimbusBridge.LegacySdk;

/// <summary>
/// This class simulates a legacy SDK that is used to communicate with the NimbusBridge legacy software.
/// This service allows to get customers data from the CRM. You can imagine that it connects to various system / database that run on premise, for example.
/// </summary>
public class CustomersService
{
    public List<Customer> GetCustomers()
    {
        return new List<Customer>()
        {
            new Customer(1, "Toby", "Miller", "Sydney", "Australia"),
            new Customer(2, "Anna", "Harris", "London", "UK"),
            new Customer(3, "Leon", "Piper", "Paris", "France"),
            new Customer(4, "Susan", "Perkins", "New York", "USA"),
            new Customer(5, "Charles", "Dickens", "Las Vegas", "USA"),
            new Customer(6, "Mike", "Tanner", "Tokyo", "Japan"),
            new Customer(7, "Hector", "Sánchez", "Madrid", "Spain")
        };
    }
}
