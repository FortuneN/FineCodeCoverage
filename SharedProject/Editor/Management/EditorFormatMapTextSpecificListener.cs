using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(IEditorFormatMapTextSpecificListener))]
    internal class EditorFormatMapTextSpecificListener : IEditorFormatMapTextSpecificListener
    {
        private List<string> keys;
        private Action callback;
        [ImportingConstructor]
        public EditorFormatMapTextSpecificListener(
            IEditorFormatMapService editorFormatMapService
        )
        {
            editorFormatMapService.GetEditorFormatMap("text").FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
        }

        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            var watchedItems = e.ChangedItems.Where(changedItem => keys.Contains(changedItem)).ToList();
            if (listening && watchedItems.Any())
            {
                callback();
            }
        }

        private bool listening;

        public void ListenFor(List<string> keys, Action callback)
        {
            this.keys = keys;
            this.callback = callback;
            listening = true;
        }

        public void PauseListeningWhenExecuting(Action action)
        {
            listening = false;
            action();
            listening = true;
        }
    }
}
