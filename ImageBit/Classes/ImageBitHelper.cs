using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageBit.Classes
{
    static class ImageBitHelper
    {
        /// <summary>
        /// Gets rid of the unwanted files, such as those which are not images.
        /// </summary>
        /// <param name="files"></param>
        /// <returns>An array of wanted filenames.</returns>
        static public string[] TruncateFiles(string[] files)
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
    }
}
