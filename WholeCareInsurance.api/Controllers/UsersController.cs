using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholeCareInsurance.api.Services;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _usersService.GetAll());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _usersService.GetById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var email = User.Identity!.Name!;
            var user = await _usersService.GetByEmail(email);
            return Ok(user);
        }
    }
}
