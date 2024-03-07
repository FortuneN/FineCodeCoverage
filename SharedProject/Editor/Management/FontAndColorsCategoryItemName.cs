using System;

namespace FineCodeCoverage.Editor.Management
{
    internal readonly struct FontAndColorsCategoryItemName
    {
        public FontAndColorsCategoryItemName(string itemName, Guid category)
        {
            this.Category = category;
            this.ItemName = itemName;
        }
        public Guid Category { get; }
        public string ItemName { get; }
    }
}
