using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B320
{
    public class DataAcquisitionWorker : BackgroundService
    {
        private readonly PayloadProcessingChannel _payloadChannel;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<DataAcquisitionWorker> _logger;
        private const int MINIMUM_BUFFER_SIZE = 2048;

        public DataAcquisitionWorker(PayloadProcessingChannel payloadChannel,
            IHostApplicationLifetime applicationLifetime, ILogger<DataAcquisitionWorker> logger)
        {
            _payloadChannel = payloadChannel;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                if (!File.Exists(Constants.EXPECTED_ZIP_FILE_NAME))
                {
                    // The package hasn't arrived yet
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                using ZipArchive zipArchive = ZipFile.OpenRead(Constants.EXPECTED_ZIP_FILE_NAME);
                ZipArchiveEntry dataEntry = zipArchive.Entries.SingleOrDefault();
                Stream zipEntryStream = dataEntry.Open();

                StreamPipeReaderOptions streamOptions =
                    new StreamPipeReaderOptions(bufferSize: MINIMUM_BUFFER_SIZE);
                PipeReader pipeReader = PipeReader.Create(zipEntryStream, streamOptions);

                while (true)
                {
                    ReadResult result = await pipeReader.ReadAsync(cancellationToken);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // Notify the PipeReader how much buffer was used
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        JsonDocument documentFromBuffer = InspectBuffer(buffer);
                        _ = await _payloadChannel.AddPayloadAsync(documentFromBuffer, cancellationToken);
                        break;
                    }
                }

                await pipeReader.CompleteAsync();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return;
            }
        }

        private static JsonDocument InspectBuffer(ReadOnlySequence<byte> buffer)
        {
            JsonDocument result = null;
            foreach (ReadOnlyMemory<byte> segment in buffer)
            {
                Console.WriteLine($"Buffer Length {buffer.Length}");
                Console.WriteLine($"Segment Length {segment.Length}");

                Console.WriteLine(Encoding.UTF8.GetString(segment.Span));
                if (!buffer.IsSingleSegment)
                {
                    // log error
                    break;
                }

                result = JsonDocument.Parse(buffer, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = true
                });
            }

            return result;
        }
    }
}