namespace TrelloNet
{
	public class CardName : ICardId
	{
		public string Id { get; set; }
        public int IdShort { get; set; }
		public string Name { get; set; }
		public string ShortLink { get; set; }

	    protected bool Equals(CardName other)
	    {
	        return string.Equals(Id, other.Id);
	    }

	    public override bool Equals(object obj)
	    {
	        if (ReferenceEquals(null, obj)) return false;
	        if (ReferenceEquals(this, obj)) return true;
	        if (obj.GetType() != this.GetType()) return false;
	        return Equals((CardName) obj);
	    }

	    public override int GetHashCode()
	    {
	        return Id?.GetHashCode() ?? 0;
	    }

	    public static bool operator ==(CardName left, CardName right)
	    {
	        return Equals(left, right);
	    }

	    public static bool operator !=(CardName left, CardName right)
	    {
	        return !Equals(left, right);
	    }

	    public string GetCardId()
		{
			return Id;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}