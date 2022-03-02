using System;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using FineCodeCoverage.Engine.Cobertura;
using Microsoft;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ISourceFileOpener))]
    internal class SourceFileOpener : ISourceFileOpener
    {
        private readonly ICoberturaUtil coberturaUtil;
        private readonly IMessageBox messageBox;
        private readonly ILogger logger;
        private readonly DTE2 dte;

        [ImportingConstructor]
        public SourceFileOpener(
            ICoberturaUtil coberturaUtil,
            IMessageBox messageBox,ILogger logger, 
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
        {
            this.coberturaUtil = coberturaUtil;
            this.messageBox = messageBox;
            this.logger = logger;
            ThreadHelper.ThrowIfNotOnUIThread();
            dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            Assumes.Present(dte);
        }
        public async System.Threading.Tasks.Task OpenFileAsync(string assemblyName, string qualifiedClassName, int file, int line)
        {
            // Note : There may be more than one file; e.g. in the case of partial classes
            //remove CoverageReport
            var sourceFiles = coberturaUtil.GetSourceFiles(assemblyName, qualifiedClassName,file);

            if (!sourceFiles.Any())
            {
                var message = $"Source File(s) Not Found : [{ assemblyName }]{ qualifiedClassName }";
                logger.Log(message);
                messageBox.Show(message);
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            dte.MainWindow.Activate();

            foreach (var sourceFile in sourceFiles)
            {
                dte.ItemOperations.OpenFile(sourceFile, Constants.vsViewKindCode);

                if (line != 0)
                {
                    ((TextSelection)dte.ActiveDocument.Selection).GotoLine(line, false);
                }
            }

        }
    }

}