namespace Domain.Entities;

public record LeftoversEntity
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Balance { get; init; }
    public string? Category { get; init; }
};