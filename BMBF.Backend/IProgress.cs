using System;
using System.Text.Json.Serialization;
using BMBF.Backend.Services;

namespace BMBF.Backend;

/// <summary>
/// Represents the progress of an operation.
/// Disposing this object will remove it from the <see cref="IProgressService"/>
/// </summary>
public interface IProgress : IDisposable
{
    /// <summary>
    /// The name of the operation that this object represents the progress of.
    /// This is not guaranteed to be unique
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Total value that the progress represents (i.e. the full value)
    /// </summary>
    int Total { get; }
    
    /// <summary>
    /// Unique identifier for each operation.
    /// </summary>
    long Id { get; }
    
    /// <summary>
    /// The current progress, how many out of the total items have been completely
    /// Note: If incrementing this concurrently, you should use <see cref="ItemCompleted"/>
    /// </summary>
    int Completed { get; set; }

    /// <summary>
    /// Increments <see cref="Completed"/> in a thread-safe manner.
    /// </summary>
    void ItemCompleted();
    
    /// <summary>
    /// If <see cref="Completed"/> changes by an amount less than or equal to this value, the change will not be
    /// forecast to the frontend.
    /// </summary>
    [JsonIgnore]
    int ChangeTolerance { get; set; }
    
    /// <summary>
    /// Whether or not this progress will be represented as a percentage.
    /// </summary>
    bool RepresentAsPercentage { get; }
    
    [JsonIgnore]
    public IProgress? Parent { get; }

    [JsonPropertyName("parent")] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ParentId => Parent?.Id;
}
