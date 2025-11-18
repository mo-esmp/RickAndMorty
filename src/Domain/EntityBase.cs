namespace Domain;

public abstract class EntityBase
{
    protected EntityBase()
    { }

    protected readonly List<IDomainEvent> DomainEvents = [];

    public List<IDomainEvent> PopDomainEvents()
    {
        List<IDomainEvent> copy = DomainEvents.ToList();
        DomainEvents.Clear();

        return copy;
    }
}