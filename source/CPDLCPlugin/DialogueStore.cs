using CPDLCPlugin.Extensions;
using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class DialogueStore
{
    readonly List<DialogueDto> _dialogues = [];
    readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public async Task Populate(DialogueDto[] dialogues, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            _dialogues.Clear();
            _dialogues.AddRange(dialogues);
        }
    }

    public async Task Upsert(DialogueDto dto, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            var existingDialogue = _dialogues.FirstOrDefault(d => d.Id == dto.Id);
            if (existingDialogue is not null)
            {
                _dialogues.Remove(existingDialogue);
            }

            _dialogues.Add(dto);
        }
    }

    public async Task<DialogueDto?> Find(Guid id, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            return _dialogues.FirstOrDefault(d => d.Id == id);
        }
    }

    public async Task<DialogueDto[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            return _dialogues.ToArray();
        }
    }

    public async Task Remove(DialogueDto dto, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            var existingDialogue = _dialogues.FirstOrDefault(d => d.Id == dto.Id);
            if (existingDialogue is not null)
            {
                _dialogues.Remove(existingDialogue);
            }
        }
    }

    public async Task Clear(CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.Lock(cancellationToken))
        {
            _dialogues.Clear();
        }
    }
}
