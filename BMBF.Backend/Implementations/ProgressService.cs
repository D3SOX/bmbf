using System;
using BMBF.Backend.Services;

namespace BMBF.Backend.Implementations;

public class ProgressService : IProgressService
{
    private class Progress : IProgress
    {
        public string Name { get; }
        public int Total { get; }

        public int Completed
        {
            get => _completed;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Progress));
                }
                
                if (Math.Abs(value - _completed) > ChangeTolerance)
                {
                    _completed = value;
                    _progressService.InvokeProgressUpdated(this);
                }
            }
        }
        public int ChangeTolerance { get; set; }
        public bool RepresentAsPercentage { get; }

        private readonly ProgressService _progressService;
        private int _completed;
        private bool _disposed;
        
        public Progress(ProgressService progressService,
            string name,
            int total,
            bool representAsPercentage,
            int changeTolerance)
        {
            _progressService = progressService;
            Name = name;
            Total = total;
            RepresentAsPercentage = representAsPercentage;
            ChangeTolerance = changeTolerance;
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _progressService.UnregisterProgress(this);
        }
    }
    
    public event EventHandler<IProgress>? Updated;
    public event EventHandler<IProgress>? Added;
    public event EventHandler<IProgress>? Removed;
    public IProgress CreateProgress(string name, int total, bool representAsPercentage = false, int changeTolerance = 0)
    {
        var progress = new Progress(this, name, total, representAsPercentage, changeTolerance);
        Added?.Invoke(this, progress);
        return progress;
    }

    private void UnregisterProgress(IProgress progress)
    {
        Removed?.Invoke(this, progress);
    }

    private void InvokeProgressUpdated(IProgress progress)
    {
        Updated?.Invoke(this, progress);
    }
    
}
