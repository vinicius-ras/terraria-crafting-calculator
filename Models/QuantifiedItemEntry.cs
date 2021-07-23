namespace TerrariaCraftingCalculator.Models
{
    /// <summary>Indicates an <see cref="ItemEntry"/> and the a quantity associated to that item.</summary>
    public class QuantifiedItemEntry
    {
        public ItemEntry Item { get; set; }
        public int Quantity { get; set; }
    }
}
