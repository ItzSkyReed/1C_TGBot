namespace Domain.Entities;

public record UserAuthDto
{
    public required uint Identifier { get; init; }
    public required long TelegramId { get; init; }
    public required string Name { get; init; }
};