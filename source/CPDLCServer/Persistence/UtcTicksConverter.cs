using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CPDLCServer.Persistence;

/// <summary>
/// Stores a DateTimeOffset as the number of ticks in UTC. The offset is not kept.
/// </summary>
public class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    value => new DateTimeOffset(value, TimeSpan.Zero));
