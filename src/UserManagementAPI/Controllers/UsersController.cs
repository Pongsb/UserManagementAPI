using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(UserStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<User[]> GetAll() => Ok(store.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<User> GetById(int id)
    {
        var user = store.GetById(id);
        return user is null
            ? NotFound(new ApiError("User not found."))
            : Ok(user);
    }

    [HttpPost]
    public ActionResult<User> Create([FromBody] UpsertUserRequest request)
    {
        var result = store.Create(request.Name, request.Email);
        if (result.Result == StoreResult.DuplicateEmail)
            return Conflict(new ApiError("A user with this email already exists."));

        return CreatedAtAction(nameof(GetById), new { id = result.User!.Id }, result.User);
    }

    [HttpPut("{id:int}")]
    public ActionResult<User> Update(int id, [FromBody] UpsertUserRequest request)
    {
        var result = store.Update(id, request.Name, request.Email);
        return result.Result switch
        {
            StoreResult.NotFound => NotFound(new ApiError("User not found.")),
            StoreResult.DuplicateEmail =>
                Conflict(new ApiError("A user with this email already exists.")),
            _ => Ok(result.User)
        };
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) => store.Delete(id)
        ? NoContent()
        : NotFound(new ApiError("User not found."));
}
