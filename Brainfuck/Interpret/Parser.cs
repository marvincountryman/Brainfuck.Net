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
using System.Collections.Generic;

using Brainfuck.Interpret.Basm;

namespace Brainfuck.Interpret {
    public class Parser {
        private int offset;
        private int pointer;
        private List<Instruction> instructions;

        public Script Script;

        public Parser (Script script) {
            offset = -1;
            pointer = 0;
            instructions = new List<Instruction> ();

            Script = script;
        }

        public Token next () {
            Token token = Script.Lexer.Next ();

            if (token.Type != '\0')
                offset++;

            return token;
        }

        public Instruction addInstruction (OpCode opcode, Token token) {
            Instruction instruction = new Instruction () {
                Token = token,
                Offset = offset,
                OpCode = opcode,
            };

            instructions.Add (instruction);
            return instruction;
        }
        public Instruction addInstruction (OpCode opcode, Token token, object operand) {
            Instruction instruction = new Instruction () {
                Token = token,
                Offset = offset,
                OpCode = opcode,
                Operand = operand,
            };

            instructions.Add (instruction);
            return instruction;
        }

        public void parseBlock (bool inLoop = false, int brfalseOffset = 0) {
            bool exitLoop = false;
            Instruction brfalse = default(Instruction);
            Token brfalseToken = default(Token);

            if (inLoop) {
                brfalse      = instructions[brfalseOffset];
                brfalseToken = brfalse.Token;
            }

            for (;;) {
                Token token = next ();

                switch (token.Type) {
                    case '>': 
                        pointer++; 
                        addInstruction (OpCode.IncrementPointer, token); 
                        break;
                    case '<': 
                        pointer--; 
                        addInstruction (OpCode.DecrementPointer, token);

                        if (pointer < 0) {
                            throw new ParseException ($"Potential StackUnderflow at line {token.Position.Line}, column {token.Position.Column}");
                        }

                        break;
                    case '+': addInstruction (OpCode.IncrementData, token); break;
                    case '-': addInstruction (OpCode.DecrementData, token); break;
                    case ',': addInstruction (OpCode.Get, token); break;
                    case '.': addInstruction (OpCode.Put, token); break;
                    case '[':
                        addInstruction (OpCode.Brfalse, token);
                        parseBlock (true, offset); 
                        break;
                    case ']':
                        if (inLoop) {
                            brfalse.Operand = addInstruction (OpCode.Brtrue, token, brfalse);
                            exitLoop = true;

                            return;
                        }

                        throw new ParseException ($"Unexpected ] at line {token.Position.Line}, column {token.Position.Column}");
                    case '\0':
                        if (inLoop & !exitLoop) {
                            throw new ParseException ($"Unfinished [ near line {brfalseToken.Position.Line}, column {brfalse.Token.Position.Column}");
                        }

                        return;
                }
            }
        }

        public Instruction[] Parse () {
            parseBlock ();
            return instructions.ToArray ();
        }
    }
}
