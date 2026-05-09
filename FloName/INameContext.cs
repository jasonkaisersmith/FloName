using System;
using System.Collections.Generic;
using System.Text;

namespace FloName
{
    public interface INameContext
    {
        int GetSequence(string key, int start);
        bool RegisterUnique(string key, string value);
    }
}
