using Octodiff.Diagnostics;

namespace BMBF.Backend.Implementations;

public class DummyProgressReporter : IProgressReporter
{
    public void ReportProgress(string operation, long currentPosition, long total) { }
}