using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Management
{
    interface IEditorFormatMapTextSpecificListener
    {
        void ListenFor(List<string> keys, Action callback);
        void PauseListeningWhenExecuting(Action value);
    }
}
