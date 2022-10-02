﻿using System;
using System.IO;
using dnlib.DotNet;
using LoGiC.NET.Protections;
using SharpConfigParser;
using LoGiC.NET.Utils;

namespace LoGiC.NET
{
    class Program
    {
        public static ModuleDefMD Module { get; set; }

        public static string FileExtension { get; set; }

        public static bool DontRename { get; set; }

        public static bool ForceWinForms { get; set; }

        public static string FilePath { get; set; }

        public static MemoryStream Stream = new MemoryStream();

        static void Main(string[] args)
        {
            if (args.Length < 2) {
                Console.WriteLine("Usage: Logic.NET <input-file> <output-file>");
                return;
            }
            string path = args[0];

            Console.WriteLine("Preparing obfuscation...");
            if (!File.Exists("config.txt"))
            {
                Console.WriteLine("Config file not found, continuing without it.");
                goto obfuscation;
            }
            Parser p = new Parser() { ConfigFile = "config.txt" };
            ForceWinForms = bool.Parse(p.Read("ForceWinFormsCompatibility").ReadResponse().ReplaceSpaces());
            DontRename = bool.Parse(p.Read("DontRename").ReadResponse().ReplaceSpaces());
            Randomizer.Initialize();

            obfuscation:
            Module = ModuleDefMD.Load(path);
            FileExtension = Path.GetExtension(path);

            Console.WriteLine("Renaming...");
            Renamer.Execute();

            Console.WriteLine("Adding proxy calls...");
            ProxyAdder.Execute();

            try
            {
                Console.WriteLine("Encrypting strings...");
                StringEncryption.Execute();
            }
            catch { }

            try
            {
                Console.WriteLine("Injecting Anti-Tamper...");
                AntiTamper.Execute();
            }
            catch { }

            try
            {
                Console.WriteLine("Adding junk methods...");
                JunkMethods.Execute();
            }
            catch { }
            try
            {
                Console.WriteLine("Executing Anti-De4dot...");
                AntiDe4dot.Execute();
            }
            catch { }

            try
            {
                Console.WriteLine("Executing Control Flow...");
                ControlFlow.Execute();
            }
            catch { }
            try
            {

                Console.WriteLine("Encoding ints...");
                IntEncoding.Execute();
            }
            catch { }
            try
            {

                Console.WriteLine("Adding invalid metadata...");
                InvalidMetadata.Execute();
            }
            catch { }
            try
            {
                Console.WriteLine("Watermarking...");
                Watermark.AddAttribute();
            }
            catch { }

            FilePath = args[1];
            Console.WriteLine("Saving file as {0}", FilePath);

            Module.Write(Stream, new dnlib.DotNet.Writer.ModuleWriterOptions(Module) { Logger = DummyLogger.NoThrowInstance });

            StripDOSHeader.Execute();

            // Save stream to file
            File.WriteAllBytes(FilePath, Stream.ToArray());

            if (AntiTamper.Tampered)
                AntiTamper.Inject(FilePath);

            Console.WriteLine("Done!");
        }
    }
}
