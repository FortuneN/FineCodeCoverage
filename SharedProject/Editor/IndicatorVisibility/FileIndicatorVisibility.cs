using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Output;

namespace FineCodeCoverage.Editor.IndicatorVisibility
{
    [Export(typeof(IInitializable))]
    [Export(typeof(IFileIndicatorVisibility))]
    internal class FileIndicatorVisibility : IFileIndicatorVisibility, IListener<ToggleCoverageIndicatorsMessage>, IInitializable
    {
        private bool showIndicators = true;
        public event EventHandler VisibilityChanged;

        [ImportingConstructor]
        public FileIndicatorVisibility(IEventAggregator eventAggregator) 
            => _ = eventAggregator.AddListener(this);

        public bool IsVisible(string filePath) => this.showIndicators;
        public void Handle(ToggleCoverageIndicatorsMessage message)
        {
            this.showIndicators = !this.showIndicators;
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
