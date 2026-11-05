using Velopack;
using Velopack.Sources;

namespace ScreenshotMaker.Services
{
    internal sealed class AppUpdater
    {
        private readonly UpdateManager _manager =
            new(new GithubSource(AppInfo.RepositoryUrl, accessToken: null, prerelease: false));

        public event EventHandler<UpdateInfo>? UpdateDownloaded;

        public bool IsInstalled => _manager.IsInstalled;

        public UpdateInfo? DownloadedUpdate { get; private set; }

        public async Task<UpdateInfo?> CheckAsync()
        {
            if (!IsInstalled)
            {
                return null;
            }

            return await _manager.CheckForUpdatesAsync();
        }

        public async Task DownloadAsync(UpdateInfo update, Action<int>? progress)
        {
            await _manager.DownloadUpdatesAsync(update, progress);
            DownloadedUpdate = update;
            UpdateDownloaded?.Invoke(this, update);
        }

        public void ApplyAndRestart()
        {
            if (DownloadedUpdate is not null)
            {
                _manager.ApplyUpdatesAndRestart(DownloadedUpdate.TargetFullRelease);
            }
        }
    }
}
