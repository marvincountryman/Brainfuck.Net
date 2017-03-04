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
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Brainfuck.Interpret {
    public class Compiler {
        public Script Script;

        public Compiler (Script script) {
            Script = script;
        }

        public void Run () {
            string filename = Script.Identifier + ".exe";

            CompileInMemory (filename).Save (filename);
            Process process;

            process = Process.Start (filename);
            process.WaitForExit ();

            File.Delete (filename);
        }

        public void Compile () {
            Compile (Script.Identifier + ".exe");
        }
        public void Compile (string outputFilename) {
            CompileInMemory (outputFilename).Save (outputFilename);
        }
        public AssemblyBuilder CompileInMemory (string outputFilename) {
            AssemblyName name = new AssemblyName (Path.GetFileNameWithoutExtension (outputFilename));
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly (name, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = assembly.DefineDynamicModule (name.Name, outputFilename);

            TypeBuilder program = module.DefineType (name.Name + ".Program", TypeAttributes.Public);
            MethodBuilder main = program.DefineMethod("Main", MethodAttributes.Static, typeof(void), new Type[] { typeof(string[]) });

            main.InitLocals = true;
            ILGenerator il = main.GetILGenerator ();

            // Initialization
            LocalBuilder pointer = il.DeclareLocal (typeof (int));
            LocalBuilder registers = il.DeclareLocal (typeof (byte[]));
            LocalBuilder consoleKeyInfo = il.DeclareLocal (typeof (ConsoleKeyInfo));

            pointer.SetLocalSymInfo ("pointer");
            registers.SetLocalSymInfo ("registers");

            il.Emit (OpCodes.Ldc_I4_0);
            il.Emit (OpCodes.Stloc, pointer);

            il.Emit (OpCodes.Ldc_I4, Script.StackSize);
            il.Emit (OpCodes.Newarr, typeof (byte));
            il.Emit (OpCodes.Stloc, registers);

            bool hasOutput = false;
            Dictionary<Basm.Instruction, Label> instructionLabelMap = new Dictionary<Basm.Instruction, Label> ();

            foreach (var instruction in Script.Parser.Parse ()) {
                switch (instruction.OpCode) {
                    case Basm.OpCode.IncrementPointer:
                    case Basm.OpCode.DecrementPointer:
                        il.Emit (OpCodes.Ldloc, pointer);
                        il.Emit (OpCodes.Ldc_I4_1);

                        if (instruction.OpCode == Basm.OpCode.IncrementPointer)
                            il.Emit (OpCodes.Add);
                        else
                            il.Emit (OpCodes.Sub);
                        
                        il.Emit (OpCodes.Stloc, pointer);

                        break; 
                    case Basm.OpCode.DecrementData:
                    case Basm.OpCode.IncrementData:
                        il.Emit (OpCodes.Ldloc, registers);
                        il.Emit (OpCodes.Ldloc, pointer);
                        il.Emit (OpCodes.Ldelema, typeof (byte));
                        il.Emit (OpCodes.Dup);
                        il.Emit (OpCodes.Ldind_U1);
                        il.Emit (OpCodes.Ldc_I4_1);

                        if (instruction.OpCode == Basm.OpCode.IncrementData)
                            il.Emit (OpCodes.Add);
                        else
                            il.Emit (OpCodes.Sub);

                        il.Emit (OpCodes.Conv_U1);
                        il.Emit (OpCodes.Stind_I1);

                        break;
                     case Basm.OpCode.Put:
                        hasOutput = true;

                        il.Emit (OpCodes.Ldloc, registers);
                        il.Emit (OpCodes.Ldloc, pointer);
                        il.Emit (OpCodes.Ldelem_U1);
                        il.EmitCall (OpCodes.Call, typeof (Convert).GetMethod ("ToChar", new Type[] { typeof(byte) }), new Type[] { typeof(char) });
                        il.EmitCall (OpCodes.Call, typeof (Console).GetMethod ("Write", new Type[] { typeof(char) }), new Type[] { typeof (char) });

                        break;
                     case Basm.OpCode.Get:
                        il.Emit (OpCodes.Ldloc, registers);
                        il.Emit (OpCodes.Ldloc, pointer);
                        il.EmitCall (OpCodes.Call, typeof (Console).GetMethod ("ReadKey", Type.EmptyTypes), Type.EmptyTypes);
                        il.Emit (OpCodes.Stloc, consoleKeyInfo);
                        il.Emit (OpCodes.Ldloca_S, consoleKeyInfo);
                        il.EmitCall (OpCodes.Call, typeof (ConsoleKeyInfo).GetProperty ("KeyChar").GetGetMethod (), Type.EmptyTypes);
                        il.Emit (OpCodes.Conv_U1);
                        il.Emit (OpCodes.Stelem_I1);

                        break;
                    case Basm.OpCode.Brfalse: {
                            Label labelFrom = instructionLabelMap[instruction] = il.DefineLabel ();
                            Label labelTo = instructionLabelMap[(Basm.Instruction)instruction.Operand] = il.DefineLabel ();

                            il.Emit (OpCodes.Ldloc, registers);
                            il.Emit (OpCodes.Ldloc, pointer);
                            il.Emit (OpCodes.Ldelem_U1);
                            il.Emit (OpCodes.Ldc_I4_0);
                            il.Emit (OpCodes.Beq, labelTo);
                            il.MarkLabel (labelFrom);
                        }
                        break;
                    case Basm.OpCode.Brtrue: {
                            Label labelFrom = instructionLabelMap[(Basm.Instruction)instruction.Operand];
                            Label labelTo = instructionLabelMap[instruction];

                            il.Emit (OpCodes.Ldloc, registers);
                            il.Emit (OpCodes.Ldloc, pointer);
                            il.Emit (OpCodes.Ldelem_U1);
                            il.Emit (OpCodes.Ldc_I4_0);
                            il.Emit (OpCodes.Bne_Un, labelFrom);
                            il.MarkLabel (labelTo);
                        }
                        break;
                }
            }

            if (hasOutput) {
                // yeah..
                il.EmitCall (OpCodes.Call, typeof (Console).GetMethod ("WriteLine", Type.EmptyTypes), Type.EmptyTypes);
            }

            il.Emit (OpCodes.Ret);

            program.CreateType ();
            assembly.SetEntryPoint (main);

            return assembly;
        }
    }
}
