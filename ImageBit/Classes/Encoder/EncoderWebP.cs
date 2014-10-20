using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageBit.Classes.Encoder
{
    class EncoderWebP
    {
        int RunningProcesses = 0;
        int CompletedFiles = 0;

        string FolderOutputPath;

        public EncoderWebP()
        {

        }

        public EncoderWebP(string folderOutputPath)
        {
            FolderOutputPath = folderOutputPath;
        }

        /// <summary>
        /// Converts images to the webp format.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="files"></param>
        /// <param name="e"></param>
        public void ConvertToWebP(object sender, string[] files, DoWorkEventArgs e)
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
            userState[(int)CurrentState.CurrentFile] = (index + 1).ToString();
            userState[(int)CurrentState.TotalFiles] = fileLength.ToString();
            userState[(int)CurrentState.FileName] = Path.GetFileName(file);
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
