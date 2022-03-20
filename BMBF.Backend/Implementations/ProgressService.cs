using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BMBF.Backend.Services;

namespace BMBF.Backend.Implementations;

public class ProgressService : IProgressService
{
    private class Progress : IProgress
    {
        public string Name { get; }
        public int Total { get; }
        public long Id { get; }

        public int Completed
        {
            get => _completed;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Progress));
                }

                _completed = value;
                InvokeChangeIfOutsideTolerance();
            }
        }

        public void ItemCompleted()
        {
            Interlocked.Increment(ref _completed);
            InvokeChangeIfOutsideTolerance();
        }

        public int ChangeTolerance { get; set; }
        public bool RepresentAsPercentage { get; }
        
        public IProgress? Parent { get; }

        private readonly ProgressService _progressService;
        private int _completed;
        private int _lastChange;
        private bool _disposed;
        
        public Progress(ProgressService progressService,
            string name,
            int total,
            bool representAsPercentage,
            int changeTolerance, 
            IProgress? parent,
            long id)
        {
            _progressService = progressService;
            Name = name;
            Total = total;
            RepresentAsPercentage = representAsPercentage;
            ChangeTolerance = changeTolerance;
            Parent = parent;
            Id = id;
        }

        private void InvokeChangeIfOutsideTolerance()
        {
            if (Math.Abs(_completed - _lastChange) > ChangeTolerance)
            {
                _lastChange = _completed;
                _progressService.InvokeProgressUpdated(this);
            }
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

    public IReadOnlyDictionary<long, IProgress> CurrentOperations => _currentOperations;
    
    private readonly ConcurrentDictionary<long, IProgress> _currentOperations = new();

    private int _currentProgressId;

    public IProgress CreateProgress(string name, int total, bool representAsPercentage = false, int changeTolerance = 0, IProgress? parent = null)
    {
        int progressId = Interlocked.Increment(ref _currentProgressId);
        
        var progress = new Progress(this, name, total, representAsPercentage, changeTolerance, parent, progressId);
        _currentOperations[progressId] = progress;
        Added?.Invoke(this, progress);
        return progress;
    }

    private void UnregisterProgress(IProgress progress)
    {
        if (_currentOperations.TryRemove(progress.Id, out var removed))
        {
            Debug.Assert(removed == progress);
        }
        else
        {
            Debug.Fail("Removed progress was not registered");
        }
        Removed?.Invoke(this, progress);
    }

    private void InvokeProgressUpdated(IProgress progress)
    {
        Updated?.Invoke(this, progress);
    }
    
}
