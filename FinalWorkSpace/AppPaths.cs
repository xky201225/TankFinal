using System;
using System.IO;

namespace Peace_of_Tanks
{
    public static class AppPaths
    {
        private const string ProjectDirectoryName = "FinalWorkSpace";

        public static string BaseDirectory
        {
            get { return AppContext.BaseDirectory; }
        }

        public static string GetContentPath(params string[] relativeParts)
        {
            return Path.Combine(BaseDirectory, Path.Combine(relativeParts));
        }

        public static string FindContentPath(params string[] relativeParts)
        {
            string relativePath = Path.Combine(relativeParts);
            DirectoryInfo directory = new DirectoryInfo(BaseDirectory);

            while (directory != null)
            {
                string projectCandidate = Path.Combine(directory.FullName, ProjectDirectoryName, relativePath);
                if (File.Exists(projectCandidate))
                {
                    return projectCandidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find required content file.", relativePath);
        }

        public static string GetDataPath(string fileName)
        {
            return Path.Combine(BaseDirectory, fileName);
        }

        public static string LocalDatabaseConnectionString
        {
            get
            {
                string databasePath = FindContentPath("Database1.mdf");
                return "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=PeaceOfTanksData;AttachDbFilename=" + databasePath + ";Integrated Security=True";
            }
        }
    }
}
