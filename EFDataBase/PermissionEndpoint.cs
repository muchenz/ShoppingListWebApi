using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using InvitationResult = Shared.DataEndpoints.Models.Result<(Shared.DataEndpoints.Models.User, Shared.DataEndpoints.Models.Invitation)>;

namespace EFDataBase;
internal class PermissionEndpoint : IPermissionEndpoint
{

    private readonly ShopingListDBContext _context;
    private readonly IMapper _mapper;
    private readonly UserEndpoint _userEndpoint;

    public PermissionEndpoint(ShopingListDBContext context, IMapper mapper, UserEndpoint userEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _userEndpoint = userEndpoint;
    }

    public async Task<Result<(User InvitedUser, Invitation Invitation)>> InviteUserPermission(int listAggregationId, int permissionLvl, string userName, string senderName, int senderId)
    {

        var user = await _userEndpoint.GetUserByNameAsync(userName);

        if (user == null)
            return InvitationResult.Failure(Error.NotFound("User not exist."));

        var IsUserInvitatedToListAggregation = await _userEndpoint.IsUserInvitatedToListAggregationAsync(user.UserId, listAggregationId);

        if (IsUserInvitatedToListAggregation)
            return InvitationResult.Failure(Error.Conflict("Ivitation is on list"));

        //bbbb = _context.UserListAggregators.AsQueryable().Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();

        var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);

        if (isUserHasListAgregation)
            return InvitationResult.Failure(Error.Conflict("User already has permission."));

        try
        {
            await _userEndpoint.AddInvitationAsync(userName, listAggregationId, permissionLvl, senderName);

        }
        catch (DbUpdateException ex)
        {
            return InvitationResult.Failure(Error.Unexpected("Failed to add invitation. Refresh data and try again."));
        }
        var invitation = new Invitation
        {
            EmailAddress = user.EmailAddress,
            UserId = user.UserId,
            ListAggregatorId = listAggregationId,
            PermissionLevel = permissionLvl,
            SenderName = senderName
        };

        return InvitationResult.Ok((user, invitation));
    }

    public async Task<Result> ChangeUserPermission(int listAggregationId
        , UserPermissionToListAggregation item, int senderId)
    {

        //if (await _userEndpoint.IsUserIsAdminOfListAggregatorAsync(senderId, listAggregationId) is not true)
        //{
        //    return Problem(title: "User has no permission.", statusCode: 403);
        //}

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return InvitationResult.Failure(Error.NotFound("User not exist." ));


        var admins = await _userEndpoint.TryGetTwoAdministratorsOfListAggregationsAsync(listAggregationId);

        if (admins.Count == 1 && user.UserId == admins.First().UserId)
            return InvitationResult.Failure(Error.Conflict("Only one Admin left - not delete." ));

              

        bool isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);


        if (!isUserHasListAggregator)
            return InvitationResult.Failure(Error.NotFound("User permission not found." ));
        
        try
        {
            await _userEndpoint.SetUserPermissionToListAggrAsync(user.UserId, listAggregationId, item.Permission);

        }
        catch (DbUpdateException ex)
        {
            return InvitationResult.Failure(Error.Unexpected("Failed to change permission. Refresh data and try again."));
        }

        return InvitationResult.Ok();
    }

    public async Task<Result> DeleteUserPermission(int listAggregationId
       , UserPermissionToListAggregation item, int senderId)
    {

        //if (await _userEndpoint.IsUserIsAdminOfListAggregatorAsync(senderId, listAggregationId) is not true)
        //{
        //    return Problem(title: "User has no permission.", statusCode: 403);
        //}

        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return InvitationResult.Failure(Error.NotFound("User not exist."));


        var admins = await _userEndpoint.TryGetTwoAdministratorsOfListAggregationsAsync(listAggregationId);

        if (admins.Count == 1 && user.UserId == admins.First().UserId)
            return InvitationResult.Failure(Error.Conflict("Only one Admin left - not delete."));



        bool isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);


        if (!isUserHasListAggregator)
            return InvitationResult.Failure(Error.NotFound("User permission not found."));

        try
        {
            await _userEndpoint.DeleteUserListAggrAscync(user.UserId, listAggregationId);

        }
        catch (DbUpdateException ex)
        {
            return InvitationResult.Failure(Error.Unexpected("Failed to Delete permission. Refresh data and try again."));
        }

        return InvitationResult.Ok();
    }
}