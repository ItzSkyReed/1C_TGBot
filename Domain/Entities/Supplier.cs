namespace Domain.Entities;

public record Supplier
{
    public required string Id { get; set; }
    public required string Name { get; set; }
};