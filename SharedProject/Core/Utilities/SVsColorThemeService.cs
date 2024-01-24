using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FineCodeCoverage.Core.Utilities
{
    [ComImport]
    [Guid("0D915B59-2ED7-472A-9DE8-9161737EA1C5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    public interface SVsColorThemeService
    {
    }
    
    public interface IVsColorTheme
    {
        event EventHandler ThemeChanged;
        VsColorEntry GetColorEntry(ColorName colorName);
        string CurrentThemeName { get; }
    }

    public class ColorName
    {
        public ColorName(Guid category, string name)
        {
            Category = category;
            Name = name;
        }
        public Guid Category { get; }

        public string Name { get; }
    }

    public class VsColorEntry
    {
        public VsColorEntry(object iVsColorEntry, ColorName colorName)
        {
            var d = iVsColorEntry as dynamic;
            BackgroundType = d.BackgroundType;
            ForegroundType = d.ForegroundType;
            Background = d.Background;
            Foreground = d.Foreground;
            BackgroundSource = d.BackgroundSource;
            ForegroundSource = d.ForegroundSource;
            ColorName = colorName;
        }

        ColorName ColorName { get; }

        byte BackgroundType { get; }

        byte ForegroundType { get; }

        uint Background { get; }

        uint Foreground { get; }

        uint BackgroundSource { get; }

        uint ForegroundSource { get; }
    }

    [Export(typeof(IVsColorTheme))]
    public class VsColorTheme : IVsColorTheme
    {
        private object currentTheme;
        private string currentThemeName;
        private object colorThemeService;

        private PropertyInfo indexer;
        private Type colorNameType;

        [ImportingConstructor]
        public VsColorTheme(
        [Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider
        )
        {
            colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));
        }

        private object CurrentTheme
        {
            get
            {
                if (currentTheme == null)
                {
                    SetCurrentTheme();
                }
                return currentTheme;
            }
        }

        public string CurrentThemeName
        {
            get => (CurrentTheme as dynamic).Name;

        }

        private event EventHandler themeChanged;

        public event EventHandler ThemeChanged
        {
            add
            {
                themeChanged = value;
                Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += (e) =>
                {
                    SetCurrentTheme();
                    themeChanged?.Invoke(this, EventArgs.Empty);

                };
            }
            remove
            {
                themeChanged = null;
            }
        }

        private void SetCurrentTheme()
        {
            var currentTheme = (colorThemeService as dynamic).CurrentTheme;
            this.currentTheme = currentTheme;
        }

        public VsColorEntry GetColorEntry(ColorName colorName)
        {
            if (indexer == null)
            {
                indexer = CurrentTheme.GetType().GetProperty("Item");
                colorNameType = indexer.GetIndexParameters()[0].ParameterType;
            }

            var vsColorName = Activator.CreateInstance(colorNameType, true);
            (vsColorName as dynamic).Category = colorName.Category;
            (vsColorName as dynamic).Name = colorName.Name;

            var colorEntry = indexer.GetValue(CurrentTheme, new object[] { vsColorName });
            if (colorEntry == null) return null;
            return new VsColorEntry(colorEntry, colorName);
        }
    }

}