namespace Contracts;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public string VirtualHost { get; init; } = "/";
    public string User { get; init; } = "guest";
    public string Pass { get; init; } = "guest";
}

