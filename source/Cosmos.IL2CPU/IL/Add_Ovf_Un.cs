using System;
using XSharp.Assembler.x86;
using XSharp;
using static XSharp.XSRegisters;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;

/* Add.Ovf is unsigned integer addition with check for overflow */
namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Add_Ovf_Un)]
    public class Add_Ovf_Un : ILOp
    {
        public Add_Ovf_Un(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            // TODO overflow check for float
            var xType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xType);
            var xIsFloat = TypeIsFloat(xType);

            if (xIsFloat)
            {
                throw new Exception("Cosmos.IL2CPU.x86->IL->Add_Ovf_Un.cs->Error: Expected unsigned integer operands but get float!");
            }

            if (xSize > 8)
            {
                //EmitNotImplementedException( Assembler, aServiceProvider, "Size '" + xSize.Size + "' not supported (add)", aCurrentLabel, aCurrentMethodInfo, aCurrentOffset, aNextLabel );
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Add_Ovf_Un.cs->Error: StackSize > 8 not supported");
            }
            else
            {
                var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
                var xSuccessLabel = xBaseLabel + "Success";
                if (xSize > 4) // long
                {
                    XS.Pop(EDX); // low part
                    XS.Pop(EAX); // high part
                    XS.Add(ESP, EDX, destinationIsIndirect: true);
                    XS.AddWithCarry(ESP, EAX, destinationDisplacement: 4);
                }
                else //integer
                {
                    XS.Pop(EAX);
                    XS.Add(ESP, EAX, destinationIsIndirect: true);
                }

                // Let's check if we add overflow and if so throw OverflowException
                XS.Jump(ConditionalTestEnum.NotCarry, xSuccessLabel);
                if (xSize > 4) // Hack to stop stack corruption
                {
                  XS.Add(ESP, 8);
                }
                else
                {
                  XS.Add(ESP, 4);
                }
                Call.DoExecute(Assembler, aMethod, Base.ExceptionHelper.GetMethod("ThrowOverflow", BindingFlags.Static | BindingFlags.Public), aOpCode, GetLabel(aMethod, aOpCode), xSuccessLabel, DebugEnabled);
                XS.Label(xSuccessLabel);
                }
        }
    }
}
