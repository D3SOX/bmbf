using System;
using BMBF.Backend.Implementations;
using Xunit;

namespace BMBF.Backend.Tests;

public class ProgressServiceTests
{
    private readonly ProgressService _progressService;

    public ProgressServiceTests()
    {
        _progressService = new ProgressService();
    }


    [Theory]
    [InlineData("Test", 15, true, 10)]
    [InlineData("Test2", 30, false, 12)]
    public void PropertiesShouldMatchCreationArgs(string name, int total, bool representAsPercentage, int changeTolerance)
    {
        using var progress = _progressService.CreateProgress(name, total, representAsPercentage, changeTolerance);
        Assert.Equal(0, progress.Completed);
        Assert.Equal(total, progress.Total);
        Assert.Equal(representAsPercentage, progress.RepresentAsPercentage);
        Assert.Equal(changeTolerance, progress.ChangeTolerance);
        // No parent, so should be null
        Assert.Null(progress.Parent);
        Assert.Null(progress.ParentId);
    }

    [Fact]
    public void ParentShouldMatch()
    {
        using var parent = _progressService.CreateProgress("Example parent", 10);
        using var progress = _progressService.CreateProgress("Example child", 10, parent: parent);

        Assert.Equal(parent, progress.Parent);
        Assert.Equal(parent.Id, progress.ParentId);
    }

    [Fact]
    public void ShouldInvokeAdded()
    {
        IProgress? added = null;
        _progressService.Added += (_, args) => added = args;

        using var progress = _progressService.CreateProgress("Example", 10);
        Assert.Equal(progress, added);
    }

    [Fact]
    public void IdsShouldBeUnique()
    {
        using var progressA = _progressService.CreateProgress("A", 10);
        using var progressB = _progressService.CreateProgress("B", 10);

        Assert.NotEqual(progressA.Id, progressB.Id);
    }

    [Theory]
    [InlineData(5, 10, false)]
    [InlineData(5, 3, true)]
    [InlineData(5, 5, false)]
    [InlineData(1, 0, true)]
    public void ChangingCompletedShouldUpdateProgressIfByLessThanOrEqualToTolerance(
        int changeAmount,
        int tolerance,
        bool shouldUpdate)
    {
        using var progress = _progressService.CreateProgress("Test", 100, changeTolerance: tolerance);
        bool invokedUpdated = false;
        _progressService.Updated += (_, p) => invokedUpdated = p == progress;

        progress.Completed += changeAmount;
        Assert.Equal(shouldUpdate, invokedUpdated);
    }


    [Fact]
    public void DisposingProgressShouldRemove()
    {
        var progress = _progressService.CreateProgress("Test", 10);

        bool invokedRemoved = false;
        _progressService.Removed += (_, p) => invokedRemoved = p == progress;
        progress.Dispose();

        Assert.True(invokedRemoved);
    }

    [Fact]
    public void ShouldOnlyRemoveOnce()
    {
        var progress = _progressService.CreateProgress("Test", 10);
        progress.Dispose();

        bool invokedRemoved = false;
        _progressService.Removed += (_, p) => invokedRemoved = p == progress;
        progress.Dispose();

        Assert.False(invokedRemoved);
    }

    [Fact]
    public void ShouldNotUpdateProgressIfDisposed()
    {
        var progress = _progressService.CreateProgress("Test", 10);
        progress.Dispose();
        Assert.Throws<ObjectDisposedException>(() => progress.Completed = 1);
    }
}
