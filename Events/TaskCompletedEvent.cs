using PromoosTT.Models;

namespace PromoosTT.Events;

public record TaskCompletedEvent(
    Guid TaskId,
    string Title,
    DateTime CompletedAt,
    Priority Priority
    );