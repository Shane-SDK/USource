using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USource.Converters
{
    public class VmfConverter : Converter
    {
        Stream stream;
        public VmfConverter(string sourcePath, Stream stream) : base(sourcePath, stream)
        {
            this.stream = stream;
        }

        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            return VMF.VMF.CreateFromVMF(stream, ctx);
        }
    }
}
