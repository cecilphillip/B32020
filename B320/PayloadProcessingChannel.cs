using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace B320
{
    public class PayloadProcessingChannel
    {
        private const int MAXIMUM_CHANNEL_MESSAGES = 1;

        private readonly Channel<JsonDocument> _channel;
        private readonly ILogger<PayloadProcessingChannel> _logger;

        public PayloadProcessingChannel(ILogger<PayloadProcessingChannel> logger)
        {
            var options = new BoundedChannelOptions(MAXIMUM_CHANNEL_MESSAGES)
            {
                SingleWriter = false,
                SingleReader = true
            };

            _channel = Channel.CreateBounded<JsonDocument>(options);

            _logger = logger;
        }

        public async Task<bool> AddPayloadAsync(JsonDocument document, CancellationToken cancellationToken = default)
        {
            while (await _channel.Writer.WaitToWriteAsync(cancellationToken))
            {
                _logger.LogInformation("Writing to channel");
                if (_channel.Writer.TryWrite(document))
                {
                    _logger.LogInformation("Channel write successful");
                    return true;
                }
            }

            if (cancellationToken.IsCancellationRequested) _logger.LogWarning("Operation cancelled {operation}",nameof(AddPayloadAsync));
            _logger.LogWarning("Unable to write to channel");

            return false;
        }

        public async Task<JsonDocument> ReadAsync(CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                _logger.LogInformation("Reading channel data");
                JsonDocument document = await _channel.Reader.ReadAsync(cancellationToken);
                _logger.LogInformation("Channel read successful");
                return document;
            }
            
            if (cancellationToken.IsCancellationRequested) _logger.LogWarning("Operation cancelled {operation}",nameof(ReadAsync));
            _logger.LogWarning("Unable to read channel");
            return null;
        }
    }
}