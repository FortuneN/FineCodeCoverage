using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(IMessageBox))]
    [ExcludeFromCodeCoverage]
    internal class MessageBoxWrapper : IMessageBox
    {
        public void Show(string message)
        {
            MessageBox.Show(message);
        }
    }

}