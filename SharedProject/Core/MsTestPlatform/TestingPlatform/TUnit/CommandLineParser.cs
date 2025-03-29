using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal interface ICommandLineParser
    {
        CommandLineParseResult Parse(string argumentsString);
    }

    /*
        Adjusted from
        https://github.com/microsoft/testfx/blob/main/src/Platform/Microsoft.Testing.Platform/CommandLine/Parser.cs
        without escaping
    */
    [Export(typeof(ICommandLineParser))]
    internal class CommandLineParser : ICommandLineParser
    {
        public CommandLineParseResult Parse(string argumentsString)
        {
            var args = CommandLineStringSplitter.Instance.Split(argumentsString).ToList();
            if (!args.Any())
            {
                return CommandLineParseResult.Empty;
            }
            return Parse(args);
        }

        public CommandLineParseResult Parse(string[] args)
            => Parse(args.ToList());

        private CommandLineParseResult Parse(List<string> args)
        {
            List<CommandLineParseOption> options = new List<CommandLineParseOption>();
            List<string> errors = new List<string>();

            string currentOption = null;
            string currentArg = null;
            string toolName = null;
            List<string> currentOptionArguments = new List<string>();
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].StartsWith("@") && ResponseFileHelper.TryReadResponseFile(args[i].Substring(1), errors, out string[] newArguments))
                {
                    args.InsertRange(i + 1, newArguments);
                    continue;
                }
                bool argumentHandled = false;
                currentArg = args[i];

                while (!argumentHandled)
                {
                    if (currentArg is null)
                    {
                        errors.Add($"UnexpectedNullArgument {i}");
                        break;
                    }

                    // we accept as start for options -- and - all the rest are arguments to the previous option
                    if ((args[i].Length > 1 && currentArg[0].Equals('-') && !currentArg[1].Equals('-')) ||
                        (args[i].Length > 2 && currentArg[0].Equals('-') && currentArg[1].Equals('-') && !currentArg[2].Equals('-')))
                    {
                        if (currentOption is null)
                        {
                            ParseOptionAndSeparators(args[i], out currentOption, out currentArg);
                            argumentHandled = currentArg is null;
                        }
                        else
                        {
                            options.Add(new CommandLineParseOption(currentOption, currentOptionArguments.ToArray()));
                            currentOptionArguments.Clear();
                            ParseOptionAndSeparators(args[i], out currentOption, out currentArg);
                            argumentHandled = true;
                        }
                    }
                    else
                    {
                        // If it's the first argument and it doesn't start with - then it's the tool name
                        if (i == 0 && !args[0][0].Equals('-'))
                        {
                            toolName = currentArg;
                        }
                        else if (currentOption is null)
                        {
                            errors.Add($"UnexpectedArgument {args[i]}");
                        }
                        else
                        {
                            currentOptionArguments.Add(currentArg.Trim());
                            currentArg = null;
                        }

                        argumentHandled = true;
                    }
                }
            }

            if (currentOption != null)
            {
                if (currentArg != null)
                {
                    currentOptionArguments.Add(currentArg.Trim());
                }

                options.Add(new CommandLineParseOption(currentOption, currentOptionArguments.ToArray()));
            }

            return new CommandLineParseResult(options, errors);
        }

        private static void ParseOptionAndSeparators(string arg, out string currentOption, out string currentArg)
        {
            var delimiterIndex = arg.IndexOfAny(new char[] { ':', '=', ' ' });
            if (delimiterIndex == -1)
            {
                currentOption = arg;
                currentArg = null;
            }
            else
            {
                currentOption = arg.Substring(0, delimiterIndex);
                currentArg = arg.Substring(++delimiterIndex);
            }
            currentOption = currentOption.TrimStart('-');
        }
    }
}
