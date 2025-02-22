﻿//#define COSMOSDEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using IL2CPU.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU
{
    // ILOpcode represents the opcode during for scanning.
    // Do not:
    //   Include reference to ILOp, the scanner should do that
    //   Include reference to System.Reflection.Emit, this is metadata
    //     only needed by reader and not ILOpCode
    public abstract class ILOpCode
    {
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
        public enum Code : ushort
        {
            #region Values

            Nop = 0x0000,
            Break = 0x0001,
            Ldarg_0 = 0x0002,
            Ldarg_1 = 0x0003,
            Ldarg_2 = 0x0004,
            Ldarg_3 = 0x0005,
            Ldloc_0 = 0x0006,
            Ldloc_1 = 0x0007,
            Ldloc_2 = 0x0008,
            Ldloc_3 = 0x0009,
            Stloc_0 = 0x000A,
            Stloc_1 = 0x000B,
            Stloc_2 = 0x000C,
            Stloc_3 = 0x000D,
            Ldarg_S = 0x000E,
            Ldarga_S = 0x000F,
            Starg_S = 0x0010,
            Ldloc_S = 0x0011,
            Ldloca_S = 0x0012,
            Stloc_S = 0x0013,
            Ldnull = 0x0014,
            Ldc_I4_M1 = 0x0015,
            Ldc_I4_0 = 0x0016,
            Ldc_I4_1 = 0x0017,
            Ldc_I4_2 = 0x0018,
            Ldc_I4_3 = 0x0019,
            Ldc_I4_4 = 0x001A,
            Ldc_I4_5 = 0x001B,
            Ldc_I4_6 = 0x001C,
            Ldc_I4_7 = 0x001D,
            Ldc_I4_8 = 0x001E,
            Ldc_I4_S = 0x001F,
            Ldc_I4 = 0x0020,
            Ldc_I8 = 0x0021,
            Ldc_R4 = 0x0022,
            Ldc_R8 = 0x0023,
            Dup = 0x0025,
            Pop = 0x0026,
            Jmp = 0x0027,
            Call = 0x0028,
            Calli = 0x0029,
            Ret = 0x002A,
            Br_S = 0x002B,
            Brfalse_S = 0x002C,
            Brtrue_S = 0x002D,
            Beq_S = 0x002E,
            Bge_S = 0x002F,
            Bgt_S = 0x0030,
            Ble_S = 0x0031,
            Blt_S = 0x0032,
            Bne_Un_S = 0x0033,
            Bge_Un_S = 0x0034,
            Bgt_Un_S = 0x0035,
            Ble_Un_S = 0x0036,
            Blt_Un_S = 0x0037,
            Br = 0x0038,
            Brfalse = 0x0039,
            Brtrue = 0x003A,
            Beq = 0x003B,
            Bge = 0x003C,
            Bgt = 0x003D,
            Ble = 0x003E,
            Blt = 0x003F,
            Bne_Un = 0x0040,
            Bge_Un = 0x0041,
            Bgt_Un = 0x0042,
            Ble_Un = 0x0043,
            Blt_Un = 0x0044,
            Switch = 0x0045,
            Ldind_I1 = 0x0046,
            Ldind_U1 = 0x0047,
            Ldind_I2 = 0x0048,
            Ldind_U2 = 0x0049,
            Ldind_I4 = 0x004A,
            Ldind_U4 = 0x004B,
            Ldind_I8 = 0x004C,
            Ldind_I = 0x004D,
            Ldind_R4 = 0x004E,
            Ldind_R8 = 0x004F,
            Ldind_Ref = 0x0050,
            Stind_Ref = 0x0051,
            Stind_I1 = 0x0052,
            Stind_I2 = 0x0053,
            Stind_I4 = 0x0054,
            Stind_I8 = 0x0055,
            Stind_R4 = 0x0056,
            Stind_R8 = 0x0057,
            Add = 0x0058,
            Sub = 0x0059,
            Mul = 0x005A,
            Div = 0x005B,
            Div_Un = 0x005C,
            Rem = 0x005D,
            Rem_Un = 0x005E,
            And = 0x005F,
            Or = 0x0060,
            Xor = 0x0061,
            Shl = 0x0062,
            Shr = 0x0063,
            Shr_Un = 0x0064,
            Neg = 0x0065,
            Not = 0x0066,
            Conv_I1 = 0x0067,
            Conv_I2 = 0x0068,
            Conv_I4 = 0x0069,
            Conv_I8 = 0x006A,
            Conv_R4 = 0x006B,
            Conv_R8 = 0x006C,
            Conv_U4 = 0x006D,
            Conv_U8 = 0x006E,
            Callvirt = 0x006F,
            Cpobj = 0x0070,
            Ldobj = 0x0071,
            Ldstr = 0x0072,
            Newobj = 0x0073,
            Castclass = 0x0074,
            Isinst = 0x0075,
            Conv_R_Un = 0x0076,
            Unbox = 0x0079,
            Throw = 0x007A,
            Ldfld = 0x007B,
            Ldflda = 0x007C,
            Stfld = 0x007D,
            Ldsfld = 0x007E,
            Ldsflda = 0x007F,
            Stsfld = 0x0080,
            Stobj = 0x0081,
            Conv_Ovf_I1_Un = 0x0082,
            Conv_Ovf_I2_Un = 0x0083,
            Conv_Ovf_I4_Un = 0x0084,
            Conv_Ovf_I8_Un = 0x0085,
            Conv_Ovf_U1_Un = 0x0086,
            Conv_Ovf_U2_Un = 0x0087,
            Conv_Ovf_U4_Un = 0x0088,
            Conv_Ovf_U8_Un = 0x0089,
            Conv_Ovf_I_Un = 0x008A,
            Conv_Ovf_U_Un = 0x008B,
            Box = 0x008C,
            Newarr = 0x008D,
            Ldlen = 0x008E,
            Ldelema = 0x008F,
            Ldelem_I1 = 0x0090,
            Ldelem_U1 = 0x0091,
            Ldelem_I2 = 0x0092,
            Ldelem_U2 = 0x0093,
            Ldelem_I4 = 0x0094,
            Ldelem_U4 = 0x0095,
            Ldelem_I8 = 0x0096,
            Ldelem_I = 0x0097,
            Ldelem_R4 = 0x0098,
            Ldelem_R8 = 0x0099,
            Ldelem_Ref = 0x009A,
            Stelem_I = 0x009B,
            Stelem_I1 = 0x009C,
            Stelem_I2 = 0x009D,
            Stelem_I4 = 0x009E,
            Stelem_I8 = 0x009F,
            Stelem_R4 = 0x00A0,
            Stelem_R8 = 0x00A1,
            Stelem_Ref = 0x00A2,
            Ldelem = 0x00A3,
            Stelem = 0x00A4,
            Unbox_Any = 0x00A5,
            Conv_Ovf_I1 = 0x00B3,
            Conv_Ovf_U1 = 0x00B4,
            Conv_Ovf_I2 = 0x00B5,
            Conv_Ovf_U2 = 0x00B6,
            Conv_Ovf_I4 = 0x00B7,
            Conv_Ovf_U4 = 0x00B8,
            Conv_Ovf_I8 = 0x00B9,
            Conv_Ovf_U8 = 0x00BA,
            Refanyval = 0x00C2,
            Ckfinite = 0x00C3,
            Mkrefany = 0x00C6,
            Ldtoken = 0x00D0,
            Conv_U2 = 0x00D1,
            Conv_U1 = 0x00D2,
            Conv_I = 0x00D3,
            Conv_Ovf_I = 0x00D4,
            Conv_Ovf_U = 0x00D5,
            Add_Ovf = 0x00D6,
            Add_Ovf_Un = 0x00D7,
            Mul_Ovf = 0x00D8,
            Mul_Ovf_Un = 0x00D9,
            Sub_Ovf = 0x00DA,
            Sub_Ovf_Un = 0x00DB,
            Endfinally = 0x00DC,
            Leave = 0x00DD,
            Leave_S = 0x00DE,
            Stind_I = 0x00DF,
            Conv_U = 0x00E0,
            Prefix7 = 0x00F8,
            Prefix6 = 0x00F9,
            Prefix5 = 0x00FA,
            Prefix4 = 0x00FB,
            Prefix3 = 0x00FC,
            Prefix2 = 0x00FD,
            Prefix1 = 0x00FE,
            Prefixref = 0x00FF,
            Arglist = 0xFE00,
            Ceq = 0xFE01,
            Cgt = 0xFE02,
            Cgt_Un = 0xFE03,
            Clt = 0xFE04,
            Clt_Un = 0xFE05,
            Ldftn = 0xFE06,
            Ldvirtftn = 0xFE07,
            Ldarg = 0xFE09,
            Ldarga = 0xFE0A,
            Starg = 0xFE0B,
            Ldloc = 0xFE0C,
            Ldloca = 0xFE0D,
            Stloc = 0xFE0E,
            Localloc = 0xFE0F,
            Endfilter = 0xFE11,
            Unaligned = 0xFE12,
            Volatile = 0xFE13,
            Tailcall = 0xFE14,
            Initobj = 0xFE15,
            Constrained = 0xFE16,
            Cpblk = 0xFE17,
            Initblk = 0xFE18,
            Rethrow = 0xFE1A,
            Sizeof = 0xFE1C,
            Refanytype = 0xFE1D,
            Readonly = 0xFE1E

            #endregion
        }

        public readonly Code OpCode;

        // Op offset within method. Used for labels etc in assembly.
        public readonly int Position;

        // position of the next instruction
        public readonly int NextPosition;

        public readonly _ExceptionRegionInfo CurrentExceptionRegion;

        protected ILOpCode(Code aOpCode, int aPos, int aNextPos, _ExceptionRegionInfo aCurrentExceptionRegion)
        {
            OpCode = aOpCode;
            Position = aPos;
            NextPosition = aNextPos;
            CurrentExceptionRegion = aCurrentExceptionRegion;
        }

        public override string ToString()
        {
            // leave here, makes easier debugging the compiler. Compiler will
            // show for example "IL_0001: ldstr" instead of just ILOpCode
            return $"IL_{Position:X4}: {OpCode}";
        }

        /// <summary>
        /// Returns the number of items popped from the stack. This is the logical stack, not physical items.
        /// So a 100byte struct is 1 pop, even though it might be multiple 32-bit or 64-bit words on the stack.
        /// </summary>
        /// <param name="aMethod"></param>
        public abstract int GetNumberOfStackPops(MethodBase aMethod);

        /// <summary>
        /// Returns the number of items pushed to the stack. This is the logical stack, not physical items.
        /// So a 100byte struct is 1 pop, even though it might be multiple 32-bit or 64-bit words on the stack.
        /// </summary>
        /// <param name="aMethod"></param>
        public abstract int GetNumberOfStackPushes(MethodBase aMethod);

        public Type[] StackPopTypes { get; set; }

        public Type[] StackPushTypes { get; set; }

        internal void InitStackAnalysis(MethodBase aMethod)
        {
            StackPopTypes = new Type[GetNumberOfStackPops(aMethod)];
            StackPushTypes = new Type[GetNumberOfStackPushes(aMethod)];
            DoInitStackAnalysis(aMethod);
        }

        protected virtual void DoInitStackAnalysis(MethodBase aMethod)
        {
        }


        /// <summary>
        /// Gets set to true on first interpreter processing. Is used for loop detection
        /// </summary>
        internal bool Processed = false;

        public uint? StackOffsetBeforeExecution = null;
        public void DoStackAnalysis(Stack<Type> aStack, ref uint aStackOffset)
        {
            StackOffsetBeforeExecution = aStackOffset;

            // if current instruction is the first instruction of a filter or catch statement, "push" the exception type now
            if (CurrentExceptionRegion  != null && (CurrentExceptionRegion.HandlerOffset == Position ||
              (CurrentExceptionRegion.FilterOffset == Position && CurrentExceptionRegion.FilterOffset != 0)))
            {
                if (CurrentExceptionRegion.Kind != ExceptionRegionKind.Finally)
                {
                    aStack.Push(BaseTypes.Object);
                    aStackOffset += ILOp.Align(ILOp.SizeOfType(BaseTypes.Object), 4);
                }
            }


            if (StackPopTypes.Length > aStack.Count)
            {
                throw new Exception(String.Format("OpCode {0} tries to pop more stuff from analytical stack than there is!", this));
            }

            var pos = 0;
            foreach (var xPopItem in StackPopTypes)
            {
                var popped = aStack.Pop();

                if (xPopItem is null)
                {
                    StackPopTypes[pos] = popped;
                }
                else if(xPopItem != popped && xPopItem.IsAssignableTo(popped))
                {
                    throw new Exception($"Tried to pop a {xPopItem} from the stack but found a {popped}");
                }

                aStackOffset -= ILOp.Align(ILOp.SizeOfType(popped), 4);

                pos++;
            }

            DoInterpretStackTypes();

            foreach (var xPushItem in StackPushTypes)
            {
                aStack.Push(xPushItem);
                aStackOffset += ILOp.Align(ILOp.SizeOfType(xPushItem), 4);
            }
        }

        /// <summary>
        /// Based on updated StackPopTypes, try to update
        /// </summary>
        public abstract void DoInterpretStackTypes();

        [Conditional("COSMOSDEBUG")]
        public static void ILInterpretationDebugLine(Func<string> message)
        {
            Console.WriteLine(message());
        }

        /// <summary>
        /// Return the position of all instructions which can be reached from this one and if they should be part of the current group or not
        /// </summary>
        /// <returns></returns>
        public virtual List<(bool newGroup, int Position)> GetNextOpCodePositions()
        {
            return new List<(bool newGroup, int Position)> { (false, NextPosition) };
        }
    }
}
