using System;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    internal class SerializedCoverageWhen
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public string Serialized { get; set; }
        public DateTime When { get; set; }

        public override bool Equals(object obj) 
            => obj is SerializedCoverageWhen when &&
                   this.Serialized == when.Serialized &&
                   this.When == when.When;
    }
    internal interface IDynamicCoverageStore
    {
        SerializedCoverageWhen GetSerializedCoverage(string filePath);
        void RemoveSerializedCoverage(string filePath);
        void SaveSerializedCoverage(string filePath, string serializedCoverage);
    }
}
