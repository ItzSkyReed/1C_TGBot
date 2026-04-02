namespace Domain.Entities;

public record Component
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}