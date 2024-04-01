using System.Net.Http.Headers;
using KoeBook.Epub.Contracts.Services;

namespace KoeBook.Epub.Services;

public sealed class ScrapingClientService : IScrapingClientService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PeriodicTimer _periodicTimer;
    private readonly Queue<Func<HttpClient, Task>> _actionQueue = [];
    private bool _workerActivated;

    public ScrapingClientService(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _periodicTimer = new(TimeSpan.FromSeconds(10), timeProvider);
    }

    public Task<string> GetAsStringAsync(string url, CancellationToken ct)
    {
        var taskCompletion = new TaskCompletionSource<string>();

        lock (_actionQueue)
            _actionQueue.Enqueue(async httpClient =>
            {
                if (ct.IsCancellationRequested)
                    taskCompletion.SetCanceled(ct);

                try
                {
                    var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
                    taskCompletion.SetResult(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    taskCompletion.SetException(ex);
                }
            });

        EnsureWorkerActivated();

        return taskCompletion.Task;
    }

    public Task<ContentDispositionHeaderValue?> GetAsStreamAsync(string url, Stream destination, CancellationToken ct)
    {
        var taskCompletion = new TaskCompletionSource<ContentDispositionHeaderValue?>();

        lock (_actionQueue)
            _actionQueue.Enqueue(async httpClient =>
            {
                if (ct.IsCancellationRequested)
                    taskCompletion.SetCanceled(ct);

                try
                {
                    var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
                    await response.Content.CopyToAsync(destination, ct).ConfigureAwait(false);
                    taskCompletion.SetResult(response.Content.Headers.ContentDisposition);
                }
                catch (Exception ex)
                {
                    taskCompletion.SetException(ex);
                }
            });

        EnsureWorkerActivated();

        return taskCompletion.Task;
    }

    public Task DownloadToFileAsync(string url, string destPath, CancellationToken ct)
    {
        var taskCompletion = new TaskCompletionSource();

        lock (_actionQueue)
            _actionQueue.Enqueue(async httpClient =>
            {
                if (ct.IsCancellationRequested)
                    taskCompletion.SetCanceled(ct);

                try
                {
                    using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
                    await response.Content.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                    await fileStream.FlushAsync(ct).ConfigureAwait(false);
                    taskCompletion.SetResult();
                }
                catch (Exception ex)
                {
                    taskCompletion.SetException(ex);
                }
            });

        EnsureWorkerActivated();

        return taskCompletion.Task;
    }

    /// <summary>
    /// <see cref="Worker"/>が起動していない場合は起動します
    /// </summary>
    private void EnsureWorkerActivated()
    {
        bool activateWorker;
        lock (_actionQueue) activateWorker = !_workerActivated;

        if (activateWorker)
            Worker();
    }

    /// <summary>
    /// <see cref="_actionQueue"/>のConsumer
    /// 別スレッドでループさせるためにvoid
    /// </summary>
    private async void Worker()
    {
        lock (_actionQueue)
            _workerActivated = true;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");
            while (await _periodicTimer.WaitForNextTickAsync().ConfigureAwait(false) && _actionQueue.Count > 0)
            {
                Func<HttpClient, Task>? action;
                lock (_actionQueue)
                    if (!_actionQueue.TryDequeue(out action))
                        continue;

                await action(httpClient).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
        finally
        {
            lock (_actionQueue)
                _workerActivated = false;
        }
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}
