using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IEditorFormatMapTextSpecificListener
    {
        void ListenFor(List<string> keys, Action callback);
        void PauseListeningWhenExecuting(Action value);
    }
}
