namespace BMBF.Backend.Models.Messages;

public class ProgressAdded : IMessage
{
    public ProgressAdded(IProgress progress, int id)
    {
        Name = progress.Name;
        Id = id;
        TotalItems = progress.Total;
        RepresentAsPercentage = progress.RepresentAsPercentage;
    }

    public string Name { get; }
    
    public int Id { get; }
    
    public int TotalItems { get; }

    public bool RepresentAsPercentage { get; }
    
    public MessageType Type => MessageType.ProgressAdded;
}
