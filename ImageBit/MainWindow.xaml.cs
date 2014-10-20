using ImageBit.Classes;
using ImageBit.Classes.Encoder;
using Ookii.Dialogs.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace ImageBit
{
    enum CurrentState
    {
        CurrentFile = 0,
        TotalFiles = 1,
        FileName = 2
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VistaFolderBrowserDialog FolderInput = new VistaFolderBrowserDialog();
        VistaFolderBrowserDialog FolderOutput = new VistaFolderBrowserDialog();

        BackgroundWorker Worker = new BackgroundWorker();

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
            files = ImageBitHelper.TruncateFiles(files);

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
                + userstate[(int)CurrentState.CurrentFile] 
                + " / " + userstate[(int)CurrentState.TotalFiles] 
                + "): " 
                + userstate[(int)CurrentState.FileName]);
            
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

            EncoderWebP encoder = new EncoderWebP(FolderOutputPath);

            encoder.ConvertToWebP(sender, files, e);
        }
    }
}
