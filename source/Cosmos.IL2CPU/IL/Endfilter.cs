using System;


namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Endfilter)]
    public class Endfilter : ILOp
    {
        public Endfilter(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            //todo actually do this correctly
            //should pop one int and then either go to finally block or go to catch block
        }


    }
}
