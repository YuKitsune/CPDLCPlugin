using MediatR;

namespace CPDLCServer.Setup;

// The setup adds the first administrator, and the operator details for the privacy policy
public record CompleteSetupCommand(string AdministratorCid, string OperatorName, string PrivacyContact) : IRequest;
