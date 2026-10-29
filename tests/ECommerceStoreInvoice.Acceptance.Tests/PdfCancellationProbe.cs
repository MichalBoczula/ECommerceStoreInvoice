using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Acceptance.Tests;

/// <summary>Holds one actual local PDF read at a known boundary; cancellation comes from RequestAborted.</summary>
internal sealed class PdfCancellationProbe(Action<string, object> record)
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _remaining = 1;
    internal string Mode { get; set; } = string.Empty;
    internal bool ServerCancelled { get; private set; }
    internal long BytesRead { get; private set; }
    internal bool ActualStreamDisposed { get; private set; }
    internal bool WrapNext() => Interlocked.Exchange(ref _remaining, 0) == 1;
    internal Task WaitEnteredAsync() => _entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
    internal Task WaitDisposedAsync() => _disposed.Task.WaitAsync(TimeSpan.FromSeconds(20));
    internal void Read(int count) => BytesRead += count;
    internal async Task BlockAsync(CancellationToken token)
    {
        _entered.TrySetResult();
        try { await Task.Delay(Timeout.InfiniteTimeSpan, token).WaitAsync(TimeSpan.FromSeconds(30)); }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            ServerCancelled = true;
            record("Technical server PDF cancellation observed", new { mode = Mode, serverTokenCancelled = true, bytesReadFromActualFile = BytesRead, clientResponseStatus = (int?)null });
            throw;
        }
    }
    internal void Disposed()
    {
        ActualStreamDisposed = true;
        record("Technical actual PDF stream disposed", new { mode = Mode, actualStreamDisposed = true, serverTokenCancelled = ServerCancelled, bytesReadFromActualFile = BytesRead });
        _disposed.TrySetResult();
    }
}

internal sealed class CancellableInvoicePdfService(IInvoicePdfService inner, PdfCancellationProbe probe) : IInvoicePdfService, IAsyncDisposable
{
    public Task<string> GenerateInvoicePdf(Guid invoiceId, Guid attemptId, Order order, IReadOnlyCollection<ProductVersion> products, ClientDataVersionResponseDto? client) => inner.GenerateInvoicePdf(invoiceId, attemptId, order, products, client);
    public Task DeleteGeneratedPdf(Guid invoiceId, Guid attemptId) => inner.DeleteGeneratedPdf(invoiceId, attemptId);
    public async Task<Stream?> OpenReadAsync(string url, CancellationToken token = default)
    {
        var stream = await inner.OpenReadAsync(url, token);
        if (stream is null || !probe.WrapNext()) return stream;
        if (probe.Mode == "before-headers")
        {
            try { await probe.BlockAsync(token); }
            finally { await stream.DisposeAsync(); probe.Disposed(); }
        }
        if (probe.Mode != "during-body") throw new InvalidOperationException("Unknown cancellation mode.");
        return new CancellablePdfStream(stream, probe);
    }
    public async ValueTask DisposeAsync() { if (inner is IAsyncDisposable disposable) await disposable.DisposeAsync(); }
}

internal sealed class CancellablePdfStream(Stream inner, PdfCancellationProbe probe) : Stream
{
    private bool _read;
    private int _disposed;
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (_read) await probe.BlockAsync(token);
        _read = true;
        var count = await inner.ReadAsync(buffer[..Math.Min(buffer.Length, 32)], token);
        probe.Read(count);
        return count;
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => ReadAsync(buffer.AsMemory(offset, count), token).AsTask();
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0) { inner.Dispose(); probe.Disposed(); }
        base.Dispose(disposing);
    }
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) { await inner.DisposeAsync(); probe.Disposed(); }
        GC.SuppressFinalize(this);
    }
}
