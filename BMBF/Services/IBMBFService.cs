namespace BMBF.Services;

// ReSharper disable once InconsistentNaming
public interface IBMBFService
{
    /// <summary>
    /// Restarts the BMBF service, and shows the loading message on the frontend while this completes.
    /// </summary>
    void Restart();

    /// <summary>
    /// Quits the BMBF service
    /// </summary>
    void Quit();
}
