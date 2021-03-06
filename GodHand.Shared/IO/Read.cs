﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using GodHand.Shared.Models;
using GodHand.Shared.Models.TreeView;

namespace GodHand.Shared.IO
{
    public class Read
    {
        public static ObservableCollection<ByteInformation> File(string path, long start, long length, string encoder, string patchPath)
        {
            ObservableCollection<Patch> patchCollection = PatchFile(patchPath);

            using (Stream fStr = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Dictionary<string, string> dict = (encoder == "Default") ? null : Read.EncodingTable(encoder);
                BinaryReader bReader = new BinaryReader(fStr, Encoding.GetEncoding(932));
                ObservableCollection<ByteInformation> lByteInformation = new ObservableCollection<ByteInformation>();

                byte[] bytes;

                if (start != -1) bReader.BaseStream.Seek(start, SeekOrigin.Begin);
                if (length == -1) bytes = bReader.ReadBytes(System.Convert.ToInt32(bReader.BaseStream.Length));
                else bytes = bReader.ReadBytes(System.Convert.ToInt32(length));

                List<byte> tempByteList = new List<byte>();
                int i = -1;

                for (int j = 0; j < bytes.Length; j++)
                {
                    if (bytes[j] == 0 && tempByteList.Count == 0) continue;
                    if (int.TryParse(bytes[j].ToString(), out i) == false) continue;
                    if (bytes[j] == 0 && tempByteList.Count > 0)
                    {
                        var currentValue = (encoder == "Default")
                            ? Encoding.GetEncoding(932).GetString(tempByteList.ToArray())
                            : Convert.ByteValueToCustomEncoding(tempByteList.ToArray(), dict);

                        if (currentValue.Contains("\\n")) currentValue = currentValue.Replace("\\n", Environment.NewLine);
                        if (currentValue.Contains("\\e")) currentValue = currentValue.Replace("\\e", ((Char)3).ToString());

                        var startPosition = j - tempByteList.Count;
                        string patchValue = null;
                        if (patchCollection.Count > 0)
                        {
                            var result = patchCollection.FirstOrDefault(x => x.StartPosition == startPosition);
                            if (result != null) patchValue = Convert.HexToString(result.Value);
                        }
                        ByteInformation bi = new ByteInformation(tempByteList.ToArray(), startPosition,
                            currentValue, patchValue);

                        lByteInformation.Add(bi);
                        tempByteList.Clear();
                    }
                    else tempByteList.Add(bytes[j]);
                }
                
                return lByteInformation;
            }
        }

        public static ObservableCollection<Patch> PatchFile(string path)
        {
            ObservableCollection<Patch> collection = new ObservableCollection<Patch>();
            if (path == null) return collection;
            if (!System.IO.File.Exists(path)) return collection;

            string value = null;
            using (Stream fStr = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BinaryReader bReader = new BinaryReader(fStr, Encoding.ASCII);

                byte[] bytes = bReader.ReadBytes(System.Convert.ToInt32(bReader.BaseStream.Length));
                value = Encoding.ASCII.GetString(bytes);
            }

            if (value != null)
            {
                var splittedValue = value.Split(new[] {"\r\n"}, StringSplitOptions.None);
                foreach (var item in splittedValue)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    string[] array = item.Split(',');
                    Patch p = new Patch()
                    {
                        ByteLength = System.Convert.ToInt32(array[0]),
                        StartPosition = System.Convert.ToInt32(array[1]),
                        Value = array[2]
                    };
                    collection.Add(p);
                }
            }

            return collection;
        }

        public static T Xml<T>(string path) where T : new()
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            var returnItem = new T();

            XmlSerializer xml = null;

            using (var rd = new XmlTextReader(fs))
            {
                xml = new XmlSerializer(typeof(T));
                returnItem = (T) xml.Deserialize(rd);
            }

            return returnItem;
        }

        public static string Picture()
        {
            string c2tPath = (Environment.Is64BitProcess)
                ? @"\lib\Capture2Text\x64\Capture2Text_CLI.exe"
                : @"\lib\Capture2Text\x86\Capture2Text_CLI.exe";
            string ocrPath = Environment.CurrentDirectory + @"\temp\ocr.txt";
            string result = string.Empty;

            try
            {
                Process proc = new Process();
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;

                processStartInfo.FileName = Environment.CurrentDirectory + c2tPath;
                processStartInfo.Arguments =
                    $@" -l ""Japanese"" -i ""{Environment.CurrentDirectory + @"\temp\temp.jpg"}"" -o ""{ocrPath}""";
                proc.StartInfo = processStartInfo;
                proc.Start();
                proc.WaitForExit();

                result = System.IO.File.ReadAllText(ocrPath, Encoding.UTF8);

            }

            catch (Exception e)
            {
                MessageBox.Show("There has been an error while reading picture: " + e);
            }

            return result;
        }

        public static Dictionary<string, string> EncodingTable(string filename)
        {
            string directory = Environment.CurrentDirectory + @"\encoding\";
            string path = directory + filename;

            Dictionary<string, string> encodingTable = new Dictionary<string, string>();

            var lines = System.IO.File.ReadAllLines(path);

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;

                var lineArray = line.Split(';');
                encodingTable.Add(lineArray[0], lineArray[1]);
            }

            return encodingTable;
        }

        public static List<TreeViewItem> DirectoriesOfPath(string path)
        {
            var items = new List<TreeViewItem>();

            var dirInfo = new DirectoryInfo(path);

            foreach (var directory in dirInfo.GetDirectories())
            {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Fullpath = directory.FullName,
                    Items = DirectoriesOfPath(directory.FullName)
                };

                items.Add(item);
            }

            return items;
        }

        public static List<TreeViewItem> FilesOfDirectory(string path)
        {
            var items = new List<TreeViewItem>();

            var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                FileInfo fi = new FileInfo(file);
                FileItem fItem = new FileItem() {Name = fi.Name, Fullpath = fi.FullName};
                items.Add(fItem);
            }

            return items;
        }
    }

}
