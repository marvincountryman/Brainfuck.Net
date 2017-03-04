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
using System.Linq;
using System.Text;

namespace Brainfuck.Interpret {
    public class Lexer {
        public Script   Script;
        public Token    Current;
        public Position Position;
        public readonly char[] Symbols = { 
            '<', '>', '+', '-', ',', '.', '[', ']', '\0'
        };

        public Lexer (Script script) {
            Script = script;
            Reset ();
        }

        public char next () {
            if (Position.Offset >= Script.Code.Length)
                return '\0';

            char ch = Script.Code[Position.Offset];

            Position.Offset++;
            Position.Column++;

            if (ch == '\n') {
                Position.Line++;
                Position.Column = 0;
            }

            return ch;
        }

        public Token Next () {
            char ch;

            for (;;) {
                ch = next ();

                if (Symbols.Contains (ch)) {
                    Current = new Token () {
                        Type = ch,
                        Position = Position
                    };
                    return Current;
                }
            }
        }
        public void Reset () {
            Position = new Position () {
                Line   = 1,
                Column = 0,
                Offset = 0,
            };
        }
    }
}
