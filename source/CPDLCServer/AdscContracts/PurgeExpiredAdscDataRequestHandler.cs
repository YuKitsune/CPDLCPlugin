using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class PurgeExpiredAdscDataRequestHandler(
    IAdscMessageLogRepository adscMessageLogRepository,
    IAdscReportRepository adscReportRepository,
    IClock clock)
    : IRequestHandler<PurgeExpiredAdscDataRequest>
{
    readonly TimeSpan _messageLogRetention = TimeSpan.FromHours(24);

    public async Task Handle(PurgeExpiredAdscDataRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();

        await adscMessageLogRepository.RemoveOlderThan(now - _messageLogRetention, cancellationToken);
        await adscReportRepository.RemoveOlderThan(now - request.AdscReportRetention, cancellationToken);
    }
}
