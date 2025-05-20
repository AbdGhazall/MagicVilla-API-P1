namespace MagicVilla_VillaAPI.Models.DTO
{
    // This class represents a Data Transfer Object (DTO) for user information (Identity)
    public class UserDTO
    {
        public string Id { get; set; } // unique identifier for the user
        public string UserName { get; set; } // username of the user
        public string Name { get; set; } // name of the user
    }
}