﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ISourceFileOpener))]
    internal class SourceFileOpener : ISourceFileOpener
    {
        private readonly ICoberturaUtil coberturaUtil;
        private readonly IMessageBox messageBox;
        private readonly ILogger logger;
        private readonly AsyncLazy<DTE2> lazyDTE2;

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
            lazyDTE2 = new AsyncLazy<DTE2>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (DTE2)serviceProvider.GetService(typeof(DTE));
            }, ThreadHelper.JoinableTaskFactory);
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
            var dte = await lazyDTE2.GetValueAsync();
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