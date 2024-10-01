namespace momiji3.Models
{
    public class RollResult
    {
        public int StarRating { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
    }

    public class UserRollData
    {
        public ulong UserId { get; set; }
        public List<UserRollEntry> RollEntries { get; set; } = new List<UserRollEntry>();
    }

    public class UserRollEntry
    {
        public int Id { get; set; }
        public int Rarity { get; set; }
        public string ImageUrl { get; set; }
        public string RollName { get; set; }
        public Element Element { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public string OwnerId { get; set; }
    }

    public enum Element
    {
        Water,
        Fire,
        Grass
    }
}
