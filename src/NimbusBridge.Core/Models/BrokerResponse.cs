namespace NimbusBridge.Core.Models;

public class BrokerResponse<TResponse> : BrokerCommandResponseBase
    where TResponse : class, new()
{
    public BrokerResponse()
        : base()
    {
    }

    public BrokerResponse(string correlationId, string tenantId)
        : base(correlationId, tenantId)
    {
    }

    public BrokerResponse(BrokerCommand command)
        : base(command.CorrelationId, command.TenantId)
    {
    }

    public bool HasError { get; set; } = false;

    public bool HasResponse
    {
        get
        {
            return Response != null;
        }
    }

    public TResponse? Response { get; set; }

}
