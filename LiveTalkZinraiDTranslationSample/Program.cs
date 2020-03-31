/*
 * Copyright 2019 FUJITSU SOCIAL SCIENCE LABORATORY LIMITED
 * システム名：LiveTalkZinraiDTranslationSample
 * 概要      ：Zinrai Translation Service 連携サンプルアプリ
*/
////using NAudio.Wave;
using LiveTalk;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LiveTalkZinraiDTranslationSample
{
    class Program
    {
        static FileCollaboration FileInterface;
        static CancellationTokenSource TokenSource = new CancellationTokenSource();
        const string IDTag = " ";

        static void Main(string[] args)
        {
            var model = new Models.ZinraiTranslatorModel();
            var param = new string[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LiveTalkOutput.csv"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Zinrai.txt"),
            };
            if (args.Length >= 1)
            {
                param[0] = args[0];
            }
            if (args.Length >= 2)
            {
                param[1] = args[1];
            }
            Console.WriteLine("InputCSVFileName  :" + param[0]);
            Console.WriteLine("OutputTextFileName:" + param[1]);
            FileInterface = new FileCollaboration(param[0], param[1]);

            // ファイル入力(LiveTalk常時ファイル出力からの入力)
            FileInterface.RemoteMessageReceived += async (s) =>
            {
                var reg = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                var items = reg.Split(s);
                var name = "\"" + System.IO.Path.GetFileNameWithoutExtension(param[1]).ToUpper() + "\"";

                Console.WriteLine(">>>>>>>");
                if (items[2].IndexOf(IDTag) == 1 && items[1] == name)
                {
                    // 自メッセージ出力分なので無視
                }
                else
                { 
                    Console.WriteLine("DateTime:" + items[0]);
                    Console.WriteLine("Speaker:" + items[1]);
                    Console.WriteLine("Speech contents:" + items[2]);
                    Console.WriteLine("Translate content:" + items[3]);

                    //Zinrai連携
                    var question = items[2].Substring(1, items[2].Length - 2);
                    var answer = await model.GetTranslation(question,"ja","en");
                    if (!string.IsNullOrEmpty(answer))
                    {
                        var answers = answer.Split('\n');
                        foreach (var item in answers)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                FileInterface.SendMessage(IDTag + item);
                            }
                        }
                    }
                }
            };

            // ファイル監視開始
            if (System.IO.File.Exists(param[0]))
            {
                System.IO.File.Delete(param[0]);
            }
            FileInterface.WatchFileSart();

            // 処理終了待ち
            var message = Console.ReadLine();

            // ファイル監視終了
            TokenSource.Cancel(true);
            FileInterface.WatchFileStop();
        }
    }
}
