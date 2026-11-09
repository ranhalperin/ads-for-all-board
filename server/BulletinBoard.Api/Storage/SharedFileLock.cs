using System.Diagnostics;
using BulletinBoard.Api.Errors;

namespace BulletinBoard.Api.Storage;

/// <summary>
/// A lock shared by every server that mounts the same folder: it opens <c>{name}.lock</c> with
/// <see cref="FileShare.None"/>. The OS (and, on a network share, the NFS or SMB file server) refuses a
/// second exclusive open from any process on any machine until the first handle is closed.
///
/// The exclusive open can't wait, so this retries with <c>await Task.Delay</c>: no thread is held while
/// waiting. If a server dies while holding the lock, its handle is closed by the OS (on NFS, when the
/// client's lease expires), so the lock can't stay stuck the way a "create a marker file" lock can.
/// </summary>
public sealed class SharedFileLock(StorageFolder folder, int retryMinMs = 25, int retryMaxMs = 100) : IDistributedLock
{
    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken ct)
    {
        var path = folder.LockFile(name);
        var waited = Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var handle = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    bufferSize: 1, FileOptions.None);
                return new Releaser(handle);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (waited.Elapsed >= timeout) throw new StorageBusyException();
                var pause = TimeSpan.FromMilliseconds(Random.Shared.Next(retryMinMs, retryMaxMs + 1));
                var left = timeout - waited.Elapsed;
                await Task.Delay(pause < left ? pause : left, ct);
            }
        }
    }

    private sealed class Releaser(FileStream handle) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => handle.DisposeAsync();
    }
}
