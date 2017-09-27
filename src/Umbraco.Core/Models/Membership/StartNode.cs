namespace Umbraco.Core.Models.Membership
{
    public class StartNode
    {
        public StartNode(int id)
        {
            Id = id;
        }

        public StartNode(int id, string label) 
            : this(id)
        {
            Label = label;
        }

        public int Id { get; set; }

        public string Label { get; set; }
    }
}
