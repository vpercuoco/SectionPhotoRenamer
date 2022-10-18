using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace SectionPhotoRenamer
{
    partial class Program
    {
        public class FileNameChanger
        {
            public FileInfo FileInfo { get; set; }
            public Match RegexMatch { get; set; }
            public FileType FileType { get; set; }

            public string? NewName { get; set; } = null;
            public string OriginalName { get; set; }

            public string CheckSum { get; set; }

            public FileNameChanger(FileInfo fileInfo, Regex regex, bool processChecksum)
            {
                FileInfo = fileInfo;
                OriginalName = fileInfo.Name;
                if (processChecksum)
                {
                    CheckSum = GetSHA256(FileInfo);
                }
                else
                {
                    CheckSum = "";
                }

                PerformRegexMatch(regex);
                SetFileType();
                CreateNewFileName();
            }

            private string GetSHA256(FileInfo file)
            {
                byte[] hashValue = null;

                using (SHA256 checksum = SHA256.Create())
                {
                    using (FileStream fileStream = file.Open(FileMode.Open))
                    {
                        try
                        {
                            fileStream.Position = 0;
                            hashValue = checksum.ComputeHash(fileStream);
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine($"I/O Exception: {e.Message}");
                            throw e;
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            Console.WriteLine($"Access Exception: {e.Message}");
                        }
                    }
                }

                string hash = BitConverter.ToString(hashValue).Replace("-", "");
                return hash;
            }
            private void PerformRegexMatch(Regex regex)
            {
                RegexMatch = regex.Match(FileInfo.Name);
            }

            /// <summary>
            /// 
            /// </summary>
            private void SetFileType()
            {
                //Fail fast
                if (!RegexMatch.Success)
                {
                    FileType = FileType.NotValidFileType;
                    return;
                }

                if (RegexMatch.Groups["degree"].Success)
                {
                    FileType = FileType.WholeRoundQuarter;
                    return;
                }
                //360 core composites do not have a timestamp in their filenames, but section halves do
                else if (RegexMatch.Groups["datetime"].Success)
                {
                    FileType = FileType.SectionHalf;
                    return;
                }
                else
                {
                    FileType = FileType.WholeRoundComposite;
                    return;
                }
            }

            private void CreateNewFileName()
            {
                if (FileType == FileType.NotValidFileType)
                {
                    NewName = null;
                    return;
                }
                //Create filenames here
                if (FileType == FileType.SectionHalf)
                {
                    string[] arr = new string[] {
                        RegexMatch.Groups["expedition"].Value,
                        RegexMatch.Groups["site"].Value,
                        RegexMatch.Groups["core"].Value,
                        RegexMatch.Groups["section"].Value,
                        RegexMatch.Groups["extension"].Value
                    };

                    NewName = string.Format("{0}_{1}_{2}_{3}{4}", arr);
                    return;
                }
                if (FileType == FileType.WholeRoundQuarter)
                {
                    string[] arr = new string[] {
                        RegexMatch.Groups["expedition"].Value,
                        RegexMatch.Groups["site"].Value,
                        RegexMatch.Groups["core"].Value,
                        RegexMatch.Groups["section"].Value,
                        RegexMatch.Groups["degree"].Value,
                        RegexMatch.Groups["extension"].Value
                    };

                    NewName = string.Format("wrls_{0}_{1}_{2}_{3}_{4}{5}", arr);
                    return;
                }
                if (FileType == FileType.WholeRoundComposite)
                {
                    string[] arr = new string[] {
                        RegexMatch.Groups["expedition"].Value,
                        RegexMatch.Groups["site"].Value,
                        RegexMatch.Groups["core"].Value,
                        RegexMatch.Groups["section"].Value,
                        RegexMatch.Groups["extension"].Value
                    };

                    NewName = string.Format("wrlsc_{0}_{1}_{2}_{3}{4}", arr);
                    return;

                }
            }
        }
    }


}
