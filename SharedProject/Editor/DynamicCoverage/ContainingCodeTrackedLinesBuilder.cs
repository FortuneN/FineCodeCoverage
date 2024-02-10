using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesBuilder : ITrackedLinesFactory
    {
        private readonly IRoslynService roslynService;
        private readonly ILinesContainingCodeTrackerFactory containingCodeTrackerFactory;
        private readonly IContainingCodeTrackedLinesFactory trackedLinesFactory;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            IRoslynService roslynService,
            ILinesContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory trackedLinesFactory,
            IThreadHelper threadHelper
        )
        {
            this.roslynService = roslynService;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.trackedLinesFactory = trackedLinesFactory;
            this.threadHelper = threadHelper;
        }

        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            var startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            var endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }
        
        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            var containingCodeTrackers = CreateContainingCodeTrackers(lines, textSnapshot, language);
            return trackedLinesFactory.Create(containingCodeTrackers);
        }

        private List<IContainingCodeTracker> CreateContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            if( lines.Count == 0 ) return Enumerable.Empty<IContainingCodeTracker>().ToList();

            if (language == Language.CPP)
            {
                return lines.Select(line => containingCodeTrackerFactory.Create(textSnapshot, line)).ToList();
            }

            return CreateRoslynContainingCodeTrackers(lines, textSnapshot);
            
        }

        private List<IContainingCodeTracker> CreateRoslynContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot)
        {
            List<IContainingCodeTracker> containingCodeTrackers = new List<IContainingCodeTracker>();

            void AddSingleLine(ILine line)
            {
                containingCodeTrackers.Add(containingCodeTrackerFactory.Create(textSnapshot, line));
            }
           
            var roslynContainingCodeSpans = threadHelper.JoinableTaskFactory.Run(() => roslynService.GetContainingCodeSpansAsync(textSnapshot));
            var currentCodeSpanIndex = -1;
            CodeSpanRange currentCodeSpanRange = null;
            SetNextCodeSpanRange();
            var containedLines = new List<ILine>();

            void SetNextCodeSpanRange()
            {
                currentCodeSpanIndex++;
                if (currentCodeSpanIndex < roslynContainingCodeSpans.Count)
                {
                    currentCodeSpanRange = GetCodeSpanRange(roslynContainingCodeSpans[currentCodeSpanIndex], textSnapshot);
                }
                else
                {
                    currentCodeSpanRange = null;
                }
            }

            void CollectContainedLines()
            {
                if (containedLines.Count > 0)
                {
                    containingCodeTrackers.Add(containingCodeTrackerFactory.Create(textSnapshot, containedLines, currentCodeSpanRange));
                }
                containedLines = new List<ILine>();
            }

            void LineAction(ILine line)
            {
                if (currentCodeSpanRange == null)
                {
                    AddSingleLine(line);
                }
                else
                {
                    var adjustedLine = line.Number - 1;
                    if (adjustedLine < currentCodeSpanRange.StartLine)
                    {
                        AddSingleLine(line);
                    }
                    else if (adjustedLine > currentCodeSpanRange.EndLine)
                    {
                        CollectContainedLines();
                        SetNextCodeSpanRange();
                        LineAction(line);

                    }
                    else
                    {
                        containedLines.Add(line);
                    }
                }
            }

            foreach (var line in lines) // these are in order`
            {
                LineAction(line);
            }

            CollectContainedLines();
                

            return containingCodeTrackers;
        }
    }
}
