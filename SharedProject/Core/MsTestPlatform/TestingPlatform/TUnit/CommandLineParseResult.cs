using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    /*
        adjusted from
        https://github.com/microsoft/testfx/blob/main/src/Platform/Microsoft.Testing.Platform/CommandLine/ParseResult.cs
    */
    internal class CommandLineParseResult
    {
        public CommandLineParseResult(
            IReadOnlyList<CommandLineParseOption> options,
            IReadOnlyList<string> errors)
        {
            Options = options;
            Errors = errors;
        }

        public static CommandLineParseResult Empty { get; } = new CommandLineParseResult(Enumerable.Empty<CommandLineParseOption>().ToList(),Enumerable.Empty<string>().ToList());

        public const char OptionPrefix = '-';

        public IReadOnlyList<CommandLineParseOption> Options { get; }
        public IReadOnlyList<string> Errors { get; }

        public bool HasError => Errors.Count > 0;

        public bool IsOptionSet(string optionName)
            => Options.Any(o => o.Name.Equals(optionName.Trim(OptionPrefix), StringComparison.OrdinalIgnoreCase));

        public bool TryGetOptionArgumentList(string optionName, out string[] arguments)
        {
            optionName = optionName.Trim(OptionPrefix);
            IEnumerable<CommandLineParseOption> result = Options.Where(x => x.Name == optionName);
            if (result.Any())
            {
                arguments = result.SelectMany(x => x.Arguments).ToArray();
                return true;
            }

            arguments = null;
            return false;
        }
    }
}
