using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MyCodeCamp.wwwroot
{
    public class AuthController : Controller
    {
        private readonly CampContext _context;
        private readonly SignInManager<CampUser> _signMgr;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<CampUser> _userManager;
        private readonly IPasswordHasher<CampUser> _hasher;
        private readonly IConfigurationRoot _configurationRoot;

        public AuthController(CampContext context,
                            SignInManager<CampUser> signInMgr,
                            ILogger<AuthController> logger,
                            UserManager<CampUser> userManager,
                            IPasswordHasher<CampUser> hasher,
                            IConfigurationRoot configurationRoot)
        {
            _context = context;
            _signMgr = signInMgr;
            _logger = logger;
            _userManager = userManager;
            _hasher = hasher;
            _configurationRoot = configurationRoot;
        }
        [HttpPost("api/Auth/login")]
        [ValidateModel]
        public async Task<IActionResult> Login([FromBody] CredentialModel model)
        {
            try
            {
                //CampUser user = new CampUser() { UserName = model.UserName };
                var result = await _signMgr.PasswordSignInAsync(model.UserName, model.Password, true, false);
                if (result.Succeeded)
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An excepion thrown while login:{ex}");
            }
            return BadRequest("Failed to login");
        }
        [ValidateModel]
        [HttpPost("api/auth/token")]
        public async Task<IActionResult> CreatedToken([FromBody] CredentialModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    if (_hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Success)
                    {
                        var userClaims = await _userManager.GetClaimsAsync(user);
                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                            new Claim(JwtRegisteredClaimNames.Email, user.Email)
                        }.Union(userClaims);
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configurationRoot["Tokens:Key"].ToString()));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken(
                                issuer: _configurationRoot["Tokens:Issuer"],
                                audience: _configurationRoot["Tokens:Audience"],
                                claims: claims,
                                expires: DateTime.UtcNow.AddMinutes(15),
                                signingCredentials: creds);
                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An excepion thrown while Creating JWT:{ex}");
            }
            return BadRequest("Failed to login");

        }
    }
}
