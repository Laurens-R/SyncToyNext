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
            frmEditor.Text = "Editor - " + filePath;
            frmEditor.Show();

        }

        private void menuDebugConsole_Click(object sender, EventArgs e)
        {
            webViewEditor.CoreWebView2.OpenDevToolsWindow();
        }

        private async void frmEditor_Load(object sender, EventArgs e)
        {
            await webViewEditor.EnsureCoreWebView2Async();

            webViewEditor.Source =
                   new Uri(System.IO.Path.Combine(
                   System.AppDomain.CurrentDomain.BaseDirectory,
                   @"Controls\Monaco\index.html"));

        }

        private async void webViewEditor_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.TryGetWebMessageAsString() == "monaco-ready")
            {
                var sourceUri = new Uri(FilePath);
                var sourceContent = JsonSerializer.Serialize(File.ReadAllText(FilePath));

                string changeContentScript = @$"
                    var currentModel = monaco.editor.createModel({sourceContent}, null, '{sourceUri}');
                    editor.setModel(currentModel);
                ";

                await webViewEditor.ExecuteScriptAsync(changeContentScript);
            }
        }
    }
}
