﻿/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.IO;

namespace HeronEngine
{
    public static class Program
    {
        public static DateTime timeStarted;

        /// <summary>
        /// Prints ta usage message to the console
        /// </summary>
        static public void Usage()
        {
            Console.WriteLine("HeronEngine.exe");
            Console.WriteLine("    by Christopher Diggins");
            Console.WriteLine("    version 0.9.1");
            Console.WriteLine("    February 15th, 2010");
            Console.WriteLine("");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  HeronEngine.exe inputfile.heron ");
            Console.WriteLine("");
            Console.WriteLine("The configuration file 'config.xml' is loaded if it is found");
            Console.WriteLine("in the same directory as the Heron executable.");
            Console.WriteLine("");
        }

        /// <summary>
        /// Write the grammar in human readable form to the file "grammar.txt" in the executable directory
        /// </summary>
        static public void OutputGrammar()
        {
            string s = Grammar.ToString(typeof(HeronGrammar));
            File.WriteAllText(Util.GetExeDir() + "\\grammar.txt", s);
        }

        /// <summary>
        /// Outputs all of the primitive types with their fields and methods.
        /// </summary>
        static public void OutputPrimitives()
        {
            string s = PrimitiveTypes.NonCodeModelTypesAsString();
            File.WriteAllText(Util.GetExeDir() + "\\primitives.txt", s);
            s = PrimitiveTypes.CodeModelTypesAsString();
            File.WriteAllText(Util.GetExeDir() + "\\codemodel.txt", s);
        }

        /// <summary>
        /// Loads, parses, and executes ta file
        /// </summary>
        /// <param name="s"></param>
        static public void RunFile(string file)
        {
            VM vm = new VM();
            vm.InitializeVM();
            vm.EvalFile(file);
        }
        
        /// <summary>
        /// Load the config file if specified in the command-line argument list
        /// </summary>
        /// <param name="funcs"></param>
        static public void LoadConfig()
        {
            string configFile = Util.GetExeDir() + "\\config.xml";
            if (File.Exists(configFile))
                Config.LoadFromFile(configFile);
        }

        /// <summary>
        /// Ouputs the time elapsed since the beginning of the program.
        /// </summary>
        static public void PrintTimeElapsed()
        {
            TimeSpan ts = DateTime.Now - timeStarted;
            Console.WriteLine(ts.TotalMilliseconds + " ms");
        }

        /// <summary>
        /// Ouputs the time elapsed since the beginning of the program.
        /// </summary>
        static public void PrintTimeElapsed(string s)
        {
            Console.Write(s + " : ");
            PrintTimeElapsed();
        }

        /// <summary>
        /// Entry point for the application. 
        /// </summary>
        /// <param name="funcs"></param>
        static public void Main(string[] args)
        {
            try
            {
                LoadConfig();
                if (Config.outputGrammar)
                    OutputGrammar();
                if (Config.outputPrimitives)
                    OutputPrimitives();
                if (Config.runUnitTests)
                    HeronTests.MainTest();
                
                timeStarted = DateTime.Now;
                if (args.Length != 1)
                {
                    Usage();
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                RunFile(args[0]);

                if (Config.showTiming)
                    PrintTimeElapsed();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured: " + e.Message);
            }

            Console.WriteLine();
            if (Config.waitForKeypress)
            {
                Console.Write("Press any key to continue ...");
                Console.ReadKey();
            }
        }
    }
}
