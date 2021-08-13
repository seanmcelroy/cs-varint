using System.Collections.Generic;
using System.Collections;

namespace VarInt.Tests
{
    public class UVarIntTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            for (ulong i = 0; i < 2000; i++)
                yield return new object[] { i };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}