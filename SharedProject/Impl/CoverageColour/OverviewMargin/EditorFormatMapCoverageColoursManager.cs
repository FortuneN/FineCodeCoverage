using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IEditorFormatMapCoverageColoursManager))]
    internal class EditorFormatMapCoverageColoursManager : IEditorFormatMapCoverageColoursManager
    {
        private bool prepared = false;
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private readonly ICoverageColours coverageColours;
        private readonly IEditorFormatMap editorFormatMap;
        private List<ICoverageEditorFormatDefinition> coverageEditorFormatDefinitions = new List<ICoverageEditorFormatDefinition>();

        [ImportingConstructor]
        public EditorFormatMapCoverageColoursManager(
            ICoverageColoursProvider coverageColoursProvider,
            ICoverageColours coverageColours,
            IEditorFormatMapService editorFormatMapService
        )
        {
            this.coverageColoursProvider = coverageColoursProvider;
            this.coverageColours = coverageColours;
            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            coverageColours.ColoursChanged += CoverageColours_ColoursChanged;
        }

        private void CoverageColours_ColoursChanged(object sender, EventArgs e)
        {
            if (prepared)
            {
                editorFormatMap.BeginBatchUpdate();
                foreach (var coverageEditorFormatDefinition in coverageEditorFormatDefinitions)
                {
                    var newBackgroundColor = GetBackgroundColor(coverageEditorFormatDefinition.CoverageType);
                    coverageEditorFormatDefinition.SetBackgroundColor(newBackgroundColor);
                    editorFormatMap.AddProperties(coverageEditorFormatDefinition.Identifier, coverageEditorFormatDefinition.CreateResourceDictionary());
                }
                editorFormatMap.EndBatchUpdate();
            }
        }

        public void Register(ICoverageEditorFormatDefinition coverageEditorFormatDefinition)
        {
            coverageEditorFormatDefinitions.Add(coverageEditorFormatDefinition);
            if (!prepared)
            {
                ThreadHelper.JoinableTaskFactory.Run(coverageColoursProvider.PrepareAsync);
                prepared = true;
            }
            Color backgroundColor = GetBackgroundColor(coverageEditorFormatDefinition.CoverageType);
            coverageEditorFormatDefinition.SetBackgroundColor(backgroundColor);
        }

        private Color GetBackgroundColor(CoverageType coverageType)
        {
            Color backgroundColor = default(Color);
            switch (coverageType)
            {
                case CoverageType.Covered:
                    backgroundColor = coverageColours.CoverageTouchedArea;
                    break;
                case CoverageType.NotCovered:
                    backgroundColor = coverageColours.CoverageNotTouchedArea;
                    break;
                case CoverageType.Partial:
                    backgroundColor = coverageColours.CoveragePartiallyTouchedArea;
                    break;
            }
            return backgroundColor;
        }
    }

}
