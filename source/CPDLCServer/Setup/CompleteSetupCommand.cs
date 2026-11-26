using MediatR;

namespace CPDLCServer.Setup;

// The setup adds the first administrator, and the operator details for the privacy policy.
// The password is only for the BuiltIn login. With VATSIM Connect, it is null.
public record CompleteSetupCommand(
    string AdministratorCid,
    string? AdministratorPassword,
    string OperatorName,
    string PrivacyContact) : IRequest;
