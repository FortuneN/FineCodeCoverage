
using System;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IOrderMetadata
    {
        int Order { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class OrderAttribute : ExportAttribute, IOrderMetadata
    {
        public OrderAttribute(int order, Type contractType)
            : base(contractType)
        {
            Order = order;
        }

        public OrderAttribute(int order, string contractName)
            : base(contractName)
        {
            Order = order;
        }

        public OrderAttribute(int order, string contractName, Type contractType)
            : base(contractName, contractType)
        {
            Order = order;
        }

        public int Order { get;private set;}
    }
}
