using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncToyNext.GuiClient.Forms
{
    public partial class frmEditor : Form
    {
        bool isReady = false;

        public bool IsReady => isReady;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FilePath { get; set; } = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OtherFilePath { get; set; } = string.Empty;

        public frmEditor()
        {
            InitializeComponent();
            webViewEditor.CoreWebView2InitializationCompleted += (object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e) =>
            {
                if (e.IsSuccess)
                {
                    isReady = true;
                }
            };
        }

        public static void ShowEditor(string filePath)
        {

            frmEditor frmEditor = new frmEditor();
            frmEditor.FilePath = filePath;
            frmEditor.Text = "Viewer - " + filePath;
            frmEditor.Show();

        }

        public static void ShowEditor(string localPath, string remotePath)
        {

            frmEditor frmEditor = new frmEditor();
            frmEditor.FilePath = localPath;
            frmEditor.OtherFilePath = remotePath;
            frmEditor.Text = "Diff Viewer - " + Path.GetFileName(localPath);
            frmEditor.Show();

        }

        private void menuDebugConsole_Click(object sender, EventArgs e)
        {
            webViewEditor.CoreWebView2.OpenDevToolsWindow();
        }

        private async void frmEditor_Load(object sender, EventArgs e)
        {
            await webViewEditor.EnsureCoreWebView2Async();

            if (string.IsNullOrWhiteSpace(OtherFilePath))
            {
                webViewEditor.Source =
                       new Uri(System.IO.Path.Combine(
                       System.AppDomain.CurrentDomain.BaseDirectory,
                       @"Controls\Monaco\editor.html"));
            } else
            {
                webViewEditor.Source =
                      new Uri(System.IO.Path.Combine(
                      System.AppDomain.CurrentDomain.BaseDirectory,
                      @"Controls\Monaco\differ.html"));
            }

        }

        private async void webViewEditor_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.TryGetWebMessageAsString() == "monaco-ready")
            {
                if (String.IsNullOrEmpty(OtherFilePath))
                {
                    var sourceUri = new Uri(FilePath);
                    var sourceContent = JsonSerializer.Serialize(File.ReadAllText(FilePath));

                    string changeContentScript = @$"
                        var currentModel = monaco.editor.createModel({sourceContent}, null, '{sourceUri}');
                        editor.setModel(currentModel);
                    ";

                    await webViewEditor.ExecuteScriptAsync(changeContentScript);
                } else
                {
                    var sourceUri = new Uri(FilePath);
                    var otherSourceUri = new Uri(OtherFilePath);
                    var sourceContent = JsonSerializer.Serialize(File.ReadAllText(FilePath));
                    var otherSourceContent = JsonSerializer.Serialize(File.ReadAllText(OtherFilePath));

                    string changeContentScript = @$"
                        var localModel = monaco.editor.createModel({sourceContent}, null, '{sourceUri}');
                        var remoteModel = monaco.editor.createModel({otherSourceContent}, null, '{otherSourceUri}');
                        diffEditor.setModel({{
                            original: remoteModel,
                            modified: localModel
                        }});
                    ";

                    await webViewEditor.ExecuteScriptAsync(changeContentScript);
                }
            }
        }
    }
}
