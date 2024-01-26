using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
	internal class GlyphDataContext : INotifyPropertyChanged, IListener<CoverageColoursChangedMessage>
	{
        private readonly CoverageType coverageType;
        private Color colour;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Width { get; } = 3;
		public int Height { get; } = 16;
        public Color Colour
        {
            get => colour;
            set
            {
                colour = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Colour)));
            }
        }
        public GlyphDataContext(CoverageType coverageType,Color initialColor)
        {
            this.coverageType = coverageType;
            Colour = initialColor;
        }

        public void Handle(CoverageColoursChangedMessage message)
        {
            var newColor = message.CoverageColours.GetColor(coverageType);
            if (newColor != Colour)
            {
                Colour = newColor;
            }
        }
    }

	internal interface IMVVMGlyphFactory
	{
		object CreateDataContext(Engine.Cobertura.Line coverageLine);
		FrameworkElement CreateUIElement();
	}

	[Export(typeof(IMVVMGlyphFactory))]
    internal class MVVMGlyphFactory : IMVVMGlyphFactory
    {
        
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private readonly IEventAggregator eventAggregator;

        [ImportingConstructor]
        public MVVMGlyphFactory(
            ICoverageColoursProvider coverageColoursProvider,
            IEventAggregator eventAggregator
            )
        {
            this.coverageColoursProvider = coverageColoursProvider;
            this.eventAggregator = eventAggregator;
        }

        public object CreateDataContext(Engine.Cobertura.Line coverageLine)
        {
            var coverageType = coverageLine.GetCoverageType();
            var initialColor = coverageColoursProvider.GetCoverageColours().GetColor(coverageType);
            var dc =  new GlyphDataContext(coverageType, initialColor);
            eventAggregator.AddListener(dc, false);
            return dc;
        }
		public FrameworkElement CreateUIElement()
		{
            return new Glyph();
            //return new GlyphVisual();
		}
    }

    public class GlyphVisual : FrameworkElement
    {
        private VisualCollection _children;
        private DrawingVisual rectangle;

        public Color Fill
        {
            get { return (Color)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Color), typeof(GlyphVisual), new PropertyMetadata(Colors.AliceBlue,(d,args) =>
            {
                (d as GlyphVisual).UpdateRectangleFill();
            }));


        public GlyphVisual()
        {
            _children = new VisualCollection(this);
            rectangle = new DrawingVisual();
            
            _children.Add(rectangle);
            Binding binding = new Binding("Colour");
            this.SetBinding(FillProperty, binding);

        }

        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        protected void UpdateRectangleFill()
        {
            var dc = rectangle.RenderOpen();
            Rect rect = new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(3, 16));
            dc.DrawRectangle(new SolidColorBrush(Fill), (System.Windows.Media.Pen)null, rect);
            dc.Close();
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _children[index];
        }

    }

    internal class CoverageLineGlyphFactory : IGlyphFactory
	{
        private readonly IMVVMGlyphFactory glyphDataContextFactory;
        
        public CoverageLineGlyphFactory(IMVVMGlyphFactory glyphDataContextFactory)
        {
            this.glyphDataContextFactory = glyphDataContextFactory;
        }

        public UIElement GenerateGlyph(IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			if (!(glyphTag is CoverageLineGlyphTag tag))
			{
				return null;
			}

            //return new Rectangle
            //{
            //    Fill = new SolidColorBrush(Colors.AliceBlue),
            //    Width = 3,
            //    Height = 16
            //};

            //a) does scrolling increase memory ?
            // b) could a custom control have different result.

            var dataContext = glyphDataContextFactory.CreateDataContext(tag.CoverageLine);
            var uiElement = glyphDataContextFactory.CreateUIElement();
            uiElement.DataContext = dataContext;
            
            return uiElement;
        }
	}
}
