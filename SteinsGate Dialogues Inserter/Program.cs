﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steins_Gate_Text_Inserter
{
    class Program
    {
        public struct Line
        {
            public UInt16 Magic;
            public string[] param;
        }

        static Line[] Source = new Line[0];
        static List<int> index = new List<int>();
        static List<uint> offsets = new List<uint>();
        static List<int> newOffsets = new List<int>();
        static List<int> positions = new List<int>();

        static List<int> lengths = new List<int>();
        static List<int> newLengths = new List<int>();

        static Dictionary<int, string> myText = new Dictionary<int, string>();

        static int differenceOffsets = 0, chapterLength = 0, newChapterLength = 0;

        static void Main(string[] args)
        {
            Console.WriteLine(
                @"
                      
                      ###################################
                      #      NSB Dialogues Inserter     #
                      ###################################
                      #         Made by Daviex          #
                      ###################################
                      #   Italian Steins;Gate VN Team   #
                      ###################################
                      #           Version 1.5           #
                      ###################################
                      #            Codename:            #
                      ###################################
                      #         El Psy Congroo          #
                      ###################################
                      
                           Press any key to start...    
                                                         ");
            Console.ReadKey();

            if (args.Length == 0)
            {
                Console.WriteLine("You should move the file .txt on me to works!");
                Console.WriteLine("Press a button to close the program.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            args[0] = args[0].Substring(args[0].LastIndexOf('\\') + 1);

            string originalFile = args[0].Replace("txt", "nsb");
            string textFile = args[0];

            BinaryReader nsbFile = new BinaryReader(File.OpenRead("nss\\" + originalFile));

            Console.WriteLine("After analyzed your NSB Files, I will start!");

            string mapName = originalFile.Substring(0, originalFile.Length - 3) + "map";
            BinaryReader mapFile = new BinaryReader(File.OpenRead("nss\\" + mapName));

            if (originalFile.Contains("sg") || originalFile.Contains("function"))
            {
                Analyzer(nsbFile, mapFile);

                Console.WriteLine();
                Console.WriteLine("Let's bring back Mayuri!");

                ImportText(textFile);

                if (!Directory.Exists("importedText\\nss"))
                    Directory.CreateDirectory("importedText\\nss");

                Console.WriteLine();
                Console.WriteLine("I own time with my Reading Steiner!");

                string newMapName = originalFile.Substring(0, originalFile.Length - 3) + "map";
                BinaryWriter newMapFile = new BinaryWriter(File.Create("importedText\\nss\\" + newMapName));
                BinaryWriter newNsbFile = new BinaryWriter(File.Create("importedText\\nss\\" + originalFile));

                NewFile(nsbFile, mapFile, newNsbFile, newMapFile);

                Console.WriteLine();
                Console.Beep(3500, 500);
                Console.WriteLine("Mayuri is back, and now we are on the world line of Steins;Gate!");
                Console.WriteLine("Press any key to finish...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("I don't know how to work with this file... " +
                                  "Please, try again.");
                Console.WriteLine("Press any key to finish...");
                Console.ReadKey();
            }
        }

        static void calculateDifference(uint oldValue, uint newValue)
        {
            if (oldValue > newValue)
            {
                differenceOffsets -= (int)(oldValue - newValue);
            }
            else if (oldValue < newValue)
            {
                differenceOffsets += (int)(newValue - oldValue);
            }
        }

        static void Analyzer(BinaryReader nsbFile, BinaryReader mapFile, bool isExtraTips = false)
        {
            uint Entry, Length;
            ushort numParam = 0;
            Line currLine;
            int point = 0;
            byte[] buffer;
            string Text = String.Empty;
            bool hasChapter = false;

            while (point < nsbFile.BaseStream.Length)
            {
                Entry = nsbFile.ReadUInt32();
                Entry -= 1;
                Array.Resize(ref Source, Source.Length + 1);
                currLine = Source[Entry];
                currLine.Magic = nsbFile.ReadUInt16();
                numParam = nsbFile.ReadUInt16();
                currLine.param = new string[numParam];

                for (int i = 0; i < numParam; i++)
                {
                    Length = nsbFile.ReadUInt32();
                    buffer = nsbFile.ReadBytes((int)Length);
                    Text = Encoding.Unicode.GetString(buffer);
                    if (Text == "$CHAPTER_NOW")
                        hasChapter = true;
                    else if (hasChapter && Text != "STRING")
                    {
                        index.Add((int)Entry + 1);
                        chapterLength = (int)Length;
                        hasChapter = false;
                    }
                    if (Text.Contains("<PRE"))
                    {
                        if (Text.Length > 15)
                        {
                            index.Add((int) Entry + 1);
                            lengths.Add((int) Length);
                            positions.Add((int) nsbFile.BaseStream.Position);
                        }
                    }

                    currLine.param[i] = Text;
                }

                point = (int)nsbFile.BaseStream.Position;

                Source[Entry] = currLine;
            }

            uint offset;
            ushort size;
            string Label;
            int pointer = 0;

            while (pointer < mapFile.BaseStream.Length)
            {
                offset = mapFile.ReadUInt32();
                offsets.Add(offset);
                size = mapFile.ReadUInt16();
                buffer = mapFile.ReadBytes(size);
                Label = Encoding.Unicode.GetString(buffer);

                pointer = (int)mapFile.BaseStream.Position;
            }
        }

        static void ImportText(string path)
        {
            int count = 0;
            bool firstPass = false, firstString = false;
            string line = String.Empty;

            foreach (string lines in File.ReadAllLines(path))
            {
                if (!lines.Contains(@"</PRE>"))
                {
                    if (lines.Contains("{textblock"))
                        continue;
                    else if (lines.Contains("<ChapterName"))
                    {
                        line += lines;

                        myText.Add(index[count], line);
                        line = String.Empty;
                        count++;
                    }
                    else if (line.Contains("//"))
                        continue;
                    else if (lines == "" && (!firstPass || firstString))
                    {
                        firstPass = true;
                        if (firstString)
                        {
                            line += lines + '\n';
                            firstString = false;
                        }
                    }
                    else
                        line += lines + '\n';

                    if (lines.Contains("text00010"))
                        firstString = true;
                }
                else
                {
                    line += lines;
                    myText.Add(index[count], line);
                    newLengths.Add(line.Length * 2);
                    line = String.Empty;
                    firstPass = false;
                    count++;
                }
            }
        }

        static void NewFile(BinaryReader oldFile, BinaryReader mapFile, BinaryWriter newFile, BinaryWriter newMapFile)
        {
            oldFile.BaseStream.Position = 0;

            uint Entry, Length;
            ushort numParam = 0, magic;
            int point = 0, currentPos = 0, newLength = 0;
            string Text = String.Empty;
            string result;
            bool hasChapterName = false;
            long chapterPosition = 0;

            while (point < oldFile.BaseStream.Length)
            {
                Entry = oldFile.ReadUInt32();
                newFile.Write(Entry);

                magic = oldFile.ReadUInt16();
                newFile.Write(magic);

                numParam = oldFile.ReadUInt16();
                newFile.Write(numParam);

                for (int i = 0; i < numParam; i++)
                {
                    Length = oldFile.ReadUInt32();
                    if (myText.ContainsKey((int)Entry) && i == 2)
                    {
                        oldFile.ReadBytes((int)Length);

                        newFile.Write(myText[(int)Entry].Length * 2);
                        newFile.Write(Encoding.Unicode.GetBytes(myText[(int)Entry]));
                    }
                    else if (myText.ContainsKey((int)Entry) && myText.TryGetValue((int)Entry, out result) && result.Contains("<ChapterName>"))
                    {
                        string tempText = Encoding.Unicode.GetString(oldFile.ReadBytes((int)Length));

                        if (tempText != "STRING")
                        {
                            string noOpenTag = result.Remove(0, "<ChapterName>".Length);
                            string noEndTag = noOpenTag.Remove(noOpenTag.IndexOf("</ChapterName>"),
                                "</ChapterName>".Length);

                            hasChapterName = true;
                            chapterPosition = newFile.BaseStream.Position;

                            newChapterLength = noEndTag.Length*2;

                            newFile.Write(noEndTag.Length*2);
                            newFile.Write(Encoding.Unicode.GetBytes(noEndTag));
                        }
                        else
                        {
                            newFile.Write(tempText.Length * 2);
                            newFile.Write(Encoding.Unicode.GetBytes(tempText));
                        }
                    }
                    else
                    {
                        newFile.Write(Length);
                        newFile.Write(oldFile.ReadBytes((int)Length));
                    }
                }

                point = (int)oldFile.BaseStream.Position;
            }

            int newOffset = 0, newLengthPlus = 0, newLengthMinus = 0;

            if (hasChapterName)
                calculateDifference((uint) chapterLength, (uint) newChapterLength);

            foreach (uint offset in offsets)
            {
                for (int i = 0; i < positions.Count && offset > positions[i]; i++)
                {
                    if (lengths[i] != newLengths[i])
                    {
                        if (lengths[i] > newLengths[i])
                        {
                            newLengthPlus += lengths[i] - newLengths[i];
                        }
                        else if (lengths[i] < newLengths[i])
                        {
                            newLengthMinus += newLengths[i] - lengths[i];
                        }
                    }
                }
                
                newOffset = (int)(offset - newLengthPlus + newLengthMinus);

                if (hasChapterName)
                    if (offset > chapterPosition)
                        newOffset += differenceOffsets;

                newOffsets.Add(newOffset);

                newLengthPlus = 0;
                newLengthMinus = 0;
            }

            uint offSet;
            ushort size;
            string Label;
            byte[] buffer;
            int pointer = 0, count = 0;

            mapFile.BaseStream.Position = 0;

            while (pointer < mapFile.BaseStream.Length)
            {
                offSet = mapFile.ReadUInt32(); //Unused
                newMapFile.Write(Convert.ToUInt32(newOffsets[count]));
                size = mapFile.ReadUInt16();
                newMapFile.Write(size);
                buffer = mapFile.ReadBytes(size);
                newMapFile.Write(buffer);

                pointer = (int)mapFile.BaseStream.Position;

                count++;
            }

            newMapFile.Flush();
            newMapFile.Close();

            newFile.Flush();
            newFile.Close();
        }
    }
}
