using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        //in this class the methods of IAuthRepository interface are implemented

        private readonly DataContext _context;

        public AuthRepository(DataContext context)
        {
            //This is to prevent a direct access to the db from outside
            _context = context;
        }

        public async Task<User> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Name == username);

            if(user == null)
                return null;

            //ValidatePassHash is a method of my creation and will be implemented outside
            //will compare the user obj passHash and Salt with the entered string pass
            if(!ValidatePasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }

        //implementing the method from Login
        //btw the two byte[] args are the ones of the user obj from above
        private bool ValidatePasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            //i didn'e quiet get it but here the passSalt is passed to hmac to be used with the password
            using(var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt)) 
            {
                //so here i compute the hash for the entered password from the user
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                //then here i compare it with the saved computed hash from the register
                for(int i = 0; i < computedHash.Length; i++)
                {
                    if(computedHash[i] != passwordHash[i]) 
                        return false;
                }
            }

            //basically, if everything is fine return true
            return true;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            //the out keyword allows the reference instead of just args
            //so when the values of the passHash and passSalt are updated in the method, theyll be also here
            CreatePasswwordHash(password, out passwordHash, out passwordSalt); //methods implemented outside

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        //implementing the method from the Register
        private void CreatePasswwordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            //used the using keyword so everything within the scope gets disposed (**what is disposal??)
            using(var hmac = new System.Security.Cryptography.HMACSHA512()) {
                passwordSalt = hmac.Key; //the key will be added to the hash in the login method(i guess)
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                //System.Text.Encoding.UTF8.GetBytes(password) converts the pass to a byte[]
            }
        }

        public async Task<bool> UserExists(string username)
        {
            //logic: if you find Any username with the same one entered by the user return true
            //btw, i named it Name instead of Username by mistake in the database
            if(await _context.Users.AnyAsync(x => x.Name == username))
                return true;
            
            //else, return false
            return false;
        }
    }
}