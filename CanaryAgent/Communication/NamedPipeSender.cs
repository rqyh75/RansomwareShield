using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CanaryAgent.Communication
{
    public class NamedPipeSender : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _pipeClient;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public bool IsConnected => _pipeClient?.IsConnected == true;

        public NamedPipeSender(string pipeName = "CanaryAgentPipe")
        {
            _pipeName = pipeName;
        }

        public async Task<bool> ConnectAsync(int timeoutMs = 5000)
        {
            _pipeClient?.Dispose();
            _pipeClient = null;

            try
            {
                _pipeClient = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);

                await _pipeClient.ConnectAsync(timeoutMs);
                Console.WriteLine("[Canary] Connected to Response Agent via named pipe");
                return true;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[Canary] Connection timed out - is Response Agent running?");
                _pipeClient?.Dispose();
                _pipeClient = null;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Canary] Failed to connect: {ex.Message}");
                _pipeClient?.Dispose();
                _pipeClient = null;
                return false;
            }
        }

        public async Task SendAlertAsync(CanaryAlert alert)
        {
            if (_disposed) return;

            string json = JsonSerializer.Serialize(alert);
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");

            await _sendLock.WaitAsync();
            try
            {
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    if (_pipeClient == null || !_pipeClient.IsConnected)
                    {
                        Console.WriteLine($"[Canary] Pipe not connected, reconnecting (attempt {attempt})...");
                        bool connected = await ConnectAsync(3000);

                        if (!connected)
                        {
                            if (attempt < 3)
                            {
                                await Task.Delay(500 * attempt);
                                continue;
                            }

                            Console.WriteLine("[Canary] Cannot send alert - Response Agent not reachable after 3 attempts");
                            return;
                        }
                    }

                    try
                    {
                        await _pipeClient!.WriteAsync(buffer, 0, buffer.Length);
                        await _pipeClient.FlushAsync();
                        Console.WriteLine($"[Canary] Alert sent: {alert.Action} on {System.IO.Path.GetFileName(alert.CanaryFile)}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Canary] Send failed (attempt {attempt}): {ex.Message}");
                        _pipeClient?.Dispose();
                        _pipeClient = null;

                        if (attempt < 3)
                            await Task.Delay(500 * attempt);
                    }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _pipeClient?.Dispose();
            _sendLock.Dispose();
        }
    }
}
