using AutoMapper;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvitationResult = Shared.DataEndpoints.Models.MessageAndStatusAndData<(Shared.DataEndpoints.Models.User, Shared.DataEndpoints.Models.Invitation)>;

namespace EFDataBase;
internal class PermissionEndpoint : IPermissionEndpoint
{

    private readonly ShopingListDBContext _context;
    private readonly IMapper _mapper;
    private readonly IUserEndpoint _userEndpoint;

    public PermissionEndpoint(ShopingListDBContext context, IMapper mapper, IUserEndpoint userEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _userEndpoint = userEndpoint;
    }

    public async Task<MessageAndStatusAndData<(User InvitedUser, Invitation Invitation)>> InviteUserPermission(int listAggregationId, UserPermissionToListAggregation item, string senderName, int senderId)
    {
      
        var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

        if (user == null)
            return InvitationResult.NotFound( "User not exist." );

        var IsUserInvitatedToListAggregation = await _userEndpoint.IsUserInvitatedToListAggregationAsync(item.User.EmailAddress, listAggregationId);

        if (IsUserInvitatedToListAggregation)
            return InvitationResult.Conflict("Ivitation is on list" );

        //bbbb = _context.UserListAggregators.AsQueryable().Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();

        var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);

        if (isUserHasListAgregation)
            return InvitationResult.Conflict("User already has permission." );


        await _userEndpoint.AddInvitationAsync(item.User.EmailAddress, listAggregationId, item.Permission, senderName);

        var invitation = new Invitation
        {
            EmailAddress = item.User.EmailAddress,
            ListAggregatorId = listAggregationId,
            PermissionLevel = item.Permission,
            SenderName = senderName
        };

        return InvitationResult.Ok((user, invitation),"Ivitation was added.");
    }

}