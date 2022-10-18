using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Serilog;
using System.Text;
using CsvHelper;
using System.Globalization;

namespace SectionPhotoRenamer
{
    partial class Program
    {
        static void Main(string[] args)
        {

#if DEBUG  //Keeping this section here in case code needs to be run as a script
            string inputDirectory = @"D:\to_be_added\EXP397T-TIF\";
            string logFile = @"D:\to_be_added\EXP397T_TIFF_NAME_CHANGES.txt";
            string outputDirectory = @"D:\to_be_added\organized\";
            bool moveFiles = true;
            bool processChecksum = false;
#else
            string inputDirectory = args[0];
            string logFile = args[1];
            string outputDirectory = args[2];
            bool moveFiles = bool.Parse(args[3])
            bool processChecksum = bool.Parse(args[4])
#endif

            var log = new FileInfo(logFile);
            if (log.Exists)
            {
                log.Delete();
            }

            Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                        .WriteTo.Console()
                        .WriteTo.File(logFile)
                        .CreateLogger();

            Log.Information("Processing files...");


            var files = ProcessFileNames(inputDirectory, null, outputDirectory, processChecksum, moveFiles);

            //Logfile contains old names and new names in case they must be switched
            Log.Information("original_name, new_name");

            // Output a summary table
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("original_name");
            dt.Columns.Add("new_name");
            dt.Columns.Add("checksum");

            foreach (var file in files)
            {
                Log.Information($"{file.OriginalName},{ file.NewName}");
                DataRow row = dt.NewRow();
                row["original_name"] = file.OriginalName;
                row["new_name"] = file.NewName;
                row["checksum"] = file.CheckSum;
                dt.Rows.Add(row);
                dt.AcceptChanges();
            }
          
            FileInfo changedNamesFile = new FileInfo(Path.Combine(outputDirectory, "changed_names.csv"));
            WriteDataTableToFile(dt, changedNamesFile);

            
        }

        public static void WriteDataTableToFile(DataTable dataTable, FileInfo filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath.FullName))
            {
                using (CsvWriter csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    //Write headers
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        csvWriter.WriteField(column.ColumnName);
                    }
                    csvWriter.NextRecord();

                    //Write data
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            csvWriter.WriteField(dataRow[i]);
                        }
                        csvWriter.NextRecord();
                    }
                }
            }
        }

        public static List<FileNameChanger> ProcessFileNames(string directory, string pattern, string outputDirectory, bool processChecksum, bool moveFiles)
        {
            if (pattern == null)
            {
                // original pattern
                // pattern = "(?<expedition>[0-9a-z]+)-(?<site>[a-z][0-9]+[a-z])-(?<core>[0-9]+[a-z])-(?<section>[0-9]+|cc)-(?<half>[a|w])_(?<textid>[a-z]+[0-9]+)_?(?<datetime>([0-9]+)(_)?(?<degree>[0-9]+)?)?(_WRLS|_WRLSC)?(?<extension>.tif)";
                
                // changed for 397T onwards. Uses more quantifiers "?" to account for names which may contain groups after section (half, text id, etc.)
                // Also uses quantifiers for some of the separating characters "-", and "_"
                pattern = "(?<expedition>[0-9a-z]+)-(?<site>[a-z][0-9]+[a-z])-(?<core>[0-9]+[a-z])-(?<section>[0-9]+|cc)-?(?<half>[a|w])?_?(?<textid>[a-z]+[0-9]+)_?(?<datetime>([0-9]+)(_)?(?<degree>[0-9]+)?)?(_WRLS|_WRLSC)?(?<extension>.tif)";
            }
            Regex regex = new Regex(pattern);

            var files = Directory.GetFiles(directory).Select(x => new FileNameChanger(new FileInfo(x), regex,processChecksum)).ToList();

            Log.Information($"Total Files: {files.Count}");
            Log.Information($"Section Halves: {files.Where(x => x.FileType == FileType.SectionHalf).Count()}");
            Log.Information($"Whole Round Composites: {files.Where(x => x.FileType == FileType.WholeRoundComposite).Count()}");
            Log.Information($"Whole Round Quarter: {files.Where(x => x.FileType == FileType.WholeRoundQuarter).Count()}");
            Log.Information($"Poorly Named Files: {files.Where(x => x.FileType == FileType.NotValidFileType).Count()}");
            Log.Information("Poor File(s):");
            foreach (var item in files.Where(x => x.FileType == FileType.NotValidFileType))
            {
                Log.Information(item.FileInfo.Name);
            }

            // Create directories:
            if (moveFiles)
            {
                Log.Information("Moving files");
                CreateDirectories(files, outputDirectory);
                Log.Information("Finished moving files");
            }


            return files;
        }


        public static void CreateDirectories(IEnumerable<FileNameChanger> files, string workingDirectory)
        {
            //determine sites:
            DirectoryInfo directory = null;
            var sites = files.Where(x => x.FileType != FileType.NotValidFileType).Select(x => x.RegexMatch.Groups["site"].Value).Distinct().ToList();

            if (!sites.Any())
            {
                return;
            }

            Dictionary<FileType, string> folders = new Dictionary<FileType, string>()
            {
                { FileType.SectionHalf,"core_photos" },
                { FileType.WholeRoundComposite,"wrlsc" },
                { FileType.WholeRoundQuarter,"wrls" },
                { FileType.NotValidFileType,"errorfiles" }

            };

            //filetype collection does not contain NotValidFileType, those files will not be moved
            var fileTypes = files.Where(x => x.FileType != FileType.NotValidFileType).Select(x => x.FileType).Distinct();

            foreach (FileType fileType in fileTypes)
            {
                foreach (var site in sites)
                {
                    directory = Directory.CreateDirectory(string.Format($@"{workingDirectory}\{site}\{folders[fileType]}\"));
                    RenameAndMoveFiles(files, fileType, site, directory);
                }
            }
        }

        public static void RenameAndMoveFiles(IEnumerable<FileNameChanger> files, FileType fileType, string site, DirectoryInfo directory)
        {
            foreach (var file in files.Where(x => x.FileType == fileType).Where(x => x.RegexMatch.Groups["site"].Value == site).Select(x => x))
            {
                //catch a duplicate file. File.IO. Exception. Need to look into how to change the filename in increment
                var runningCheck = new FileInfo(Path.Combine(directory.FullName, file.NewName));
                var originalName = new FileInfo(Path.Combine(directory.FullName, file.NewName));

                //test for existence
                int index = 1;
                //increment filename if file already exists, use original name so that multiple underscores are not appended for dups, i.e. _1_2_3_4.tiff etc
                while (runningCheck.Exists)
                {
                    runningCheck = new FileInfo(IncrementFileName(originalName.FullName, index));
                    index++;
                }

                file.NewName = runningCheck.Name;

                file.FileInfo.MoveTo(Path.Combine(directory.FullName, file.NewName));
            }
        }

        public static string? IncrementFileName(string fileName, int index)
        {
            return $@"{Path.GetDirectoryName(fileName)}\{Path.GetFileNameWithoutExtension(fileName)}_{index}{Path.GetExtension(fileName)}";
        }
    }


}
