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

        private Dictionary<string, string> extensionLanguageMapping = new Dictionary<string, string>();

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

            extensionLanguageMapping.Add(".cs", "csharp");
            extensionLanguageMapping.Add(".c", "cpp");
            extensionLanguageMapping.Add(".cpp", "cpp");
            extensionLanguageMapping.Add(".h", "cpp");
            extensionLanguageMapping.Add(".hpp", "cpp");
            extensionLanguageMapping.Add(".bat", "bat");
            extensionLanguageMapping.Add(".htm", "html");
            extensionLanguageMapping.Add(".html", "html");
            extensionLanguageMapping.Add(".css", "css");
            extensionLanguageMapping.Add(".scss", "scss");
            extensionLanguageMapping.Add(".js", "javascript");
            extensionLanguageMapping.Add(".json", "javascript");
            extensionLanguageMapping.Add(".ts", "typescript");
            extensionLanguageMapping.Add(".tsx", "typescript");
            extensionLanguageMapping.Add(".java", "java");
            extensionLanguageMapping.Add(".go", "go");
            extensionLanguageMapping.Add(".rs", "rust");
            extensionLanguageMapping.Add(".less", "less");
            extensionLanguageMapping.Add(".php", "php");
            extensionLanguageMapping.Add(".yaml", "yaml");
            extensionLanguageMapping.Add(".cshtml", "razor");
            extensionLanguageMapping.Add(".lua", "lua");
            extensionLanguageMapping.Add(".ini", "ini");
            extensionLanguageMapping.Add(".ps1", "powershell");
            extensionLanguageMapping.Add(".py", "python");
            extensionLanguageMapping.Add(".r", "r");
            extensionLanguageMapping.Add(".md", "markdown");

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

                    var language = extensionLanguageMapping.ContainsKey(Path.GetExtension(FilePath)) ? $"'{extensionLanguageMapping[Path.GetExtension(FilePath)]}'" : "null";

                    string changeContentScript = @$"
                        var currentModel = monaco.editor.createModel({sourceContent}, {language}, '{sourceUri}');
                        editor.setModel(currentModel);
                    ";

                    await webViewEditor.ExecuteScriptAsync(changeContentScript);
                } else
                {
                    var sourceUri = new Uri(FilePath);
                    var otherSourceUri = new Uri(OtherFilePath);
                    var sourceContent = JsonSerializer.Serialize(File.ReadAllText(FilePath));
                    var otherSourceContent = JsonSerializer.Serialize(File.ReadAllText(OtherFilePath));

                    var language = extensionLanguageMapping.ContainsKey(Path.GetExtension(FilePath)) ? $"'{extensionLanguageMapping[Path.GetExtension(FilePath)]}'" : "null";
                    var otherLanguage = extensionLanguageMapping.ContainsKey(Path.GetExtension(OtherFilePath)) ? $"'{extensionLanguageMapping[Path.GetExtension(OtherFilePath)]}'" : "null";

                    string changeContentScript = @$"
                        var localModel = monaco.editor.createModel({sourceContent}, {language}, '{sourceUri}');
                        var remoteModel = monaco.editor.createModel({otherSourceContent}, {otherLanguage}, '{otherSourceUri}');
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
