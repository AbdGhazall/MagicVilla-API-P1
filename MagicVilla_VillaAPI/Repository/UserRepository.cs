using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MagicVilla_VillaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private string secretkey; // access token secret key for JWT token generation
        private readonly UserManager<ApplicationUser> _userManager; // user manager for managing users
        private readonly IMapper _mapper; // for mapping between DTOs and models
        private readonly RoleManager<IdentityRole> _roleManager; // role manager for managing roles

        public UserRepository(ApplicationDbContext db, IConfiguration configuration, UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            secretkey = configuration.GetValue<string>("ApiSettings:Secret"); // get secret key from appsettings.json
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        public bool IsUniqueUser(string userName)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == userName);
            if (user == null)
            {
                return true; // User is unique
            }
            return false; // User is not unique
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = _db.ApplicationUsers
                .FirstOrDefault(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password); // check if password is correct for the user

            if (user == null || isValid == false)
            {
                // User not found or password incorrect
                return new LoginResponseDTO()
                {
                    Token = "",
                    User = null
                };
            }

            #region JWT

            // if user found generate JWT token
            var roles = await _userManager.GetRolesAsync(user); // get all user roles
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretkey); // convert secret key to byte array

            var tokenDiscriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.UserName.ToString()), // provide user id in token as the name
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()) // provide user role in token as the role if I have multiple roles I can use loop here to add all roles
                }),
                Expires = DateTime.UtcNow.AddDays(7), // token expiration time
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) // for signing the token with a specific algorithm
            };
            var token = tokenHandler.CreateToken(tokenDiscriptor);
            LoginResponseDTO loginResponseDTO = new()
            {
                Token = tokenHandler.WriteToken(token), // convert token to string
                User = _mapper.Map<UserDTO>(user), // map user to UserDTO
                //Role = roles.FirstOrDefault() // get user role
            };

            #endregion JWT

            return loginResponseDTO;
        }

        public async Task<UserDTO> Register(RegistirationRequestDTO registirationRequestDTO)
        {
            ApplicationUser user = new() // we can make auoto mapper here and use it
            {
                UserName = registirationRequestDTO.UserName,
                Email = registirationRequestDTO.UserName,
                NormalizedEmail = registirationRequestDTO.UserName.ToUpper(),
                Name = registirationRequestDTO.Name,
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registirationRequestDTO.Password); // create user with password
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync("admin").GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole("admin")); // create role if it does not exist
                        await _roleManager.CreateAsync(new IdentityRole("customer"));
                    }
                    await _userManager.AddToRoleAsync(user, "admin"); // add user to role
                    var userToReturn = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == registirationRequestDTO.UserName);
                    return _mapper.Map<UserDTO>(userToReturn); // map user to UserDTO
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return new UserDTO(); // return empty user if registration failed
        }
    }
}