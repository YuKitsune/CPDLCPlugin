using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public record DialogueChangedNotification(Dialogue Dialogue) : INotification;