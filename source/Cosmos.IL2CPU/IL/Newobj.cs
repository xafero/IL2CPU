using System;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;
using CPUx86 = XSharp.Assembler.x86;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Newobj)]
    public class Newobj : ILOp
    {
        public Newobj(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xMethod = (OpMethod)aOpCode;
            var xCurrentLabel = GetLabel(aMethod, aOpCode);
            var xType = xMethod.Value.DeclaringType;

            Assemble(Assembler, aMethod, xMethod, xCurrentLabel, xType, xMethod.Value, DebugEnabled);
        }

        public static void Assemble(Assembler aAssembler, _MethodInfo aMethod, OpMethod xMethod, string currentLabel, Type objectType, MethodBase constructor, bool debugEnabled)
        {
            // call cctor:
            if (aMethod != null)
            {
                var xCctor = (objectType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic) ?? Array.Empty<ConstructorInfo>()).SingleOrDefault();
                if (xCctor != null)
                {
                    XS.Call(LabelName.Get(xCctor));
                    EmitExceptionLogic(aAssembler, aMethod, xMethod, true, null, ".AfterCCTorExceptionCheck");
                    XS.Label(".AfterCCTorExceptionCheck");
                }
            }

            if (objectType.IsValueType)
            {
                #region Valuetypes

                XS.Comment("ValueType");
                XS.Comment("Type: " + objectType);

                /*
                 * Current sitation on stack:
                 *   $ESP       Arg
                 *   $ESP+..    other items
                 *
                 * What should happen:
                 *  + The stack should be increased to allow space to contain:
                 *         + .ctor arguments
                 *         + struct _pointer_ (ref to start of emptied space)
                 *         + empty space for struct
                 *  + arguments should be copied to the new place
                 *  + old place where arguments were should be cleared
                 *  + pointer should be set
                 *  + call .ctor
                 */

                // Size of return value - we need to make room for this on the stack.
                uint xStorageSize = Align(SizeOfType(objectType), 4);
                XS.Comment("StorageSize: " + xStorageSize);
                if (xStorageSize == 0)
                {
                    throw new Exception("ValueType storage size cannot be 0.");
                }

                uint xArgSize = 0;
                var xParameterList = constructor.GetParameters();
                foreach (var xParam in xParameterList)
                {
                    xArgSize = xArgSize + Align(SizeOfType(xParam.ParameterType), 4);
                }
                XS.Comment("ArgSize: " + xArgSize);

                // set source of args copy
                XS.Set(ESI, ESP);

                // allocate space for struct
                XS.Sub(ESP, xStorageSize + 4);

                // set destination and count of args copy
                XS.Set(EDI, ESP);
                XS.Set(ECX, xArgSize / 4);

                // move the args to their new location
                new CPUx86.Movs { Size = 32, Prefixes = CPUx86.InstructionPrefixes.Repeat };

                // set struct ptr
                XS.Set(EAX, ESP);
                XS.Add(EAX, xArgSize + 4);
                XS.Set(ESP, EAX, destinationDisplacement: (int)xArgSize);

                XS.Push(EAX);

                var xOpType = new OpType(xMethod.OpCode, xMethod.Position, xMethod.NextPosition, xMethod.Value.DeclaringType, xMethod.CurrentExceptionRegion);
                new Initobj(aAssembler).Execute(aMethod, xOpType);

                new Call(aAssembler).Execute(aMethod, xMethod);

                // Need to put these *after* the call because the Call pops the args from the stack
                // and we have mucked about on the stack, so this makes it right before the next
                // op.

                #endregion Valuetypes
            }
            else
            {
                // If not ValueType, then we need gc

                var xParams = constructor.GetParameters();

                // array length + 8
                bool xHasCalcSize = false;

                #region Special string handling
                // try calculating size:
                if (constructor.DeclaringType == BaseTypes.String)
                {
                    if (xParams.Length == 1 && xParams[0].ParameterType == Base.CharArray)
                    {
                        xHasCalcSize = true;
                        XS.Set(EAX, ESP, sourceDisplacement: 4, sourceIsIndirect: true); // address
                        XS.Set(EAX, EAX, sourceDisplacement: 8, sourceIsIndirect: true); // element count
                        XS.Set(EDX, 2); // element size
                        XS.Multiply(EDX);
                        XS.Push(EAX);
                    }
                    else if (xParams.Length == 3
                             && (xParams[0].ParameterType == Base.CharArray || xParams[0].ParameterType == Base.CharStar)
                             && xParams[1].ParameterType == BaseTypes.Int32
                             && xParams[2].ParameterType == BaseTypes.Int32)
                    {
                        xHasCalcSize = true;
                        XS.Set(EAX, ESP, sourceIsIndirect: true);
                        XS.ShiftLeft(EAX, 1);
                        XS.Push(EAX);
                    }
                    else if (xParams.Length == 2
                             && xParams[0].ParameterType == BaseTypes.Char
                             && xParams[1].ParameterType == BaseTypes.Int32)
                    {
                        xHasCalcSize = true;
                        XS.Set(EAX, ESP, sourceIsIndirect: true);
                        XS.ShiftLeft(EAX, 1);
                        XS.Push(EAX);
                    }
                    /*
                     * TODO see if something is needed in stack / register to make them really work
                     */
                    else if (xParams.Length == 3
                             && (xParams[0].ParameterType == Base.SbyteStar
                             && xParams[1].ParameterType == BaseTypes.Int32
                             && xParams[2].ParameterType == BaseTypes.Int32))
                    {
                        xHasCalcSize = true;
                        XS.Push(ESP, isIndirect: true);
                    }
                    else if (xParams.Length == 1 && (xParams[0].ParameterType == Base.SbyteStar))
                    {
                        xHasCalcSize = true;
                        /* xParams[0] contains a C / ASCII Z string the following ASM is de facto the C strlen() function */
                        var xSByteCountLabel = currentLabel + ".SByteCount";

                        XS.Set(EAX, ESP, sourceIsIndirect: true);
                        XS.Or(ECX, 0xFFFFFFFF);

                        XS.Label(xSByteCountLabel);

                        XS.Increment(EAX);
                        XS.Increment(ECX);

                        XS.Compare(EAX, 0, destinationIsIndirect: true);
                        XS.Jump(CPUx86.ConditionalTestEnum.NotEqual, xSByteCountLabel);

                        XS.Push(ECX);
                    }
                    else if (xParams.Length == 1 && (xParams[0].ParameterType == Base.CharStar))
                    {
                        xHasCalcSize = true;
                        /* xParams[0] contains a C / ASCII Z string the following ASM is de facto the C strlen() function */
                        // todo: does this actually work for empty strings?
                        var xSByteCountLabel = currentLabel + ".SByteCount";

                        XS.Set(EAX, ESP, sourceIsIndirect: true);
                        XS.Or(ECX, 0xFFFFFFFF);

                        XS.Label(xSByteCountLabel);

                        XS.Increment(EAX); // a char is two bytes
                        XS.Increment(EAX);
                        XS.Increment(ECX);
                        XS.Set(EBX, EAX, sourceIsIndirect: true);
                        XS.And(EBX, 0xFF); // Only compare the char
                        XS.Compare(EBX, 0);
                        XS.Jump(CPUx86.ConditionalTestEnum.NotEqual, xSByteCountLabel);

                        XS.ShiftLeft(ECX, 1); // every character needs two bytes
                        XS.Push(ECX);
                    }
                    else if(xParams.Length == 1 && xParams[0].ParameterType == Base.ReadOnlySpanChar)
                    {
                        xHasCalcSize = true;
                        // push the lenght of the span as well
                        // ReadOnlySpan<char> in memory is a Pointer and Length, simply dup the length and multiply by 2 to get the length to allocate
                        XS.Set(EAX, ESP, sourceIsIndirect: true, sourceDisplacement: 4);
                        XS.ShiftLeft(EAX, 1);
                        XS.Push(EAX);
                    }
                    else
                    {
                        // You actually have to do something to implement a new ctor. For every ctor, newobj has to calculate the size of the string being allocated so that the GC can give enough space.
                        // If this is not done, it will seem to work until a new object is allocated in the space after the string overwriting the string data. This may only happen for long enough strings i.e.
                        // strings with more than one character
                        throw new NotImplementedException();
                    }
                }
                #endregion Special string handling

                uint xMemSize = GetStorageSize(objectType);
                int xExtraSize = 12; // additional size for set values after alloc
                XS.Push((uint)(xMemSize + xExtraSize));
                if (xHasCalcSize)
                {
                    XS.Pop(EAX);
                    XS.Add(ESP, EAX, destinationIsIndirect: true);
                }

                // todo: probably we want to check for exceptions after calling Alloc
                XS.Call(LabelName.Get(GCImplementationRefs.AllocNewObjectRef));
                XS.Label(".AfterAlloc");
                XS.Push(ESP, isIndirect: true);
                XS.Push(ESP, isIndirect: true);
                // it's on the stack now 3 times. Once from the Alloc return value, twice from the pushes

                // todo: use a cleaner approach here. this class shouldnt assemble the string
                string strTypeId = GetTypeIDLabel(constructor.DeclaringType);

                XS.Pop(EAX);
                XS.Set(EBX, strTypeId, sourceIsIndirect: true);
                XS.Set(EAX, EBX, destinationIsIndirect: true);
                XS.Set(EAX, (uint)ObjectUtils.InstanceTypeEnum.NormalObject, destinationDisplacement: 4, destinationIsIndirect: true, size: RegisterSize.Int32);
                XS.Set(EAX, xMemSize, destinationDisplacement: 8, destinationIsIndirect: true, size: RegisterSize.Int32);
                uint xSize = (uint)(from item in xParams
                                    let xQSize = Align(SizeOfType(item.ParameterType), 4)
                                    select (int)xQSize).Take(xParams.Length).Sum();
                XS.Push(0);

                foreach (var xParam in xParams)
                {
                    uint xParamSize = Align(SizeOfType(xParam.ParameterType), 4);
                    XS.Comment($"Arg {xParam.Name}: {xParamSize}");
                    for (int i = 0; i < xParamSize; i += 4)
                    {
                        XS.Push(ESP, isIndirect: true, displacement: (int)(xSize + 8));
                    }
                }

                XS.Call(LabelName.Get(constructor));
                // should the complete error handling happen by ILOp.EmitExceptionLogic?
                if (aMethod != null)
                {
                    // todo: only happening for real methods now, not for ctor's ?
                    XS.Test(ECX, 2);
                    string xNoErrorLabel = currentLabel + ".NoError" + LabelName.LabelCount.ToString();
                    XS.Jump(CPUx86.ConditionalTestEnum.Equal, xNoErrorLabel);

                    PushAlignedParameterSize(constructor);

                    // an exception occurred, we need to cleanup the stack, and jump to the exit
                    XS.Add(ESP, 4);

                    new Comment(aAssembler, "[ Newobj.Execute cleanup end ]");
                    Jump_Exception(aMethod);
                    XS.Label(xNoErrorLabel);
                }
                XS.Pop(EAX);

                PushAlignedParameterSize(constructor);

                XS.Push(EAX);
                XS.Push(0);
            }
        }

        private static void PushAlignedParameterSize(MethodBase aMethod)
        {
            ParameterInfo[] xParams = aMethod.GetParameters();

            uint xSize;
            XS.Comment("[ Newobj.PushAlignedParameterSize start count = " + xParams.Length.ToString() + " ]");
            for (int i = 0; i < xParams.Length; i++)
            {
                xSize = SizeOfType(xParams[i].ParameterType);
                XS.Add(ESP, Align(xSize, 4));
            }
            XS.Comment("[ Newobj.PushAlignedParameterSize end ]");
        }
    }
}
