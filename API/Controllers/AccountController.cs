

using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseAPIController
    {
       private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context, ITokenService tokenService) {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("login")] //api/account/register
        public async Task<ActionResult<UserDTO>>Logon(LoginDTO logindto){
             var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == logindto.Username.ToLower() );
            if(user==null) return Unauthorized("Invalid User Name");
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computeHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(logindto.Password));
            for(int i = 0; i < computeHash.Length; i++) {
                if(computeHash[i] != user.PasswordHash[i])
                   return Unauthorized("Invalid Password");
            }
             return new UserDTO{
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("register")] //api/account/register
        public async Task<ActionResult<UserDTO>>Register(RegisterDTO registerdto){
            if( await UserExists(registerdto.Username)==true) return new UserDTO();
            using var hmac = new HMACSHA512();
            var user = new AppUser{
               UserName = registerdto.Username.ToLower(),
               PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerdto.Password)),
               PasswordSalt = hmac.Key
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDTO{
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };

        }

    
        private async Task<bool> UserExists(string username){
            return await _context.Users.AnyAsync(x=>x.UserName.Equals(username.ToLower()));
        }
    
    }
} 