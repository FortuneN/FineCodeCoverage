using Moq;

namespace FineCodeCoverageTests.TestHelpers
{
    internal static class MoqAssertionsHelper
    {
        public static Times ExpectedTimes(bool expected) => expected ? Times.Once() : Times.Never();
    }
}
