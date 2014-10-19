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
        
        // Define constants for user state
        private const int STATE_CURRENTFILE = 0;
        private const int STATE_TOTALFILES = 1;
        private const int STATE_FILENAME = 2;

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
                    BeginConversion();
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
        /// Starts the conversion process
        /// </summary>
        private void BeginConversion()
        {

            Reset();
            string[] files = Directory.GetFiles(TextBoxFolderInput.Text);
            files = TruncateFiles(files);

            FolderInputPath = TextBoxFolderInput.Text;
            FolderOutputPath = TextBoxFolderOutput.Text;

            ProgressBarConvert.Maximum = files.Length;
            SetupBackgroundWorker();
            Worker.RunWorkerAsync(files);

            Converting = true;
            ButtonConvert.Content = "Cancel";
        }

        /// <summary>
        /// Prepares the background worker for a new job.
        /// 
        /// Prevent the UI from hanging
        /// </summary>
        private void SetupBackgroundWorker()
        {
            Worker.WorkerReportsProgress = true;
            Worker.WorkerSupportsCancellation = true;
            Worker.ProgressChanged += worker_ProgressChanged;
            Worker.DoWork += worker_DoWork;
            Worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        /// <summary>
        /// Triggered on the completion of a job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            OnWorkerCancelled(e.Cancelled);
            RemoveWorkerEvents(worker);

            Converting = false;
            ButtonConvert.Content = "Convert";
        }

        /// <summary>
        /// Writes to log based on worker cancellation status
        /// </summary>
        /// <param name="workerCancelled">if the worker was cancelled</param>
        private void OnWorkerCancelled(bool workerCancelled)
        {
            if (workerCancelled)
            {
                WriteToListBoxLog("Job has been cancelled!");
            }
            else
            {
                WriteToListBoxLog("Job has been completed!");
            }
        }

        /// <summary>
        /// Removes the events from the worker.
        /// 
        /// Prevents events from being repeated when the worker is recycled
        /// </summary>
        /// <param name="worker"></param>
        private void RemoveWorkerEvents(BackgroundWorker worker)
        {
            worker.ProgressChanged -= worker_ProgressChanged;
            worker.DoWork -= worker_DoWork;
            worker.RunWorkerCompleted -= worker_RunWorkerCompleted;
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

            WriteToListBoxLog("Converting Image (" 
                + userstate[STATE_CURRENTFILE] 
                + " / " + userstate[STATE_TOTALFILES] 
                + "): " 
                + userstate[STATE_FILENAME]);
            
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
                    index = SetupConversionProcess(files, worker, index);
                }
            }
        }

        /// <summary>
        /// Prepares the conversion process
        /// </summary>
        /// <param name="files">Files we will be converting</param>
        /// <param name="worker">Conversion worker</param>
        /// <param name="index">Current location in files to convert</param>
        /// <returns></returns>
        private int SetupConversionProcess(string[] files, BackgroundWorker worker, int index)
        {
            if (RunningProcesses < Environment.ProcessorCount)
            {
                string file = files[index];

                string exePath = Path.GetFullPath(@"Encoders\cwebp.exe");
                string output = FolderOutputPath + @"\" + Path.GetFileNameWithoutExtension(file) + ".webp";
                string args = "-lossless " + '"' + file + '"' + " -o " + '"' + output + '"';

                InitializeWebp(exePath, args);

                string[] userState = getNewUserState(files.Length, index, file);

                worker.ReportProgress(index + 1, userState);

                // Increment the counter variables.
                RunningProcesses++;
                index++;
            }
            else
            {
                System.Threading.Thread.Sleep(100);
            }
            return index;
        }

        /// <summary>
        /// Generates a new userstate based current file
        /// </summary>
        /// <param name="fileLength">Total number of files</param>
        /// <param name="index">Current index</param>
        /// <param name="file">Current file name</param>
        /// <returns></returns>
        private static string[] getNewUserState(int fileLength, int index, string file)
        {
            // This sends the information we want to display to the listbox.
            string[] userState = new string[3];
            userState[STATE_CURRENTFILE] =  (index + 1).ToString();
            userState[STATE_TOTALFILES] = fileLength.ToString();
            userState[STATE_FILENAME] = Path.GetFileName(file);
            return userState;
        }

        /// <summary>
        /// Sets up the Webp converter
        /// </summary>
        /// <param name="exePath">Location of the executable</param>
        /// <param name="args">Arguments to pass to converter</param>
        private void InitializeWebp(string exePath, string args)
        {
            // This is the code to start the cwebp.exe process with the wanted arguments.
            Process converter = new Process();
            converter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            converter.StartInfo.FileName = exePath;
            converter.StartInfo.Arguments = args;
            converter.EnableRaisingEvents = true;
            converter.Exited += new System.EventHandler(converter_Exited);
            converter.Start();
        }

        /// <summary>
        /// Executed when a converter process exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void converter_Exited(object sender, System.EventArgs e)
        {
            RunningProcesses--;
            CompletedFiles++;
        }
    }
}
