using CPDLCServer.Contracts;
using MediatR;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

public record HandoffCompletedNotification(string Callsign, string? NextControllerCallsign) : INotification;
