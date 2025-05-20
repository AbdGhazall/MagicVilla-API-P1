namespace MagicVilla_VillaAPI.Models.DTO
{
    public class LoginResponseDTO
    {
        public UserDTO User { get; set; } // for returning user information
        public string Token { get; set; } // for returning JWT token

    }
}