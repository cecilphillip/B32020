using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B320.Transformers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ColoredConsole;

namespace B320
{
    public class ProcessHandlerWorker : BackgroundService
    {
        private readonly PayloadProcessingChannel _payloadChannel;
        private readonly DigitalSigner _signer;
        private readonly ITextTransformer _transformer;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<ProcessHandlerWorker> _logger;
        private readonly Random _random = new Random(DateTime.UtcNow.Millisecond);

        public ProcessHandlerWorker(PayloadProcessingChannel payloadChannel, DigitalSigner signer,
            ITextTransformer transformer, IHostApplicationLifetime applicationLifetime,
            ILogger<ProcessHandlerWorker> logger)
        {
            _payloadChannel = payloadChannel;
            _signer = signer;
            _transformer = transformer;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker started");
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Operation cancelled");
                return;
            }

            try
            {
                JsonDocument payload = await _payloadChannel.ReadAsync(cancellationToken);
                if (!payload.RootElement.TryGetProperty("message", out JsonElement messageElement))
                {
                    _logger.LogError("Payload property missing {property}", "message");
                    return;
                }

                if (!payload.RootElement.TryGetProperty("signature", out JsonElement signatureElement))
                {
                    _logger.LogError("Payload property missing {property}", "signature");
                    return;
                }

                string message = messageElement.GetString();

                byte[] preTransformBytes = Convert.FromBase64String(message);
                string preTransformMessage = Encoding.UTF8.GetString(preTransformBytes);

                _logger.LogDebug("Decoding message");
                message = _transformer.Decode(preTransformMessage);
                message = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(message);

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                using SHA512Managed hasher = new SHA512Managed();
                byte[] hashedMessage = hasher.ComputeHash(messageBytes);

                string signature = signatureElement.GetString();
                byte[] signedHash = Convert.FromBase64String(signature);

                _logger.LogInformation("Verifying message signature");
                if (!_signer.Verify(hashedMessage, signedHash))
                {
                    _logger.LogError("Failed to verify message signature");
                    return;
                }

                string outputText = string.Empty;
                ConsoleColor[] colorOptions =
                {
                    ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Blue,
                    ConsoleColor.Cyan, ConsoleColor.Red, ConsoleColor.White, ConsoleColor.Magenta
                };

                _logger.LogTrace("Format and display the message payload");
                message.Split("\n")
                    .ForEach(str =>
                    {
                        outputText = Figgle.FiggleFonts.Standard.Render(str);
                        int choice = _random.Next(0, colorOptions.Length - 1);
                        ColorConsole.WriteLine(outputText.Color(colorOptions[choice]));
                    });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Operation cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "oh oh.....");
            }
            finally
            {
                _logger.LogTrace("Stopping application");
                _applicationLifetime.StopApplication();
            }
        }
    }
}