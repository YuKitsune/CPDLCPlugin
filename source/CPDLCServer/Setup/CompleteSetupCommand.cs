using MediatR;

namespace CPDLCServer.Setup;

// The setup adds the first administrator, and the operator details for the privacy policy.
// The name and the password are only for the BuiltIn login. With VATSIM Connect, they are null.
public record CompleteSetupCommand(
    string AdministratorCid,
    string? AdministratorName,
    string? AdministratorPassword,
    string OperatorName,
    string PrivacyContact) : IRequest;
