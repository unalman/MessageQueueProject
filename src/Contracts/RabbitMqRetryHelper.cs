using Microsoft.Extensions.Logging;

namespace Contracts;

public static class RabbitMqRetryHelper
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger logger,
        string operationName,
        CancellationToken cancellationToken,
        int maxDelaySeconds = 60)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var delaySeconds = Math.Min(maxDelaySeconds, 1 << Math.Min(6, attempt));
                logger.LogWarning(
                    ex,
                    "{OperationName} attempt {Attempt} failed. Retrying in {DelaySeconds}s...",
                    operationName,
                    attempt,
                    delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }
}

