using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;
using NUnit.Framework;

namespace FineCodeCoverageTests.TestHelpers
{
    public static class MefOrderAssertions
    {
        private static FineCodeCoverage.Core.Utilities.OrderAttribute GetOrderAtrribute(Type classType)
        {
            return classType.GetTypedCustomAttributes<FineCodeCoverage.Core.Utilities.OrderAttribute>(
                false)[0];
        }
        public static void TypeHasExpectedOrder(Type classType,int expectedOrder)
        {
            Assert.AreEqual(GetOrderAtrribute(classType).Order, expectedOrder);
        }

        public static void InterfaceExportsHaveConsistentOrder(Type interfaceType)
        {
            var types = interfaceType.Assembly.GetTypes();
            var derivations = types.Where(t => t != interfaceType && interfaceType.IsAssignableFrom(t));
            var orders = derivations.Select(d =>
            {
                var orderAttribute = GetOrderAtrribute(d);
                if (orderAttribute == null)
                {
                    throw new Exception("Missing mef attribute");
                }
                if (orderAttribute.ContractType != interfaceType)
                {
                    throw new Exception("Incorrect contract type");
                }
                return orderAttribute.Order;
            }).OrderBy(i => i).ToList();
            Assert.Greater(orders.Count, 0);
            var count = 1;
            foreach(var order in orders)
            {
                Assert.AreEqual(order, count);
                count++;
            }
        }
    }
}
