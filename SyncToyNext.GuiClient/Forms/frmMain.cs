using SyncToyNext.Core;
using SyncToyNext.Core.Runners;
using SyncToyNext.Core.UX;
using SyncToyNext.GuiClient.Forms;
using SyncToyNext.GuiClient.Helpers;
using SyncToyNext.GuiClient.Models;
using System.IO.Compression;
using System.Runtime.InteropServices;

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
            UserIO.OnErrorReceivedHandler = (string message, string? ex) =>
            {
                MessageBox.Show(message, "Oops...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
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
            if (string.IsNullOrEmpty(SessionContext.LocalFolderPath)) return;
            lblLocalPath.Text = $"Local path: {Path.Combine(SessionContext.LocalFolderPath, fileBrowserLocal.CurrentPath)}";
        }

        private void RefreshLocalFolderBrowser()
        {
            if (String.IsNullOrEmpty(SessionContext.LocalFolderPath))
            {
                UserIO.Error("Local folder path is not set. Please select a local folder first.");
                return;
            }

            var files = Directory.GetFiles(browserFolders.SelectedPath, "*", SearchOption.AllDirectories);

            fileBrowserLocal.AllItemPaths = files;
            fileBrowserLocal.RootPath = SessionContext.LocalFolderPath;
            fileBrowserLocal.NavigateToPath(".");
            fileBrowserLocal.RefreshItems();
        }

        private void ResetClientState()
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
                ResetClientState();
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

                if (syncPoints.Count > 0) comboSyncPoints.SelectedIndex = 0;

                lblRemotePath.Text = "Remote path: " + Path.GetDirectoryName(SessionContext.RemoteFolderPath);
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
            catch (Exception)
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
                if (ClientHelpers.IsAcceptedTextExtension(extension))
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

        private bool TryOpenInBuiltInDiffer(string filePath, string otherPath)
        {
            if (File.Exists(filePath) && File.Exists(otherPath))
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var otherExtension = Path.GetExtension(otherPath).ToLowerInvariant();
                if (ClientHelpers.IsAcceptedTextExtension(extension) && ClientHelpers.IsAcceptedTextExtension(otherExtension))
                {
                    frmEditor.ShowEditor(filePath, otherPath);
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
            string remotePath = ClientHelpers.RetrieveRemoteItem(selectedItem);

            if (!TryOpenInBuiltInEditor(remotePath))
            {
                OpenFileUsingDefaultHandler(remotePath);
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

                    ManualRunner.Run(SessionContext.LocalFolderPath, SessionContext.RemoteFolderPath, true, result.ID, result.Description);

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

                var remoteConfigChanges = frmRemote.ShowRemoteDialog(this, existingRemoteConfig);

                if (remoteConfigChanges != null)
                {
                    ClientHelpers.ApplyRemoteConfigChanges(existingRemoteConfig, remoteConfigChanges);

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
                    ClientHelpers.RestoreSyncPoint(selectedSyncPoint);

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
            if (fileBrowserRemote.SelectedItems.Count() > 0)
            {
                var selectedItem = fileBrowserRemote.SelectedItems.FirstOrDefault() as SyncPointEntry;
                if (selectedItem != null)
                {
                    OpenRemoteItem(selectedItem);
                }
            }
        }

        private void menuContextCompareLocal_Click(object sender, EventArgs e)
        {
            if (fileBrowserRemote.SelectedItems.Count() > 0)
            {
                var selectedItem = fileBrowserRemote.SelectedItems.FirstOrDefault() as SyncPointEntry;
                
                if (selectedItem == null || String.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                {
                    return;
                }

                var localPath = Path.Combine(SessionContext.LocalFolderPath, selectedItem.SourcePath);
                var remoteItem = ClientHelpers.RetrieveRemoteItem(selectedItem);

                TryOpenInBuiltInDiffer(localPath, remoteItem);
            }
        }

        private void fileBrowserRemote_PathChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SessionContext.RemoteFolderPath)) return;
            lblRemotePath.Text = $"Remote path: {Path.Combine(Path.GetDirectoryName(SessionContext.RemoteFolderPath) ?? SessionContext.RemoteFolderPath, fileBrowserRemote.CurrentPath)}";
        }

        private void contextMenuRestoreItems_Click(object sender, EventArgs e)
        {
            try
            {

                if (MessageBox.Show("Are you sure you want to restore the selected items? This will overwrite the files on the local repository.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var remoteSelectedItems = fileBrowserRemote.SelectedItems.Select(x => (SyncPointEntry)x);
                    var currentSyncPoint = comboSyncPoints.SelectedItem as SyncPoint;

                    UserIO.Message("Started restoring individual items from syncpoint.");

                    if (String.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
                    {
                        throw new InvalidOperationException("Local folder path should not be empty.");
                    }

                    ClientHelpers.RestoreMultipleEntriesFromSyncPoint(
                        remoteSelectedItems, 
                        currentSyncPoint, 
                        SessionContext.LocalFolderPath, 
                        SessionContext.RemoteFolderPath, 
                        SessionContext.LocalFolderPath);

                    UserIO.Message("Completed restoring individual items from syncpoint.");
                    ShowLog();
                }
            } catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }
    }
}
