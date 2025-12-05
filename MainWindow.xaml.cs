using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Windows.Threading;

namespace MD5Compare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Timer to poll the clipboard
        private readonly DispatcherTimer _clipboardTimer;
        private string _lastClipboardText = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            this.AllowDrop = true;

            // Monitor the clipboard every second
            _clipboardTimer = new DispatcherTimer();
            _clipboardTimer.Interval = TimeSpan.FromSeconds(1);
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();
        }

        // Return the hash digest of a file
        public string HashFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = MD5.Create().ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        // Return the hash value of a specific file stored in the specified digest file
        public string GetHashFromFile(string digestFilePath, string targetFileName)
        {
            // Loop through every line in the digest file    
            string[] lines = File.ReadAllLines(digestFilePath);
            foreach (string str in lines)
            {
                // Get the substrings that are separated by a space    
                string[] parts = str.Split(' ');
                if (parts.GetLength(0) == 1)
                {
                    // There is no filename in the digest, so the MD5 is assumed to be the correct one    
                    return parts[0];

                }
                else if (parts[1] == targetFileName)
                {
                    return parts[0];
                }
            }

            return "No matching MD5 digest found in file";
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Return if the filename is the same as what we already have
            if (files[0] == tbMainFileName.Text) return;

            tbCompareFileName.Text = tbMainFileName.Text;
            tbCompareFileMD5.Text = tbMainFileMD5.Text;

            // Prepare the main file text boxes
            tbMainFileName.Text = files[0];
            tbMainFileMD5.Clear();

            // Look for an MD5 file associated to the new main file
            tbMD5FileName.Text = files[0] + ".md5";
            if (File.Exists(tbMD5FileName.Text))
            {
                tbMD5FileContents.Text = GetHashFromFile(tbMD5FileName.Text, System.IO.Path.GetFileName(tbMainFileName.Text)).ToUpper();

            }
            else
            {
                tbMD5FileContents.Text = "** File Does Not Exist **";

            }

            // Perform hash function on the main file
            this.Cursor = Cursors.Wait;
            tbMainFileMD5.Text = HashFile(files[0]);
            this.Cursor = Cursors.Arrow;
            Update_Status();
        }

        private void Update_Status()
        {
            if (tbMainFileMD5.Text == string.Empty)
            {
                tbMD5FileStatus.Text = string.Empty;
                tbComparisonFileStatus.Text = string.Empty;
                tbClipboardStatus.Text = string.Empty;
                return;
            }

            if (tbMD5FileContents.Text == tbMainFileMD5.Text)
            {
                tbMD5FileStatus.Text = "\u2713";
                tbMD5FileStatus.Foreground = Brushes.Green;
            }
            else if ((tbMD5FileContents.Text == string.Empty) || (tbMD5FileContents.Text.Substring(0, 1) == "*"))
            {
                tbMD5FileStatus.Text = string.Empty;

            }
            else
            {
                tbMD5FileStatus.Text = "X";
                tbMD5FileStatus.Foreground = Brushes.Red;

            }

            if (tbCompareFileMD5.Text == tbMainFileMD5.Text)
            {
                tbComparisonFileStatus.Text = "\u2713";
                tbComparisonFileStatus.Foreground = Brushes.Green;
            }
            else if (tbCompareFileMD5.Text == string.Empty)
            {
                tbComparisonFileStatus.Text = string.Empty;

            }
            else
            {
                tbComparisonFileStatus.Text = "X";
                tbComparisonFileStatus.Foreground = Brushes.Red;

            }

            if (tbClipboardMD5.Text == tbMainFileMD5.Text)
            {
                tbClipboardStatus.Text = "\u2713";
                tbClipboardStatus.Foreground = Brushes.Green;
            }
            else if (tbClipboardMD5.Text == string.Empty)
            {
                tbClipboardStatus.Text = string.Empty;

            }
            else
            {
                tbClipboardStatus.Text = "X";
                tbClipboardStatus.Foreground = Brushes.Red;

            }

        }

        // Poll the system clipboard every second and update tbClipboardMD5 when the clipboard text changes
        private void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            string clipText = string.Empty;
            try
            {
                if (Clipboard.ContainsText())
                {
                    clipText = Clipboard.GetText();
                }
            }
            catch
            {
                // Clipboard may be in use by another process; ignore this tick
                return;
            }

            if (clipText == null) clipText = string.Empty;

            if (!string.Equals(clipText, _lastClipboardText, StringComparison.Ordinal))
            {
                _lastClipboardText = clipText;

                // Take the first 32 characters and paste into the textbox
                string first32 = clipText.Length <= 32 ? clipText : clipText.Substring(0, 32);
                tbClipboardMD5.Text = first32.Trim().ToUpper();
                Update_Status();
            }
        }

        // Clear button click handler to reset UI to initial empty state
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            // Stop timer while clearing to avoid race with clipboard ticks
            try
            {
                if (_clipboardTimer != null && _clipboardTimer.IsEnabled)
                {
                    _clipboardTimer.Stop();
                }
            }
            catch
            {
                // ignore
            }

            tbClipboardMD5.Text = string.Empty;
            tbClipboardStatus.Text = string.Empty;
            tbMainFileName.Text = string.Empty;
            tbMainFileMD5.Text = string.Empty;
            tbMD5FileName.Text = string.Empty;
            tbMD5FileContents.Text = string.Empty;
            tbMD5FileStatus.Text = string.Empty;
            tbCompareFileName.Text = string.Empty;
            tbCompareFileMD5.Text = string.Empty;
            tbComparisonFileStatus.Text = string.Empty;

            _lastClipboardText = string.Empty;

            Update_Status();

            try
            {
                _clipboardTimer?.Start();
            }
            catch
            {
                // ignore
            }
        }

    }
}
