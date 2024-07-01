namespace LootMaster
{
    public enum RollOption : uint
    {
        [Display("Need")]
        Need = 1,
        [Display("Greed")]
        Greed = 2,
        [Display("Pass")]
        Pass = 5,
        [Display("Not Available")]
        NotAvailable = 7,
    }
    public enum AutoRollOption : uint
    {
        [Display("Need Then Greed")]
        NeedThenGreed = 0,
        [Display("Need")]
        Need = 1,
        [Display("Greed")]
        Greed = 2,
        [Display("Pass")]
        Pass = 5,
        //[Display("Not Available")]
        //NotAvailable = 7,
    }
    public class Display : System.Attribute
    {
        private readonly string _value;

        public Display(string value)
        {
            _value = value;
        }

        public string Value => _value;
    }
}
