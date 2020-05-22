using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B320.Transformers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B320
{
    public class PayloadGeneratorWorker : BackgroundService
    {
        private readonly DigitalSigner _signer;
        private readonly ITextTransformer _transformer;
        private readonly ManualResetEventSlim _payloadReadEvent;
        private readonly ILogger<PayloadGeneratorWorker> _logger;

        public PayloadGeneratorWorker(DigitalSigner signer, ITextTransformer transformer,
            ManualResetEventSlim payloadReadEvent, ILogger<PayloadGeneratorWorker> logger)
        {
            _signer = signer;
            _transformer = transformer;
            _payloadReadEvent = payloadReadEvent;
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

            if (File.Exists(Constants.EXPECTED_ZIP_FILE_NAME))
            {
                _logger.LogInformation("Data archive found {filename}", Constants.EXPECTED_ZIP_FILE_NAME);
                try
                {
                    // clean up artifacts
                    File.Delete(Constants.EXPECTED_FILE_NAME);
                    File.Delete(Constants.EXPECTED_ZIP_FILE_NAME);
                    _logger.LogDebug("Clean up completed\n\t {archivename}\n\t {filename}", Constants.EXPECTED_ZIP_FILE_NAME, Constants.EXPECTED_FILE_NAME);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception thrown during file clean up");
                    return;
                }
            }

            string message = await GetMessagePayload();
            
            // generate hash and message signature
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using SHA512Managed hasher = new SHA512Managed();
            byte[] hashedMessage = hasher.ComputeHash(messageBytes);
            byte[] signedHash = _signer.Sign(hashedMessage);
            string signature = Convert.ToBase64String(signedHash);

            _logger.LogInformation("Payload hashed and signed");
            // apply text transformation
            message = _transformer.Encode(message);
            message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

            // prepare final payload
            var payload = new
            {
                eventType = "Microsoft Build",
                version = "2.02.0",
                message, signature
            };

            string serializedData = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                IgnoreNullValues = true
            });

            await using (FileStream fileStream = File.Create(Constants.EXPECTED_FILE_NAME))
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(serializedData);
                await fileStream.WriteAsync(dataBytes, cancellationToken);
            }
            _logger.LogDebug("Payload file created {filename}", Constants.EXPECTED_FILE_NAME);
            
            await using (FileStream zipFileStream = File.Create(Constants.EXPECTED_ZIP_FILE_NAME))
            {
                using ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
                ZipArchiveEntry payloadEntry =
                    archive.CreateEntryFromFile(Constants.EXPECTED_FILE_NAME, Constants.EXPECTED_FILE_NAME);
            }
            _logger.LogInformation("Compression complete {filename}", Constants.EXPECTED_ZIP_FILE_NAME);
            _payloadReadEvent.Set();
        }

        private ValueTask<string> GetMessagePayload()
        {
            string data =
                CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                    "No, Thank You!!!");
            
            _logger.LogInformation("Payload acquired");
            return new ValueTask<string>(data);
        }
    }
}