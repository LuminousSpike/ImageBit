using System.Windows;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace ImageBit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VistaFolderBrowserDialog FolderInput = new VistaFolderBrowserDialog();
        VistaFolderBrowserDialog FolderOutput = new VistaFolderBrowserDialog();

        BackgroundWorker Worker = new BackgroundWorker();

        int RunningProcesses = 0;
        int CompletedFiles = 0;

        string FolderInputPath;
        string FolderOutputPath;
        bool Converting = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Open folder button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFolderInputOpen_Click(object sender, RoutedEventArgs e)
        {
            if (FolderInput.ShowDialog() == true)
            {
                TextBoxFolderInput.Text = FolderInput.SelectedPath;
            }
        }

        /// <summary>
        /// Output folder button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFolderOutputOpen_Click(object sender, RoutedEventArgs e)
        {
            if (FolderOutput.ShowDialog() == true)
            {
                TextBoxFolderOutput.Text = FolderOutput.SelectedPath;
            }
        }

        /// <summary>
        /// Reset any class scope variables, and clear the listbox.
        /// </summary>
        private void Reset()
        {
            ListBoxLog.Items.Clear();
            RunningProcesses = 0;
            CompletedFiles = 0;
        }

       /// <summary>
       /// Begins the conversion process when the button is pressed
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void ButtonConvert_Click(object sender, RoutedEventArgs e)
        {
            if (!Converting)
            {
                try
                {
                    Reset();
                    string[] files = Directory.GetFiles(TextBoxFolderInput.Text);
                    files = TruncateFiles(files);

                    FolderInputPath = TextBoxFolderInput.Text;
                    FolderOutputPath = TextBoxFolderOutput.Text;

                    ProgressBarConvert.Maximum = files.Length;

                    // The worker for converting the files, this is so the UI does not hang.
                    Worker.WorkerReportsProgress = true;
                    Worker.WorkerSupportsCancellation = true;
                    Worker.ProgressChanged += worker_ProgressChanged;
                    Worker.DoWork += worker_DoWork;
                    Worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                    Worker.RunWorkerAsync(files);

                    Converting = true;
                    ButtonConvert.Content = "Cancel";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                Worker.CancelAsync();
            }
        }

        /// <summary>
        /// Triggered on the completion of a job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (e.Cancelled)
            {
                WriteToListBoxLog("Job has been cancelled!");
            }
            else
            {
                WriteToListBoxLog("Job has been completed!");
            }
            // We need to remove the events, otherwise they will be repeated on the next run of the program.
            worker.ProgressChanged -= worker_ProgressChanged;
            worker.DoWork -= worker_DoWork;
            worker.RunWorkerCompleted -= worker_RunWorkerCompleted;

            Converting = false;
            ButtonConvert.Content = "Convert";
        }

        /// <summary>
        /// Update the progress bar when progress changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarConvert.Value = e.ProgressPercentage;
            string[] userstate = e.UserState as string[];

            WriteToListBoxLog("Converting Image (" + userstate[0] + " / " + userstate[1] + "): " + userstate[2]);
            
        }

        /// <summary>
        /// Convenience method for writing to log
        /// </summary>
        /// <param name="message"></param>
        void WriteToListBoxLog(string message)
        {
            ListBoxLog.Items.Add(message);
            ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
        }

        /// <summary>
        /// Start a new worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            string[] files = e.Argument as string[];
            ConvertToWebP(sender, files, e);
        }

        /// <summary>
        /// Gets rid of the unwanted files, such as those which are not images.
        /// </summary>
        /// <param name="files"></param>
        /// <returns>An array of wanted filenames.</returns>
        private string[] TruncateFiles(string[] files)
        {
            List<string> filesList = new List<string>();

            foreach (string file in files)
            {
                if (file.ToLower().Contains(".png"))
                {
                    filesList.Add(file);
                }
            }

            return filesList.ToArray();
        }

        /// <summary>
        /// Converts images to the webp format.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="files"></param>
        /// <param name="e"></param>
        private void ConvertToWebP(object sender, string[] files, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            int index = 0;

            while (index < files.Length)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    if (RunningProcesses < Environment.ProcessorCount)
                    {
                        string file = files[index];

                        string exePath = Path.GetFullPath(@"Encoders\cwebp.exe");
                        string output = FolderOutputPath + @"\" + Path.GetFileNameWithoutExtension(file) + ".webp";
                        string arguements = "-lossless " + '"' + file + '"' + " -o " + '"' + output + '"';

                        // This is the code to start the cwebp.exe process with the wanted arguments.
                        Process converter = new Process();
                        converter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        converter.StartInfo.FileName = exePath;
                        converter.StartInfo.Arguments = arguements;
                        converter.EnableRaisingEvents = true;
                        converter.Exited += new System.EventHandler(converter_Exited);
                        converter.Start();

                        // This sends the information we want to display to the listbox.
                        string[] userState = { (index + 1).ToString(), files.Length.ToString(), Path.GetFileName(file) };

                        worker.ReportProgress(index + 1, userState);

                        // Increment the counter variables.
                        RunningProcesses++;
                        index++;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Executed when a converter process exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void converter_Exited(object sender, System.EventArgs e)
        {
            RunningProcesses--;
            CompletedFiles++;
        }
    }
}
