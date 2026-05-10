using RabbitMQ.Client;

namespace Contracts;

public static class RabbitMqConnectionFactoryExtensions
{
    public static ConnectionFactory CreateFactory(this RabbitMqOptions options)
    {
        return new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            VirtualHost = options.VirtualHost,
            UserName = options.User,
            Password = options.Pass,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
    }
}

