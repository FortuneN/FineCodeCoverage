using System;

namespace FineCodeCoverage.Core.Utilities
{
    interface IFileRenameListener
    {
        void ListenForFileRename(Action<string, string> callback);
    }

}
