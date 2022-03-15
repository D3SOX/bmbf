using System;
using BMBF.Backend.Services;

namespace BMBF.Backend.Implementations;

public class ProgressService : IProgressService
{
    private class PercentageProgress : IPercentageProgress
    {
        public float Percentage
        {
            get => _percentage;
            set {
                if (Math.Abs(_percentage - value) > 1) // Only update the progress if increased by at least 1%
                {
                    _percentage = value;
                    _progressService.InvokeProgressUpdated(this);
                }    
            }
        }
        public string Name { get; }

        private float _percentage;
        private readonly ProgressService _progressService;
        
        public PercentageProgress(ProgressService progressService, string name)
        {
            _progressService = progressService;
            Name = name;
        }

        public void Dispose()
        {
            _progressService.UnregisterProgress(this);
        }
    }
    
    private class ChunkedProgress : IChunkedProgress
    {
        public int ItemsCompleted
        {
            get => _itemsCompleted;
            set
            {
                if (_itemsCompleted != value)
                {
                    _itemsCompleted = value;
                    _progressService.InvokeProgressUpdated(this);
                }
            }
        }
        private int _itemsCompleted;
        public int TotalItems { get; }
        public string Name { get; }
        
        private readonly ProgressService _progressService;
        public ChunkedProgress(ProgressService progressService, int totalItems, string name)
        {
            TotalItems = totalItems;
            Name = name;
            _progressService = progressService;
        }

        public void Dispose()
        {
            _progressService.UnregisterProgress(this);
        }
    }
    
    public event EventHandler<IProgress>? Updated;
    public event EventHandler<IProgress>? Added;
    public event EventHandler<IProgress>? Removed;
    
    private void UnregisterProgress(IProgress progress)
    {
        Removed?.Invoke(this, progress);
    }

    private void InvokeProgressUpdated(IProgress progress)
    {
        Updated?.Invoke(this, progress);
    }

    public IPercentageProgress CreatePercentageProgress(string name)
    {
        var progress = new PercentageProgress(this, name);
        Added?.Invoke(this, progress);
        return progress;
    }

    public IChunkedProgress CreateChunkedProgress(string name, int maxItems)
    {
        var progress = new ChunkedProgress(this, maxItems, name);
        Added?.Invoke(this, progress);
        return progress;
    }
}
