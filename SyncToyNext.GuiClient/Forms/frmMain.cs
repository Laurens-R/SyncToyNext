using SyncToyNext.Core;
using SyncToyNext.Core.Helpers;
using SyncToyNext.Core.SyncPoints;
using SyncToyNext.Core.UX;
using SyncToyNext.GuiClient.Forms;
using System.Reflection;

namespace SyncToyNext.GuiClient
{
    public partial class frmMain : Form
    {
        private SyncPoint? currentSyncPoint = null;

        private Repository? repository = null;

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
            if (repository == null || string.IsNullOrEmpty(repository.LocalPath)) return;
            lblLocalPath.Text = $"Local path: {Path.Combine(repository.LocalPath, fileBrowserLocal.CurrentPath)}";
        }

        private void fileBrowserRemote_PathChanged(object sender, EventArgs e)
        {
            if (repository == null) return;
            lblRemotePath.Text = $"Remote path: {Path.Combine(repository.RemotePath, fileBrowserRemote.CurrentPath)}";
        }

        private void RefreshLocalFolderBrowser()
        {
            if (repository == null || String.IsNullOrEmpty(repository.LocalPath))
            {
                UserIO.Error("Local folder path is not set. Please select a local folder first.");
                return;
            }

            var files = repository.GetLocalFiles();

            fileBrowserLocal.AllItemPaths = files;
            fileBrowserLocal.RootPath = repository.LocalPath;
            fileBrowserLocal.NavigateToPath(".");
            fileBrowserLocal.RefreshItems();

            if (repository.LatestSyncPoint != null)
                txtLocalSyncPoint.Text = repository.LatestSyncPoint.ToString();
        }

        private void RefreshRemoteFolderBrowserAfterInit()
        {
            if (repository == null || String.IsNullOrEmpty(repository.LocalPath))
            {
                UserIO.Error("Local folder path is not set. Please select a local folder first.");
                return;
            }

            if (repository.LatestSyncPoint == null)
            {
                UserIO.Error("No syncpoints present in remote. Either it's not a valid remote or it has not been properly been initialized as such.");
                return;
            }

            var files = repository.GetRemoteFiles(repository.LatestSyncPoint.SyncPointId);

            fileBrowserRemote.AllItemPaths = files;
            fileBrowserRemote.RootPath = string.Empty;
            fileBrowserRemote.NavigateToPath(".");
            fileBrowserRemote.RefreshItems();

            if (currentSyncPoint != null)
                txtLocalSyncPoint.Text = repository.LatestSyncPoint.ToString();
        }

        private void ResetClientState()
        {
            repository = null;
            currentSyncPoint = null;
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
                    ResetClientState();
                    var localPath = browserFolders.SelectedPath;

                    try
                    {
                        repository = new Repository(localPath);
                    }
                    catch
                    {
                        if (MessageBox.Show("Repository has not been initialized. Do you want to proceed to initialize it?", "Init repo?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            var dialogResult = frmRemote.ShowRemoteDialog(this);

                            if (dialogResult == null)
                            {
                                throw new InvalidOperationException("Remote must be specified");
                            }

                            var progressForm = frmProgress.ShowProgressDialog(this);
                            Repository.UpdateProgressHandler = progressForm.UpdateHandler;
                            repository = Repository.Initialize(localPath, dialogResult.RemotePath, dialogResult.IsCompressed);
                            Repository.UpdateProgressHandler = null;
                            progressForm.Close();
                        }
                        else
                        {
                            ResetClientState();
                            return;
                        }
                    }

                    RefreshLocalFolderBrowser();
                    RefreshRemoteFolderBrowserAfterInit();
                    RefreshSyncPoints(repository.SyncPoints);
                }
            }
            catch (Exception ex)
            {
                ResetClientState();
                UserIO.Error(ex.Message);
            }
        }

        private void RefreshSyncPoints(IReadOnlyList<SyncPoint> syncPoints)
        {
            try
            {
                if (repository == null) throw new InvalidOperationException("A valid repository must be loaded before refreshing syncpoints.");

                comboSyncPoints.Items.Clear();

                foreach (var syncpoint in syncPoints)
                {
                    comboSyncPoints.Items.Add(syncpoint);
                }

                if (syncPoints.Count > 0) comboSyncPoints.SelectedIndex = 0;

                txtLocalSyncPoint.Text = syncPoints.First(sp => sp.SyncPointId == repository.LocalSyncPointID).ToString();
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
                throw;
            }
        }

        private void comboSyncPoints_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                currentSyncPoint = comboSyncPoints.SelectedItem as SyncPoint;

                if (currentSyncPoint == null) throw new InvalidOperationException("Syncpoint could not be loaded.");
                if (repository == null) throw new InvalidOperationException("Local repository is not initialized.");

                var syncPointEntries = repository.GetRemoteFiles(currentSyncPoint.SyncPointId);

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
                if (FileHelpers.IsAcceptedTextExtension(extension))
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
                if (FileHelpers.IsAcceptedTextExtension(extension) && FileHelpers.IsAcceptedTextExtension(otherExtension))
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
            if (repository == null) return;
            if (currentSyncPoint == null) return;

            string tempCopyPath = repository.GetTempCopyOfFile(selectedItem, currentSyncPoint);

            if (!TryOpenInBuiltInEditor(tempCopyPath))
            {
                OpenFileUsingDefaultHandler(tempCopyPath);
            }
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            if (repository != null)
            {
                try
                {
                    var result = frmPush.ShowPushDialog(this);
                    if (result == null) return;

                    var progressForm = frmProgress.ShowProgressDialog(this);
                    Repository.UpdateProgressHandler = progressForm.UpdateHandler;

                    UserIO.Message("Starting push to remote location.");
                    repository.Push(result.ID, result.Description);
                    RefreshSyncPoints(repository.SyncPoints);

                    Repository.UpdateProgressHandler = null;
                    progressForm.Close();

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
                if (repository == null || repository.Config == null) return;
                var remoteConfigChanges = frmRemote.ShowRemoteDialog(this, repository.Config);

                if (remoteConfigChanges != null)
                {
                    repository.ChangeRemote(remoteConfigChanges.RemotePath, remoteConfigChanges.IsCompressed);

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

                    if (selectedSyncPoint != null && repository != null)
                    {
                        var progressForm = frmProgress.ShowProgressDialog(this);
                        Repository.UpdateProgressHandler = progressForm.UpdateHandler;
                        repository.Restore(selectedSyncPoint.SyncPointId);
                        Repository.UpdateProgressHandler = null;
                        progressForm.Close();
                    }

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

                if (selectedItem == null || repository == null || String.IsNullOrWhiteSpace(repository.LocalPath))
                {
                    return;
                }

                if (currentSyncPoint == null) return;

                var localPath = Path.Combine(repository.LocalPath, selectedItem.SourcePath);
                var tempRemotePath = repository.GetTempCopyOfFile(selectedItem, currentSyncPoint);

                TryOpenInBuiltInDiffer(localPath, tempRemotePath);
            }
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

                    if (repository == null) throw new InvalidOperationException("Local repository should be initialized at this point.");

                    repository.RestoreMultipleEntriesFromSyncPoint(
                        remoteSelectedItems,
                        currentSyncPoint);

                    UserIO.Message("Completed restoring individual items from syncpoint.");
                    ShowLog();
                }
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        private void menuFileManualMerge_Click(object sender, EventArgs e)
        {
            if (frmManualMerge.ShowMergeDialog(this) == DialogResult.OK)
            {
                ShowLog();
            }
        }

        private void menuRemoteClone_Click(object sender, EventArgs e)
        {
            var result = frmClone.ShowCloneDialog();

            if (result.Result == Models.CloneResult.Completed)
            {
                try
                {
                    MessageBox.Show("Succesfully cloned repository.");

                    repository = new Repository(result.NewLocalPath);
                    RefreshLocalFolderBrowser();
                    RefreshRemoteFolderBrowserAfterInit();
                    RefreshSyncPoints(repository.SyncPoints);
                }
                catch
                {
                    MessageBox.Show("Something went wrong when opening the newly cloned local repo.");
                }
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            if(currentSyncPoint != null)
            {
                frmSyncPointDetails.ShowSyncPointDetailsDialog(currentSyncPoint, this);
            }
        }
    }
}
