using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class SerializedStateTests
    {
        [Test]
        public void Works()
        {
            var states = new List<SerializedState>
            {
                new SerializedState(new CodeSpanRange(1,5), ContainingCodeTrackerType.OtherLines, new List<DynamicLine>
                {
                    new DynamicLine(1, DynamicCoverageType.Dirty)
                })
            };

            
            var jsonConvertService = new JsonConvertService();
            var serialized = jsonConvertService.SerializeObject(states);
            var deserialized = jsonConvertService.DeserializeObject<List<SerializedState>>(serialized);
            var state = deserialized[0];
            Assert.That(state.Lines.Count, Is.EqualTo(1));
            Assert.That(state.Lines[0].Number, Is.EqualTo(1));
            Assert.That(state.Lines[0].CoverageType, Is.EqualTo(DynamicCoverageType.Dirty));
            Assert.That(state.CodeSpanRange.Equals(new CodeSpanRange(1, 5)), Is.True);
            Assert.That(state.Type, Is.EqualTo(ContainingCodeTrackerType.OtherLines));

        }
    }
}
