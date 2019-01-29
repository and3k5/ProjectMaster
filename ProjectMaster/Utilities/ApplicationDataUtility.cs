using System;
using System.IO;

namespace ProjectMaster.Utilities
{
    public static class ApplicationDataUtility
    {
        public static string GetBaseFolder(bool createIfMissing = false)
        {
            var baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "And3k5", "ProjectMaster");
            if (createIfMissing)
                CreatePathIfMissing(baseFolder);
            return baseFolder;
        }

        public static DirectoryInfo CreatePathIfMissing(string folderPath)
        {
            var directory = new DirectoryInfo(folderPath);
            if (!directory.Exists)
                directory.Create();
            return directory;
        }
    }
}