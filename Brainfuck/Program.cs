//
//  Copyright 2017  Marvin Countryman
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using System.IO;
using System.Linq;

using Mono.Options;

namespace Brainfuck {
    class Program {
        /*
            -of = output file
            -if = input file

            [string] code, if file not read
        */
        private static bool   Help = false;
        private static string Input = String.Empty;
        private static string Output = String.Empty;
        private static Script Script = new Script ();

        private static OptionSet Options = new OptionSet () {
            {"if|input", "Input brainfuck file.  (ifndefined uses last argument as code)",
                v => Input = v
            },
            {"of|output", "Output executable file. (ifndefined runs code automagically)",
                v => Output = v
            },
            {"h|help", "Show help message",
                v => Help = v != null
            },
            {"<>",
                v => Script.Code = v
            }
        };

        private static void Main (string[] args) {
            try {
                Options.Parse (args);
            } catch (OptionException e) {
                Console.WriteLine (e.Message);
                Console.WriteLine ("Try --help for more information.");
                return;
            }

            if (Help) {
                Console.WriteLine ("USAGE: Brainfuck [OPTIONS]+ [OPTIONAL CODE]");
                Options.WriteOptionDescriptions (Console.Out);
                return;
            }

            if (Input != String.Empty) {
                try {
                    Script.Code       = File.ReadAllText (Input);
                    Script.Identifier = Path.GetFileNameWithoutExtension (Input);
                } catch {
                    Console.WriteLine ($"Unable to read file '{Input}'.");
                    return;
                }
            } else {
                Script.Identifier = "stdin";
            }

            if (Script.Code == String.Empty) {
                Console.WriteLine ("Can't run what's not there.  Try --help for more information.");
                return;
            }

            if (Output == String.Empty)
                Script.Compiler.Run ();
            else
                Script.Compiler.Compile (Output);
        }
    }
}
