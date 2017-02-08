using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace t1
{
    class Program
    {
        static IdentifyEncoding sinodetector = new IdentifyEncoding();
        static DirectoryInfo dr = new DirectoryInfo(Assembly.GetEntryAssembly().Location);
        static string path = dr.Parent.FullName;
        static Encoding gb2312 = Encoding.GetEncoding(936);
        static Encoding utf8 = Encoding.GetEncoding(65001);
        //static Encoding big5 = Encoding.GetEncoding(950);
        static string fileSuffix = ".txt";

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" --------------------------------- ");
            Console.WriteLine(" | T1文件编码查询转换器 v1.0     | ");
            Console.WriteLine(" | https://github.com/dengfan/t1 | ");
            Console.WriteLine(" --------------------------------- ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("> 当前所在位置：");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(path);
            Console.WriteLine();
            Console.ResetColor();

            StringBuilder sb = new StringBuilder("  [a]所有 ");
            int i = 0;
            foreach (var item in IdentifyEncoding.Names)
            {
                sb.Append("[" + i + "]" + item + " ");
                i++;
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(sb.ToString());
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("[z]转码");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("> 请输入文件查询功能编号并回车：");
            Console.ForegroundColor = ConsoleColor.Yellow;

            try
            {
                var key = Console.ReadLine();

                if (key.Length != 1 || !"az1234567890".Contains(key.ToLower()))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Error.Write("  无法识别[");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Error.Write(key);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Error.Write("]，请输入正确的功能编号！");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("> 请输入要处理的文件后缀名(如 .xml)并回车：");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    var fileType = Console.ReadLine();
                    if (string.IsNullOrEmpty(fileType) || !fileType.StartsWith(".") || fileType.Split('.').Length != 2)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("  无效的文件扩展名，请重新输入！");
                        Console.ResetColor();
                    }
                    else
                    {
                        fileSuffix = fileType;

                        if (key.ToLower().Equals("z"))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("  [06]GB2312>>UTF-8 [60]UTF-8>>GB2312");
                            //Console.WriteLine("  [36]BIG5>>UTF-8 [63]UTF-8>>BIG5");
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("> 请输入文件转码功能编号并回车：");

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            string key2 = Console.ReadLine();
                            Console.ResetColor();
                            int srcKey = 0;
                            int dstKey = 0;
                            if (key2.Length !=2 || !"06,60".Contains(key2) || !(int.TryParse(key2.Substring(0, 1), out srcKey) && int.TryParse(key2.Substring(1, 1), out dstKey)))
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.Error.Write("  无法识别[");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Error.Write(key2);
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.Error.Write("]，请输入正确的功能编号，必须为2位数字！");
                                Console.ResetColor();
                                Console.WriteLine();
                            }
                            else
                            {
                                int count = ConvertFiles(new DirectoryInfo(path), srcKey, dstKey);

                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine("################## 转码完成，共转码{0}个符合条件的文件。##################", count);
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            int count = ListFiles(new DirectoryInfo(path), key);

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("################## 查询完成，共找到{0}个符合条件的文件。##################", count);
                            Console.ResetColor();
                        }
                    }
                    
                }
            }
            catch (IOException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("出错了: " + e);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(">>请按任意键从头开始 ...");
            Console.ResetColor();
            Console.ReadLine();

            Program.Main(args);
        }

        #region 列出指定编码的文件
        public static int ListFiles(FileSystemInfo fsi, string key)
        {
            int count = 0;

            if (!fsi.Exists) return 0;

            DirectoryInfo dir = fsi as DirectoryInfo;

            if (dir == null) return 0; //不是目录

            FileSystemInfo[] files = dir.GetFileSystemInfos();
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i] as FileInfo;
                if (file != null && fileSuffix.ToLower().Trim().Equals(file.Extension)) //是文件
                {
                    var str = GetEncodingName(file, key);
                    if (!string.IsNullOrEmpty(str))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write("  [{0}] ", str);
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(file.FullName.Replace(path, string.Empty));
                        Console.WriteLine();
                        count++;
                    }
                }
                else //对于子目录，进行递归调用 
                {
                    count += ListFiles(files[i], key);
                }
            }

            return count;
        }

        public static string GetEncodingName(FileInfo f, string key)
        {
            string name = string.Empty;

            try
            {
                sbyte[] rawtext = new sbyte[f.Length];
                using (FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read))
                {
                    IdentifyEncoding.ReadInput(fs, ref rawtext, 0, rawtext.Length);
                }

                name = GetEncodingName(rawtext, key);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("分析文件出错(" + f.FullName + ")");
                Console.Error.WriteLine(e);
                Console.ResetColor();
            }

            return name;
        }

        public static string GetEncodingName(sbyte[] rawtext, string key)
        {
            int[] scores;
            int index, maxscore = 0;
            int encoding_guess = 0;

            scores = new int[10];
            //分析编码的概率
            scores[0] = sinodetector.GB2312Probability(rawtext);
            scores[1] = sinodetector.GBKProbability(rawtext);
            scores[2] = sinodetector.HZProbability(rawtext);
            scores[3] = sinodetector.BIG5Probability(rawtext);
            scores[4] = sinodetector.ENCTWProbability(rawtext);
            scores[5] = sinodetector.ISO2022CNProbability(rawtext);
            scores[6] = sinodetector.UTF8Probability(rawtext);
            scores[7] = sinodetector.UnicodeProbability(rawtext);
            scores[8] = sinodetector.ASCIIProbability(rawtext);
            scores[9] = 0;

            // Tabulate Scores
            for (index = 0; index < 10; index++)
            {
                if (scores[index] > maxscore)
                {
                    encoding_guess = index;
                    maxscore = scores[index];
                }
            }

            // Return OTHER if nothing scored above 50
            if (maxscore <= 50)
            {
                encoding_guess = 9;
            }

            if (encoding_guess.ToString().Equals(key))
            {
                return IdentifyEncoding.Names[encoding_guess];
            }

            if (key.ToLower().Equals("a"))
            {
                return IdentifyEncoding.Names[encoding_guess];
            }

            return string.Empty;
        }
        #endregion


        #region 文件转码
        static int ConvertFiles(FileSystemInfo fsi, int srcKey, int dstKey)
        {
            int count = 0;

            if (!fsi.Exists) return 0;

            DirectoryInfo dir = fsi as DirectoryInfo;

            if (dir == null) return 0; //不是目录

            FileSystemInfo[] files = dir.GetFileSystemInfos();
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i] as FileInfo;
                if (file != null && fileSuffix.ToLower().Trim().Equals(file.Extension)) //是文件
                {
                    string fileFullName = file.FullName;
                    string encodingName = GetEncodingName(file, srcKey.ToString());
                    string convertInfo = string.Format("[{0}>>{1}]", encodingName, IdentifyEncoding.Names[dstKey]);
                    if (encodingName.ToUpper().Equals("GB2312") && srcKey == 0)
                    {
                        if (dstKey == 6 && Convert(fileFullName, convertInfo, gb2312, utf8)) count++; // GB2312>>UTF-8
                    }
                    else if (encodingName.ToUpper().Equals("UTF-8") && srcKey == 6)
                    {
                        if (dstKey == 0 && Convert(fileFullName, convertInfo, utf8, gb2312)) count++; // UTF-8>>GB2312
                    }
                }
                else //对于子目录，进行递归调用
                {
                    count += ConvertFiles(files[i], srcKey, dstKey);
                }
            }

            return count;
        }

        static bool Convert(string fileFullName, string convertInfo, Encoding srcEncoding, Encoding dstEncoding)
        {
            try
            {
                if (File.Exists(fileFullName))
                {
                    string txt = string.Empty;
                    using (StreamReader sr = new StreamReader(fileFullName, srcEncoding))
                    {
                        txt = sr.ReadToEnd();
                        sr.Close();
                    }

                    if (File.Exists(fileFullName))
                    {
                        string newFullFileName = string.Format("{0}.bak", fileFullName);
                        if (File.Exists(newFullFileName))
                        {
                            File.Delete(newFullFileName);
                        }
                        File.Move(fileFullName, newFullFileName);
                    }

                    using (StreamWriter sw = new StreamWriter(fileFullName, false, dstEncoding))
                    {
                        sw.Write(txt);
                        sw.Close();
                    }

                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("  {0} ", convertInfo);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(fileFullName.Replace(path, string.Empty));
                    Console.WriteLine();

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("出错了: {0}", e.ToString());
            }

            return false;
        }
        #endregion

    }
}
