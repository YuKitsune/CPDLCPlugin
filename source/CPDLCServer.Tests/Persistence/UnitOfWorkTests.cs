using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Persistence;

public class UnitOfWorkTests
{
    [Fact]
    public async Task Commit_SavesChangesBeforeActions()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var unitOfWork = new UnitOfWork(dbContext);

        var dialogue = new Dialogue("UAL123");
        await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);

        var savedWhenActionRan = false;
        unitOfWork.AfterCommit(async ct =>
        {
            await using var readDbContext = database.CreateDbContext();
            savedWhenActionRan = await new DialogueRepository(readDbContext).FindById(dialogue.Id, ct) is not null;
        });

        // Act
        await unitOfWork.Commit(CancellationToken.None);

        // Assert
        Assert.True(savedWhenActionRan);
    }

    [Fact]
    public async Task AfterCommit_DoesNotRunActionBeforeCommit()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var unitOfWork = new UnitOfWork(dbContext);

        var actionRan = false;

        // Act
        unitOfWork.AfterCommit(_ =>
        {
            actionRan = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.False(actionRan);
    }

    [Fact]
    public async Task Commit_RunsActionsInOrder()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var unitOfWork = new UnitOfWork(dbContext);

        var order = new List<int>();
        unitOfWork.AfterCommit(_ =>
        {
            order.Add(1);
            return Task.CompletedTask;
        });
        unitOfWork.AfterCommit(_ =>
        {
            order.Add(2);
            return Task.CompletedTask;
        });

        // Act
        await unitOfWork.Commit(CancellationToken.None);

        // Assert
        Assert.Equal([1, 2], order);
    }

    [Fact]
    public async Task Commit_WhenActionFails_RunsOtherActionsThenThrows()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var unitOfWork = new UnitOfWork(dbContext);

        var secondActionRan = false;
        unitOfWork.AfterCommit(_ => throw new InvalidOperationException("Send failed"));
        unitOfWork.AfterCommit(_ =>
        {
            secondActionRan = true;
            return Task.CompletedTask;
        });

        // Act
        var exception = await Assert.ThrowsAsync<AggregateException>(() => unitOfWork.Commit(CancellationToken.None));

        // Assert
        Assert.True(secondActionRan);
        Assert.IsType<InvalidOperationException>(Assert.Single(exception.InnerExceptions));
    }
}
