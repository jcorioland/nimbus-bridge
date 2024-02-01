using System.Text.Json.Serialization;

namespace NimbusBridge.Core.Models;

public class GetCustomersResponse : BrokerResponseBase
{
    public GetCustomersResponse()
    {
    }

    public GetCustomersResponse(BrokerCommand command) : base(command)
    {
    }

    public GetCustomersResponse(string correlationId, string tenantId) : base(correlationId, tenantId)
    {
    }

    public List<Customer> Customers { get; set; } = new List<Customer>();
}
