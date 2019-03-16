using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    //DTOs Notes
    //I don't need to specify anything for the dto to be used because im using an [APICONTROLLER]
    //It will understand the right args by itself (Y)
    // => **if im not using [apicontroller] the actionresult will not know where to get stuff from
    //therefor i will get a null reference exception
    //and in this case i must use the [frombody] and also if(!Modelstate.IsValid)

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _config = config;
            //To prevent a direct access to the db from outside
            _repo = repo;
        }

        //I'm still confused on why do i have to give a name to the http requests?
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserToRegisterDto userToRegisterDto)
        {
            userToRegisterDto.Name = userToRegisterDto.Name.ToLower(); //because usernames should be unique, and else the Jack is different than jack, get it?

            //logic: since UserExists returns false by default, if its true to be false, return bad request :)
            if (await _repo.UserExists(userToRegisterDto.Name))
                return BadRequest("username already exists");

            //next, the register method expects a user obj and a string password
            var userToCreate = new User
            {
                //by mistake i called it Name instead of Username in the database
                Name = userToRegisterDto.Name
            };

            //passing the user obj and the string password
            var createdUser = await _repo.Register(userToCreate, userToRegisterDto.Password);

            return StatusCode(201);
        }


        //Token note: i use the id and name as claims because i mustn't store secret info like a password in it
        // BECAUSE IT WILL BE SENT TO THE CLIENT
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserToLoginDto userToLoginDto)
        {
            //using to lower because im storing name in lower case in the database
            var userFromRepo = await _repo.Login(userToLoginDto.Name.ToLower(), userToLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();

            //NOW start building the Token

            //claims are like when saying employees only, so you check for their badges. (still not sure)
            //so i think here im making sure the user has an id and a name
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Name)
            };

            //To make sure the right token is sent i will use a key as a part of the signing credtials
            var key = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_config.GetSection("AppSettings:Token").Value)); //_config is an obj of IConfiguration to get to appsettings
            //the key is stored in app settings.json which i created

            //To hash the key and store it as creds:
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature); //HmacSha512Signature because Json Web Token

            //I think this is for the body of the token
            var tokenDecriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(2), //so it expires after 2 days from now (i chose this)
                SigningCredentials = creds
            };
            
            //Need this for encryption I guess
            var tokenHandler = new JwtSecurityTokenHandler();

            //Finally
            var token = tokenHandler.CreateToken(tokenDecriptor);

            return Ok(new {
                token = tokenHandler.WriteToken(token) //to return the token as an obj to the client
            });

            //jwt.io to test a token
        }
    }
}