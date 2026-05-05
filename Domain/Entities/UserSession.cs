namespace Domain.Entities;

public record UserSession
{
    public string ActiveCommand { get; init; } = string.Empty;
    public required SessionStep CurrentStep { get; set; }
    private string ContextData { get; set; } = "{}";


    public T GetData<T>() where T : new()
    {
        if (string.IsNullOrWhiteSpace(ContextData))
            return new T();

        return System.Text.Json.JsonSerializer.Deserialize<T>(ContextData) ?? new T();
    }

    public void SetData<T>(T data)
    {
        ContextData = System.Text.Json.JsonSerializer.Serialize(data);
    }
}