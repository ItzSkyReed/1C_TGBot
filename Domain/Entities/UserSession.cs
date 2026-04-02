namespace Domain.Entities;

public record UserSession
{
    public string ActiveCommand { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
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