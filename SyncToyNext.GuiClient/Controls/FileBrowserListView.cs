using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncToyNext.GuiClient
{
    public partial class FileBrowserListView : UserControl
    {
        private IEnumerable<object> _itemPaths = Array.Empty<object>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string RootPath { get; set; } = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<object> AllItemPaths
        {
            get => _itemPaths;
            set
            {
                _itemPaths = value ?? Array.Empty<object>();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentPath { get; private set; } = ".";

        public IEnumerable<object> SelectedItems
        {
            get
            {
                var selectedItems = new List<object>();

                for (int i = 0; i < listEntries.SelectedItems.Count; i++)
                {
                    var item = listEntries.SelectedItems[i];
                    if (item != null && item.Tag != null)
                    {
                        selectedItems.Add(item.Tag);
                    }
                }

                return selectedItems;
            }
        }

        public IEnumerable<FileBrowserEntry> ItemsAtCurrentPath
        {
            get
            {
                HashSet<string> itemsAtPath = new HashSet<string>();
                var results = new List<FileBrowserEntry>();

                foreach (var pathItem in _itemPaths)
                {
                    if (pathItem == null) continue;
                    var pathItemString = pathItem.ToString();

                    if (string.IsNullOrWhiteSpace(pathItemString))
                    {
                        continue; // Skip null or empty paths
                    }

                    var partialPath = !String.IsNullOrWhiteSpace(RootPath) ? Path.GetRelativePath(RootPath, pathItemString) : pathItem.ToString();

                    if (string.IsNullOrWhiteSpace(partialPath))
                    {
                        continue; // Skip null or empty paths
                    }

                    if (!partialPath.StartsWith(CurrentPath + "\\") && CurrentPath != ".")
                    {
                        continue;
                    }

                    if (partialPath == CurrentPath)
                        continue;

                    var itemToProcess = CurrentPath != "." ? partialPath.Replace(CurrentPath + Path.DirectorySeparatorChar, string.Empty) : partialPath;

                    var pathParts = itemToProcess.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (pathParts.Length > 0)
                    {
                        if (!itemsAtPath.Contains(pathParts[0]))
                        {
                            itemsAtPath.Add(pathParts[0]);
                            results.Add(new FileBrowserEntry
                            {
                                DisplayValue = pathParts[0],
                                Value = pathItem
                            });
                        }
                    }
                }

                var comparer = Comparer<FileBrowserEntry>.Create((x, y) => {
                    bool isXFolder = !Path.HasExtension(x.DisplayValue) || x.DisplayValue.StartsWith(".");
                    bool isYFolder = !Path.HasExtension(y.DisplayValue) || y.DisplayValue.StartsWith(".");

                    int folderCompare = isYFolder.CompareTo(isXFolder); // folders first (true < false)
                    if(folderCompare != 0)
                    {
                        return folderCompare;
                    }

                    var stringDifference = string.Compare(x.DisplayValue, y.DisplayValue, StringComparison.OrdinalIgnoreCase);
                    return stringDifference;
                }); 


                //sort the items based on folder type
                results.Sort(comparer);

                return results;
            }
        }

        public event EventHandler? PathChanged;
        public event EventHandler? DoubleClickSelectedItem;

        public void NavigateToPath(string? relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
            }

            if (relativePath == "..")
            {
                if (!string.IsNullOrWhiteSpace(CurrentPath))
                {
                    if (!String.IsNullOrWhiteSpace(RootPath))
                    {
                        var parentDirectory = Directory.GetParent(Path.Combine(RootPath, CurrentPath));
                        if (parentDirectory == null)
                        {
                            throw new InvalidOperationException("Cannot navigate to parent directory from root.");
                        }

                        CurrentPath = Path.GetRelativePath(RootPath, parentDirectory.FullName);
                    } else
                    {
                        var pathParts = CurrentPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                        CurrentPath = Path.Combine(pathParts.SkipLast(1).ToArray());
                        if(String.IsNullOrWhiteSpace(CurrentPath)) CurrentPath = ".";
                    }
                }
            }
            else
            {
                if(CurrentPath == ".")
                {
                    CurrentPath = relativePath;
                }
                 else
                {
                    CurrentPath = Path.Combine(CurrentPath, relativePath);
                }
                
            }

            PathChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshItems()
        {
            var browserEntries = ItemsAtCurrentPath;

            listEntries.Items.Clear();

            if (browserEntries == null || !browserEntries.Any())
            {
                listEntries.Items.Add(new ListViewItem("Please open a location in the File menu."));
                return;
            }

            if (CurrentPath != ".")
            {
                var navigateUpItem = new ListViewItem("..");
                navigateUpItem.ImageKey = "folderWhite"; // Assuming you have an image for navigating up
                listEntries.Items.Add(navigateUpItem);
            }

            foreach (var entry in browserEntries)
            {
                bool isFolder = Path.HasExtension(entry.ToString()) == false || entry.ToString().StartsWith(".");

                var newItem = new ListViewItem(entry.ToString());
                newItem.SubItems.Add(isFolder ? "[ FOLDER ]" : Path.GetExtension(entry.ToString()));
                newItem.ImageKey = isFolder ? "folderWhite" : "fileWhite";
                newItem.Tag = entry.Value;
                listEntries.Items.Add(newItem);
            }
        }

        public FileBrowserListView()
        {
            InitializeComponent();
        }

        public void Reset()
        {
            listEntries.Items.Clear();
            Array.Empty<object>();
        }

        private void listEntries_DoubleClick(object sender, EventArgs e)
        {
            if(listEntries.SelectedItems.Count > 0)
            {
                var selectedItem = listEntries.SelectedItems[0];
                
                if (Path.HasExtension(selectedItem.Text) && !selectedItem.Text.StartsWith("."))
                {
                    DoubleClickSelectedItem?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    NavigateToPath(selectedItem.Text);
                    RefreshItems();
                }
            }
        }
    }
}
