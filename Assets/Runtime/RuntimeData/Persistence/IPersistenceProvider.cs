using System.Collections.Generic;

namespace BB.Framework
{
    public interface IPersistenceProvider
    {
        bool TryLoad(out Dictionary<string, string> blob);
        void Save(Dictionary<string, string> blob);
    }
}