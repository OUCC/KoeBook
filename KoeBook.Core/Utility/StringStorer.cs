using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoeBook.Core.Utility
{
    public class StringStorer
    {
        private StringBuilder _stringBuilder = new();

        public void Store(string text)
        {
            _stringBuilder.Append(text);
        }

        public string Release()
        {
            var result = _stringBuilder.ToString();
            _stringBuilder.Clear();
            return result;
        }
    }
}
