using SyncToyNext.Core;
using SyncToyNext.GuiClient.Forms;
using System.IO.Compression;

namespace SyncToyNext.GuiClient
{
    public partial class frmMain : Form
    {
        private SyncPointManager? syncPointManager = null;
        private SyncPoint? currentSyncPoint = null;

        public frmMain()
        {
            InitializeComponent();
            fileBrowserLocal.PathChanged += FileBrowserLocal_PathChanged;
        }

        private void FileBrowserLocal_PathChanged(object? sender, EventArgs e)
        {
            lblLocalPath.Text = $"Local Path: {fileBrowserLocal.CurrentPath}";
        }

        private void menuOpenLocalLocation_Click(object sender, EventArgs e)
        {
            if (browserFolders.ShowDialog(this) == DialogResult.OK)
            {
                SessionContext.LocalFolderPath = browserFolders.SelectedPath;
                var files = Directory.GetFiles(browserFolders.SelectedPath, "*", SearchOption.AllDirectories);

                fileBrowserLocal.AllItemPaths = files;
                fileBrowserLocal.RootPath = SessionContext.LocalFolderPath;
                fileBrowserLocal.NavigateToPath(".");
                fileBrowserLocal.RefreshItems();

                if (RemoteConfig.RemoteConfigExists(SessionContext.LocalFolderPath))
                {
                    var remoteConfig = RemoteConfig.Load(SessionContext.LocalFolderPath);
                    if (remoteConfig == null) throw new InvalidOperationException("Remote configuration is invalid");
                    SessionContext.RemoteFolderPath = remoteConfig.RemotePath;

                    LoadRemote();
                }
                else
                {
                    MessageBox.Show("No remote configured for this location. Please select the remote location in the next dialog.");

                    var dialogResult = frmRemote.ShowRemoteDialog(this);

                    if(dialogResult == null)
                    {
                        throw new InvalidOperationException("Remote must be specified");
                    }
                    
                    SessionContext.RemoteFolderPath = dialogResult.RemotePath;

                    if(dialogResult.IsCompressed)
                    {
                        SessionContext.RemoteFolderPath = Path.Combine(SessionContext.RemoteFolderPath, Path.GetFileName(SessionContext.LocalFolderPath) + ".zip");
                    }
                    
                    var remoteConfig = new RemoteConfig(browserFolders.SelectedPath, SessionContext.LocalFolderPath);
                    remoteConfig.Save(SessionContext.LocalFolderPath);
                    SessionContext.RemoteFolderPath = browserFolders.SelectedPath;

                    LoadRemote();
                    
                }
            }
        }

        private void LoadRemote()
        {
            if (String.IsNullOrWhiteSpace(SessionContext.RemoteFolderPath) || String.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
            {
                throw new InvalidOperationException("Remote or Local folder path is not set.");
            }

            syncPointManager = new SyncPointManager(SessionContext.RemoteFolderPath, SessionContext.LocalFolderPath);
            var syncPoints = syncPointManager.SyncPoints;
            RefreshSyncPoints(syncPoints);
        }

        private void RefreshSyncPoints(IReadOnlyList<SyncPoint> syncPoints)
        {
            comboSyncPoints.Items.Clear();

            foreach (var syncpoint in syncPoints)
            {
                comboSyncPoints.Items.Add(syncpoint);
            }

            comboSyncPoints.SelectedIndex = 0;

            lblRemotePath.Text = SessionContext.RemoteFolderPath;
        }

        private void menuOpenRemoteLocation_Click(object sender, EventArgs e)
        {
            if (browserFolders.ShowDialog(this) == DialogResult.OK)
            {
                SessionContext.RemoteFolderPath = browserFolders.SelectedPath;
            }
        }

        private void comboSyncPoints_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentSyncPoint = comboSyncPoints.SelectedItem as SyncPoint;

            if (currentSyncPoint == null) throw new InvalidOperationException("Syncpoint could not be loaded.");
            if (syncPointManager == null) throw new InvalidOperationException("Syncpoint Manager not initialized.");

            var syncPointEntries = syncPointManager
                                        .GetFileEntriesAtSyncpoint(currentSyncPoint.SyncPointId)
                                        .Where(x => x.EntryType == SyncPointEntryType.AddOrChanged);

            fileBrowserRemote.AllItemPaths = syncPointEntries;
            //fileBrowserRemote.RootPath = SessionContext.RemoteFolderPath;
            fileBrowserRemote.NavigateToPath(".");
            fileBrowserRemote.RefreshItems();
        }

        private void OpenFileUsingDefaultHandler(string filePath)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

        private void fileBrowserLocal_DoubleClickSelectedItem(object sender, EventArgs e)
        {
            var selectedItem = fileBrowserLocal.SelectedItems.FirstOrDefault() as String;
            if (selectedItem != null)
            {
                //open the file with the default associated program
                OpenFileUsingDefaultHandler(selectedItem);
            }
        }

        private void fileBrowserRemote_DoubleClickSelectedItem(object sender, EventArgs e)
        {
            var selectedItem = fileBrowserRemote.SelectedItems.FirstOrDefault() as SyncPointEntry;

            if (selectedItem != null)
            {
                var pathParts = selectedItem.RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                var relativePath = pathParts[0];

                if (pathParts.Length > 0)
                {
                    var remoteFolderPath = Path.GetDirectoryName(SessionContext.RemoteFolderPath);

                    if (remoteFolderPath == null) throw new InvalidOperationException("Remote folder path could not be retrieved");

                    var zipPath = Path.Combine(remoteFolderPath, pathParts[1]);
                    using var zip = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                    using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                    var entryPath = relativePath.Replace("\\", "/");
                    var zipEntry = archive.GetEntry(entryPath);

                    var tempFolder = Path.GetTempPath();
                    var tempPath = Path.Combine(tempFolder, Path.GetFileName(relativePath));

                    if (zipEntry != null)
                    {
                        zipEntry.ExtractToFile(tempPath, true);
                        OpenFileUsingDefaultHandler(tempPath);
                    }
                }
                else
                {
                    if(SessionContext.RemoteFolderPath == null)
                    {
                        throw new InvalidOperationException("Remote folder path is not set.");
                    }

                    var fullPath = Path.Combine(SessionContext.RemoteFolderPath, relativePath);

                    if (File.Exists(fullPath))
                    {
                        OpenFileUsingDefaultHandler(fullPath);
                    }
                }
            }
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            if(syncPointManager != null)
            { 
                try
                {
                    if (SessionContext.LocalFolderPath == null)
                        throw new InvalidOperationException("No Local Folder Path known. Please load a local folder first.");

                    if (SessionContext.RemoteFolderPath == null)
                        throw new InvalidOperationException("No remote specified for local path.");

                    var result = frmPush.ShowPushDialog(this);

                    if (result == null) return;

                    ManualRun.Run(SessionContext.LocalFolderPath, SessionContext.RemoteFolderPath, true, result.ID, result.Description);

                    syncPointManager.RefreshSyncPoints();
                    RefreshSyncPoints(syncPointManager.SyncPoints);

                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before pushing.");
                }
            }
        }

        private void menuChangeRemote_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                throw new InvalidOperationException("Cannot load remote config if no local folder path is loaded");

            var existingRemoteConfig = RemoteConfig.Load(SessionContext.LocalFolderPath);

            if (existingRemoteConfig == null)
                throw new InvalidOperationException("Could not load remote config.");

            var result = frmRemote.ShowRemoteDialog(this, existingRemoteConfig);

            if(result != null)
            {
                var currentRemotePath = SessionContext.RemoteFolderPath;
                SessionContext.RemoteFolderPath = result.RemotePath;

                if(result.IsCompressed)
                {
                    SessionContext.RemoteFolderPath = Path.Combine(SessionContext.RemoteFolderPath, Path.GetFileName(SessionContext.LocalFolderPath) + ".zip");
                }

                existingRemoteConfig.RemotePath = SessionContext.RemoteFolderPath;
                existingRemoteConfig.Save(SessionContext.LocalFolderPath);

                LoadRemote();
            }
        }
    }
}
