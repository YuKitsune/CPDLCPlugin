using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public record ArchiveDialogueCommand(Guid DialogueId) : IRequest;
