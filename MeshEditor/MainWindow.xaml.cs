using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace MeshEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        string path;
        public MainWindow() {
            InitializeComponent();
            path = "";
            mExit.Click += (s, e) => Close();
            mCompile.Click += Compile;
            mListBox.SelectionChanged += mListBox_SelectionChanged;
            KeyDown += Shorcuts;
        }

        void mListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            //System.Windows.MessageBox.Show(mTextBox.GetLineText(7));
            //int n = LineNumber();
            //int count = 0;
            //int startindex = 0;
            //int endindex = 0;
            //for (int i = 0; i < n; i++) {
            //    if (i == n - 1) {
            //        startindex = count;
            //        endindex = mTextBox.GetLineText(i).Length - 2;
            //        System.Windows.MessageBox.Show(endindex.ToString());
            //    }
            //    count += mTextBox.GetLineText(i).Length + 2;
            //}
            //System.Windows.MessageBox.Show(endindex.ToString());
            //mTextBox.SelectionStart = startindex;
            //mTextBox.SelectionLength = endindex;
            //startindex = mTextBox.GetCharacterIndexFromLineIndex(n - 1);
            //endindex = mTextBox.GetLineLength(n - 1) - 2;
            //System.Windows.MessageBox.Show(endindex.ToString());
            //mTextBox.SelectionStart = startindex;
            //mTextBox.SelectionLength = endindex;

        }

        int LineNumber() {
            int i = 10;
            string tmp = "";
            while (char.IsDigit(mListBox.SelectedItem.ToString()[i])) {
                tmp += mListBox.SelectedItem.ToString()[i];
                i++;
            }
            return int.Parse(tmp);
        }

        void Shorcuts(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.Control) {
                mCompile.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent, mCompile));
            }

            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) {
                mOpen.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent, mOpen));
            }

            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) {
                mSave.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent, mSave));
            }

            else if (e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.Control) {
                mDocument.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent, mDocument));
            }
        }

        void Document(object sender, RoutedEventArgs e) {
            mListBox.Items.Clear();
            Save(sender, e);
            int nExitCode;
            string FileName = @"D:\Old Projects\WEB\BuildFolder\mWeave.exe";
            List<string> Errors = CallExternalEXE(FileName, out nExitCode);
            if (nExitCode == 0)
                mListBox.Items.Add("Successfully Documented");
            foreach (string s in Errors)
                mListBox.Items.Add(s);
        }

        void Compile(object sender, RoutedEventArgs e) {
            mListBox.Items.Clear();
            Save(sender, e);
            int nExitCode;
            string FileName = @"D:\Old Projects\WEB\BuildFolder\mTangle.exe";
            List<string> Errors = CallExternalEXE(FileName, out nExitCode);
            foreach (string s in Errors)
                mListBox.Items.Add(s);
            if (nExitCode == 0 && mListBox.Items.Count == 0)
                mListBox.Items.Add("Successfully Compiled");
        }



        List<string> CallExternalEXE(string filename, out int Exitcode) {
            List<string> tmp = new List<string>();
            ProcessStartInfo psi = new ProcessStartInfo() { CreateNoWindow = false, UseShellExecute = false, WorkingDirectory = Directory.GetCurrentDirectory(), FileName = filename, WindowStyle = ProcessWindowStyle.Hidden, Arguments = path, RedirectStandardError = true, RedirectStandardOutput = true };
            Process pr = Process.Start(psi);
            if (pr == null) throw new IOException(string.Format("Cannot Execute {0}", filename));
            pr.OutputDataReceived += (s, e) => { if (e.Data != null) tmp.Add(e.Data); };
            pr.ErrorDataReceived += (s, e) => { if (e.Data != null) tmp.Add(e.Data); };
            pr.BeginOutputReadLine(); pr.BeginErrorReadLine();
            pr.WaitForExit(); Exitcode = pr.ExitCode;
            pr.Close();
            return tmp;
        }

        void Save(object sender, RoutedEventArgs e) {
            if (path == "")
                SaveAs(sender, e);
            else {
                using (StreamWriter sw = new StreamWriter(path)) {
                    sw.Write(mTextBox.Text);
                }
            }
        }

        void SaveAs(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog() { DefaultExt = ".m" };
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                path = Path.GetFileName(sfd.FileName);
                using (StreamWriter sw = new StreamWriter(path)) {
                    sw.Write(mTextBox.Text);
                }
            }
        }

        void Open(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                StreamReader sd = new StreamReader(path = Path.GetFileName(ofd.FileName));
                string text = sd.ReadToEnd();
                mTextBox.Text = text;
            }
        }
    }
}

