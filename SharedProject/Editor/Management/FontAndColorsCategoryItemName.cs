using System;

namespace FineCodeCoverage.Editor.Management
{
    internal struct FontAndColorsCategoryItemName
    {
        public FontAndColorsCategoryItemName(string itemName, Guid category)
        {
            Category = category;
            ItemName = itemName;
        }
        public Guid Category { get; }
        public string ItemName { get; }

    }
}
