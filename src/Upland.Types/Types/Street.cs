namespace Upland.Types.Types
{
    public class Street
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int CityId { get; set; }


        // These are so it works with the upland api
        public int id
        {
            set { Id = value; }
            get { return Id; }
        }

        public string name
        {
            set { Name = value; }
            get { return Name; }
        }

        public string type
        {
            set { Type = value; }
            get { return Type; }
        }

        public int city_id
        {
            set { CityId = value; }
            get { return CityId; }
        }
    }
}
