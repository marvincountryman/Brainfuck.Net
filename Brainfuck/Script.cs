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

using Brainfuck;
using Brainfuck.Interpret;

namespace Brainfuck {
    public class Script {
        public string Code = String.Empty;
        public string Identifier = "brainfuck";

        public int StackSize = 3000;

        public int Vm;
        public Lexer Lexer;
        public Parser Parser;
        public Compiler Compiler;

        public Script () {
            Lexer = new Lexer (this);
            Parser = new Parser (this);
            Compiler = new Compiler (this);
        }
    }
}
