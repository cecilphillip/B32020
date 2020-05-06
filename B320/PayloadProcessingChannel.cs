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
            while (await _channel.Writer.WaitToWriteAsync(cancellationToken) &&
                   !cancellationToken.IsCancellationRequested)
            {
                if (_channel.Writer.TryWrite(document))
                {
                    // log stuff 
                    return true;
                }
            }

            return false;
        }

        public async Task<JsonDocument> ReadAsync(CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                JsonDocument document = await _channel.Reader.ReadAsync(cancellationToken);
                return document;
            }

            return null;
        }
    }
}