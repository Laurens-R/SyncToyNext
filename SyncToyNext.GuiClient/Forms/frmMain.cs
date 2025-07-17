using SyncToyNext.Core;
using SyncToyNext.GuiClient.Forms;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SyncToyNext.GuiClient
{
    public partial class frmMain : Form
    {
        private SyncPointManager? syncPointManager = null;
        private SyncPoint? currentSyncPoint = null;
        private List<string> acceptedTextExtensions = new List<string>();

        public frmMain()
        {
            InitializeComponent();
            fileBrowserLocal.PathChanged += FileBrowserLocal_PathChanged;
            UserIO.OnErrorReceivedHandler = (string message, string? ex) =>
            {
                MessageBox.Show(message, "Oops...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            acceptedTextExtensions.Add(".txt");
            acceptedTextExtensions.Add(".html");
            acceptedTextExtensions.Add(".htm");
            acceptedTextExtensions.Add(".md");
            acceptedTextExtensions.Add(".json");
            acceptedTextExtensions.Add(".xml");
            acceptedTextExtensions.Add(".cs");
            acceptedTextExtensions.Add(".cpp");
            acceptedTextExtensions.Add(".c");
            acceptedTextExtensions.Add(".h");
            acceptedTextExtensions.Add(".hpp");
            acceptedTextExtensions.Add(".h++");
            acceptedTextExtensions.Add(".c++");
            acceptedTextExtensions.Add(".js");
            acceptedTextExtensions.Add(".css");
            acceptedTextExtensions.Add(".ts");
            acceptedTextExtensions.Add(".tsx");
            acceptedTextExtensions.Add(".py");
            acceptedTextExtensions.Add(".java");
            acceptedTextExtensions.Add(".php");
            acceptedTextExtensions.Add(".rb");
            acceptedTextExtensions.Add(".go");
            acceptedTextExtensions.Add(".sh");
            acceptedTextExtensions.Add(".bat");
            acceptedTextExtensions.Add(".ps1");
            acceptedTextExtensions.Add(".sql");
            acceptedTextExtensions.Add(".yaml");
            acceptedTextExtensions.Add(".yml");
            acceptedTextExtensions.Add(".log");
            acceptedTextExtensions.Add(".conf");
            acceptedTextExtensions.Add(".ini");
            acceptedTextExtensions.Add(".properties");
            acceptedTextExtensions.Add(".mdx");
            acceptedTextExtensions.Add(".txt");
            acceptedTextExtensions.Add(".csv");
            acceptedTextExtensions.Add(".tsv");
            acceptedTextExtensions.Add(".bas");
            acceptedTextExtensions.Add(".sh");
            acceptedTextExtensions.Add(".rs");
        }

        frmLog? logForm = null;

        private void ShowLog()
        {
            if (logForm == null || logForm.IsDisposed)
            {
                logForm = new frmLog();

            }

            if (!logForm.Visible)
                logForm.Show(this);
        }

        private void FileBrowserLocal_PathChanged(object? sender, EventArgs e)
        {
            lblLocalPath.Text = $"Local Path: {fileBrowserLocal.CurrentPath}";
        }

        private void RefreshLocalFolderBrowser()
        {
            var files = Directory.GetFiles(browserFolders.SelectedPath, "*", SearchOption.AllDirectories);

            fileBrowserLocal.AllItemPaths = files;
            fileBrowserLocal.RootPath = SessionContext.LocalFolderPath;
            fileBrowserLocal.NavigateToPath(".");
            fileBrowserLocal.RefreshItems();
        }

        private void ResetState()
        {
            syncPointManager = null;
            currentSyncPoint = null;
            SessionContext.LocalFolderPath = string.Empty;
            SessionContext.RemoteFolderPath = string.Empty;
            comboSyncPoints.Items.Clear();
            fileBrowserLocal.Reset();
            fileBrowserRemote.Reset();
        }

        private void menuOpenLocalLocation_Click(object sender, EventArgs e)
        {
            try
            {
                if (browserFolders.ShowDialog(this) == DialogResult.OK)
                {
                    SessionContext.LocalFolderPath = browserFolders.SelectedPath;

                    RefreshLocalFolderBrowser();

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

                        if (dialogResult == null)
                        {
                            throw new InvalidOperationException("Remote must be specified");
                        }

                        SessionContext.RemoteFolderPath = dialogResult.RemotePath;

                        if (dialogResult.IsCompressed)
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
            catch (Exception ex)
            {
                ResetState();
                UserIO.Error(ex.Message);
            }
        }

        private void LoadRemote()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(SessionContext.RemoteFolderPath) || String.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                {
                    throw new InvalidOperationException("Remote or Local folder path is not set.");
                }

                syncPointManager = new SyncPointManager(SessionContext.RemoteFolderPath, SessionContext.LocalFolderPath);
                var syncPoints = syncPointManager.SyncPoints;
                RefreshSyncPoints(syncPoints);
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
                throw;
            }
        }

        private void RefreshSyncPoints(IReadOnlyList<SyncPoint> syncPoints)
        {
            try
            {
                comboSyncPoints.Items.Clear();

                foreach (var syncpoint in syncPoints)
                {
                    comboSyncPoints.Items.Add(syncpoint);
                }

                comboSyncPoints.SelectedIndex = 0;

                lblRemotePath.Text = SessionContext.RemoteFolderPath;
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
                throw;
            }
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
            try
            {
                currentSyncPoint = comboSyncPoints.SelectedItem as SyncPoint;

                if (currentSyncPoint == null) throw new InvalidOperationException("Syncpoint could not be loaded.");
                if (syncPointManager == null) throw new InvalidOperationException("Syncpoint Manager not initialized.");

                var syncPointEntries = syncPointManager
                                            .GetFileEntriesAtSyncpoint(currentSyncPoint.SyncPointId)
                                            .Where(x => x.EntryType == SyncPointEntryType.AddOrChanged);

                fileBrowserRemote.AllItemPaths = syncPointEntries;
                fileBrowserRemote.NavigateToPath(".");
                fileBrowserRemote.RefreshItems();
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
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
            try
            {
                var selectedItem = fileBrowserLocal.SelectedItems.FirstOrDefault() as String;
                if (selectedItem != null)
                {
                    if (!TryOpenInBuiltInEditor(selectedItem))
                    {
                        OpenFileUsingDefaultHandler(selectedItem);
                    }
                }
            }
            catch (Exception ex)
            {
                UserIO.Error("Something went wrong when trying to open the local file using the default handler for the file type.");
            }
        }

        private void fileBrowserRemote_DoubleClickSelectedItem(object sender, EventArgs e)
        {
            try
            {
                var selectedItem = fileBrowserRemote.SelectedItems.FirstOrDefault() as SyncPointEntry;

                if (selectedItem != null)
                {
                    OpenRemoteItem(selectedItem);
                }
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        private bool TryOpenInBuiltInEditor(string filePath)
        {
            if (File.Exists(filePath))
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (acceptedTextExtensions.Contains(extension))
                {
                    frmEditor.ShowEditor(filePath);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private void OpenRemoteItem(SyncPointEntry selectedItem)
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

                    if (!TryOpenInBuiltInEditor(tempPath))
                    {
                        OpenFileUsingDefaultHandler(tempPath);
                    }
                }
            }
            else
            {
                if (SessionContext.RemoteFolderPath == null)
                {
                    throw new InvalidOperationException("Remote folder path is not set.");
                }

                var fullPath = Path.Combine(SessionContext.RemoteFolderPath, relativePath);

                if (File.Exists(fullPath))
                {
                    if (!TryOpenInBuiltInEditor(fullPath))
                    {
                        OpenFileUsingDefaultHandler(fullPath);
                    }
                }
            }
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            if (syncPointManager != null)
            {
                try
                {
                    if (SessionContext.LocalFolderPath == null)
                        throw new InvalidOperationException("No Local Folder Path known. Please load a local folder first.");

                    if (SessionContext.RemoteFolderPath == null)
                        throw new InvalidOperationException("No remote specified for local path.");

                    var result = frmPush.ShowPushDialog(this);

                    if (result == null) return;

                    UserIO.Message("Starting push to remote location.");

                    ManualRun.Run(SessionContext.LocalFolderPath, SessionContext.RemoteFolderPath, true, result.ID, result.Description);

                    syncPointManager.RefreshSyncPoints();
                    RefreshSyncPoints(syncPointManager.SyncPoints);

                    UserIO.Message("Completed push to remote location.");

                    ShowLog();

                }
                catch (Exception ex)
                {
                    UserIO.Error(ex.Message);
                }
            }
        }

        private void menuChangeRemote_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                    throw new InvalidOperationException("Cannot load remote config if no local folder path is loaded");

                var existingRemoteConfig = RemoteConfig.Load(SessionContext.LocalFolderPath);

                if (existingRemoteConfig == null)
                    throw new InvalidOperationException("Could not load remote config.");

                var result = frmRemote.ShowRemoteDialog(this, existingRemoteConfig);

                if (result != null)
                {
                    UserIO.Message("Changing configured remote location.");
                    var currentRemotePath = SessionContext.RemoteFolderPath;
                    SessionContext.RemoteFolderPath = result.RemotePath;

                    if (result.IsCompressed)
                    {
                        SessionContext.RemoteFolderPath = Path.Combine(SessionContext.RemoteFolderPath, Path.GetFileName(SessionContext.LocalFolderPath) + ".zip");
                    }

                    existingRemoteConfig.RemotePath = SessionContext.RemoteFolderPath;
                    existingRemoteConfig.Save(SessionContext.LocalFolderPath);

                    LoadRemote();
                    UserIO.Message("Completed configuring remote location.");
                }
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        private void menuLog_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            try
            {
                var questionResult = MessageBox.Show("Are you sure you want to restore the selected syncpoint? This will undo any changes in the local folder since that restore point.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (questionResult == DialogResult.Yes)
                {
                    var selectedSyncPoint = comboSyncPoints.SelectedItem as SyncPoint;

                    if (selectedSyncPoint == null)
                    {
                        throw new InvalidOperationException("No valid syncpoint could be retrieved");
                    }

                    if (String.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                    {
                        throw new InvalidOperationException("No valid remote path was opened");
                    }

                    UserIO.Message($"Starting with restore of syncpoint {selectedSyncPoint.SyncPointId}");

                    SyncPointRestorer.RestorePath = SessionContext.LocalFolderPath;
                    SyncPointRestorer.Run(selectedSyncPoint.SyncPointId);

                    RefreshLocalFolderBrowser();

                    ShowLog();
                }
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        private void menuContextEditor_Click(object sender, EventArgs e)
        {
            if(fileBrowserRemote.SelectedItems.Count() > 0)
            {
                var selectedItem = fileBrowserRemote.SelectedItems.FirstOrDefault() as SyncPointEntry;
                if (selectedItem != null) {
                    OpenRemoteItem(selectedItem);
                }

            }
        }
    }
}
