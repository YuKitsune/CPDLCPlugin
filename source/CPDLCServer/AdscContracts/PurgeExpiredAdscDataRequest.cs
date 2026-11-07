using MediatR;

namespace CPDLCServer.AdscContracts;

public record PurgeExpiredAdscDataRequest(TimeSpan AdscReportRetention) : IRequest;
