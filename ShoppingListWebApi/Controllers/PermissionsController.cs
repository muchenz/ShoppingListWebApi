using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceMediatR.SignalREvents;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Auth.Api;
using ShoppingListWebApi.Data;
using SignalRService;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShoppingListWebApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PermissionsController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly SignarRService _signarRService;
    private readonly IInvitationEndpoint _invitationEndpoint;
    private readonly IUserEndpoint _userEndpoint;
    private readonly IMediator _mediator;

    public PermissionsController(IMapper mapper, SignarRService signarRService, ILogger<PermissionsController> logger
            , IInvitationEndpoint invitationEndpoint, IUserEndpoint userEndpoint, IMediator mediator)
    {
        _mapper = mapper;
        _signarRService = signarRService;
        _invitationEndpoint = invitationEndpoint;
        _userEndpoint = userEndpoint;
        _mediator = mediator;
    }

    [HttpPost("InviteUserPermission")]
    [SecurityLevel(1)]
    public async Task<ActionResult> InviteUserPermission(int listAggregationId,
            [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
    {

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return  NotFound (new ProblemDetails { Title="User not exist." });

        var IsUserInvitatedToListAggregation = await _userEndpoint.IsUserInvitatedToListAggregationAsync(item.User.EmailAddress, listAggregationId);

        if (IsUserInvitatedToListAggregation)
            return Conflict(new ProblemDetails { Title = "Ivitation is on list" });

        //bbbb = _context.UserListAggregators.AsQueryable().Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();

        var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);

        if (isUserHasListAgregation)
            return Conflict(new ProblemDetails { Title = "User already has permission." });

        var senderName = HttpContext.User.Identity.Name;

        await _userEndpoint.AddInvitationAsync(item.User.EmailAddress, listAggregationId, item.Permission, senderName);


        await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { user.UserId }, "New_Invitation", signalRId: signalRId);

        return Ok("Ivitation was added.");
    }

    //[HttpPost("AddUserPermission")]  // not used, 
    //[SecurityLevel(1)]
    // ratcher for aministrator
    public async Task<ActionResult> AddUserPermission(int listAggregationId, [FromBody] UserPermissionToListAggregation item)
    {

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return NotFound(new ProblemDetails { Title = "User not exist." });


        var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);

        if (isUserHasListAgregation)
            return Conflict(new ProblemDetails { Title = "User is on list" });

        await _userEndpoint.AddUserListAggregationAsync(user.UserId, listAggregationId, item.Permission);

        return Ok("User was added." );
    }


    [HttpPost("ChangeUserPermission")]
    [SecurityLevel(1)]
    public async Task<ActionResult> ChangeUserPermission(int listAggregationId
        , [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
    {

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return NotFound(new ProblemDetails { Title = "User not exist." });


        var count = await _userEndpoint.GetNumberOfAdministratorsOfListAggregationsAsync(listAggregationId);

        int lastAdminId = -1;
        if (count == 1)
            lastAdminId = await _userEndpoint.GetLastAdminIdAsync(listAggregationId);

        if (count == 1 && user.UserId == lastAdminId)
            return Conflict(new ProblemDetails { Title = "Only one Admin left - not changed." });


        //var userListAggr = await _context.UserListAggregators.AsQueryable().Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId)
        //    .FirstOrDefaultAsync();

        //if (userListAggr == null)
        //    return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." };
        //else
        //    userListAggr.PermissionLevel = item.Permission;


        ////  _context.Update(userListAggr);
        //await _context.SaveChangesAsync();

        bool isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);


        if (!isUserHasListAggregator)
            return NotFound(new ProblemDetails { Title = "User permission not found." });
        
        await _userEndpoint.SetUserPermissionToListAggrAsync(user.UserId, listAggregationId, item.Permission);

        await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

        return Ok("Permission has changed." );
    }

    [HttpPost("DeleteUserPermission")]
    [SecurityLevel(1)]
    public async Task<ActionResult> DeleteUserPermission(int listAggregationId
        , [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
    {

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return NotFound(new ProblemDetails { Title = "User not exist." });


        var count = await _userEndpoint.GetNumberOfAdministratorsOfListAggregationsAsync(listAggregationId);

        int lastAdminId = -1;
        if (count == 1)
            lastAdminId = await _userEndpoint.GetLastAdminIdAsync(listAggregationId);

        if (count == 1 && user.UserId == lastAdminId)
            return Conflict(new ProblemDetails { Title = "Only one Admin left - not delete." });

        var isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(item.User.UserId, listAggregationId);

        if (!isUserHasListAggregator)
            return NotFound(new ProblemDetails { Title = "User permission not found." });
        
        await _userEndpoint.DeleteUserListAggrAscync(item.User.UserId, listAggregationId);


        await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

        return Ok("User permission was deleted." );
    }


    [Authorize]
    [HttpGet("GetListAggregationForPermission")]
    public async Task<List<ListAggregationWithUsersPermission>> GetListAggregationForPermission()
    {
        var  userName = User.FindFirstValue(ClaimTypes.Name);
        var dataTransfer = await _userEndpoint.GetListAggregationForPermission2Async(userName);


        return dataTransfer;

    }

    [Authorize]
    [HttpPost("GetListAggregationForPermission_Empty")]
    public async Task<ActionResult<MessageAndStatusAndData<List<ListAggregationWithUsersPermission>>>> GetListAggregationForPermission_Empty()
    {

        var sUnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userId = int.Parse(sUnerId);

        var dataTransfer = await _userEndpoint.GetListAggregationForPermission_EmptyAsync(userId);


        return MessageAndStatusAndData<List<ListAggregationWithUsersPermission>>.Ok(dataTransfer);

    }

    // TODO: must be admin on ListAggrId:   [SecurityLevel(1)]
    [Authorize]
    [HttpPost("GetListAggregationForPermissionByListAggrId")] // TODO: only ListAggrId is enough
    public async Task<ActionResult<MessageAndStatusAndData<ListAggregationWithUsersPermission>>>
        GetListAggregationForPermissionByListAggrId([FromBody] ListAggregationWithUsersPermission listAggregationForPermission)
    {

        var data = await _userEndpoint.GetListAggregationForPermissionByListAggrIdAsync(listAggregationForPermission);

        return MessageAndStatusAndData<ListAggregationWithUsersPermission>.Ok(data);

    }
}
