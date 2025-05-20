namespace MagicVilla_VillaAPI.Models
{
    //database
    public class LocalUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // Admin, User (will be stored here in localUser table)
    }
}