using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text.Classification;

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
        ) => editorFormatMapService.GetEditorFormatMap("text").FormatMappingChanged += this.EditorFormatMap_FormatMappingChanged;

        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            var watchedItems = e.ChangedItems.Where(changedItem => this.keys.Contains(changedItem)).ToList();
            if (this.listening && watchedItems.Any())
            {
                this.callback();
            }
        }

        private bool listening;

        public void ListenFor(List<string> keys, Action callback)
        {
            this.keys = keys;
            this.callback = callback;
            this.listening = true;
        }

        public void PauseListeningWhenExecuting(Action action)
        {
            this.listening = false;
            action();
            this.listening = true;
        }
    }
}
