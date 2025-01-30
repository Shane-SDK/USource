using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USource.Formats
{
    public interface ISourceObject
    {
        public void ReadToObject(UReader reader, int version = default);
        public void ReadToObject(UReader reader, long startReadPosition, int version = default)
        {
            reader.BaseStream.Position = startReadPosition;
            this.ReadToObject(reader, version);
        }
    }
}
