using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CriPakTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CriPakTools\n");
            Console.WriteLine("根据Falo在Xentax论坛上发布的代码(请参阅readme.txt),由FuwaNovels的Nanashi3修改.\nEpelKnight的插入代码");

            if (args.Length == 0)
            {
                Console.WriteLine("CriPakTool使用方法:\n");
                Console.WriteLine("CriPakTool.exe 输入文件 EXTRACT_ME - 提取文件.\n");
                Console.WriteLine("CriPakTool.exe 输入文件 ALL - 提取所有文件.\n");
                Console.WriteLine("CriPakTool.exe 输入文件 REPLACE_ME REPLACE_WITH [输出文件] - 用REPLACE_ME代替REPLACE_WITH.可选将其输出为新的CPK文件，否则将被替换.\n");
                return;
            }

            if (args.Length < 2)
            {
                Console.WriteLine("参数数量不足，无法执行操作，请按照使用方法提供参数。");
                return;
            }

            string cpk_name = args[0];
            string cpkDirectory = Path.GetDirectoryName(cpk_name);

            CPK cpk = new CPK(new Tools());
            cpk.ReadCPK(cpk_name);

            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));

            string extractMe = args[1];
            string unpackFolderName = "";
            List<FileEntry> entries = null;

            string baseName = Path.GetFileNameWithoutExtension(cpk_name);
            baseName = baseName.Replace('.', '_');
            unpackFolderName = Path.Combine(cpkDirectory, baseName + "_unpack");

            if (extractMe.ToUpper() == "ALL")
            {
                Directory.CreateDirectory(unpackFolderName);
                entries = cpk.FileTable.Where(x => x.FileType == "FILE").ToList();
            }
            else
            {
                entries = cpk.FileTable.Where(x => ((x.DirName != null) ? x.DirName + "/" : "") + x.FileName.ToString().ToLower() == extractMe.ToLower()).ToList();
            }

            if (args.Length == 1)
            {
                entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();
                for (int i = 0; i < entries.Count; i++)
                {
                    Console.WriteLine(((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName);
                }
            }
            else if (args.Length == 2)
            {
                if (entries.Count == 0)
                {
                    Console.WriteLine("找不到" + extractMe + ".");
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    string fullPath = ((entries[i].DirName != null) ? Path.Combine(unpackFolderName, (string)entries[i].DirName) : unpackFolderName);
                    if (!String.IsNullOrEmpty((string)entries[i].DirName))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                    string isComp = Encoding.ASCII.GetString(oldFile.ReadBytes(8));
                    oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                    byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                    if (isComp == "CRILAYLA")
                    {
                        int size = Int32.Parse((entries[i].ExtractSize ?? entries[i].FileSize).ToString());
                        chunk = cpk.DecompressCRILAYLA(chunk, size);
                    }

                    Console.WriteLine("正在提取: " + ((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName.ToString());
                    File.WriteAllBytes(Path.Combine(fullPath, entries[i].FileName.ToString()), chunk);
                }
            }
            else
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("插入的用法CriPakTools IN_CPK REPLACE_THIS REPLACE_WITH [OUT_CPK]");
                    return;
                }

                string ins_name = args[1];
                string replace_with = args[2];

                FileInfo fi = new FileInfo(cpk_name);

                string outputName = fi.FullName + ".tmp";
                if (args.Length >= 4)
                {
                    bool isFullyQualified = args[3].IndexOf(':') > 0 || args[3].StartsWith(@"\\");
                    if (!isFullyQualified)
                    {
                        outputName = Path.Combine(cpkDirectory, args[3]);
                    }
                    else
                    {
                        outputName = args[3];
                    }
                }

                BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

                entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].FileType != "CONTENT")
                    {
                        if (entries[i].FileType == "FILE")
                        {
                            if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                            {
                                ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                                for (ulong z = 0; z < padLength; z++)
                                {
                                    newCPK.Write((byte)0);
                                }
                            }
                        }

                        if (entries[i].FileName.ToString() != ins_name)
                        {
                            if (i < entries.Count)
                            {
                                oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                                entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                                cpk.UpdateFileEntry(entries[i]);

                                byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                                newCPK.Write(chunk);
                            }
                        }
                        else
                        {
                            byte[] newbie = File.ReadAllBytes(replace_with);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(newbie);
                        }

                        if ((newCPK.BaseStream.Position % 0x800) > 0)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }
                    else
                    {
                        cpk.UpdateFileEntry(entries[i]);
                    }
                }

                cpk.WriteCPK(newCPK);
                cpk.WriteITOC(newCPK);
                cpk.WriteTOC(newCPK);
                cpk.WriteETOC(newCPK);
                cpk.WriteGTOC(newCPK);

                newCPK.Close();
                oldFile.Close();

                if (args.Length < 4)
                {
                    File.Delete(cpk_name);
                    File.Move(outputName, cpk_name);
                    File.Delete(outputName);
                }
            }
        }
    }
}
