using System;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
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
        private readonly IServiceProvider serviceProvider;

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
            this.serviceProvider = serviceProvider;
        }
        public async System.Threading.Tasks.Task OpenFileAsync(string assemblyName, string qualifiedClassName, int file, int line)
        {
            // Note : There may be more than one file; e.g. in the case of partial classes
            //remove CoverageReport
            var sourceFiles = coberturaUtil.GetSourceFiles(assemblyName, qualifiedClassName);

            if (!sourceFiles.Any())
            {
                var message = $"Source File(s) Not Found : [{ assemblyName }]{ qualifiedClassName }";
                logger.Log(message);
                messageBox.Show(message);
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var Dte = (DTE)serviceProvider.GetService(typeof(DTE));
            Assumes.Present(Dte);
            Dte.MainWindow.Activate();

            foreach (var sourceFile in sourceFiles)
            {
                Dte.ItemOperations.OpenFile(sourceFile, Constants.vsViewKindCode);

                if (line != 0)
                {
                    ((TextSelection)Dte.ActiveDocument.Selection).GotoLine(line, false);
                }
            }

        }
    }

}