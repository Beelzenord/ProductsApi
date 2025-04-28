namespace ProductsApi.Domain.Entities.Data
{
    public class ProductRaw
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public AttrMap Attributes { get; set; } = null!;
    }

    public class AttrMap : Dictionary<string, string> { }

    public class AttrDef
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public List<AttrValue> Values { get; set; } = new();
    }

    public class AttrValue
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
    }



    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<ResolvedAttr> Attributes { get; set; } = new();
    }

    public class ResolvedAttr
    {
        public string AttributeName { get; set; } = null!;
        public string AttributeCode { get; set; } = null!;
        public List<string> SelectedValues { get; set; }
    }
}
