using AutoMapper;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase;
internal class PermissionEndpoint
{

    private readonly IMapper _mapper;
    private readonly ILogger<UserEndpointFD> _logger;
    FirestoreDb Db;

    CollectionReference _listAggrCol;
    CollectionReference _listCol;
    CollectionReference _listItemCol;
    CollectionReference _invitationsCol;
    CollectionReference _userListAggrCol;
    CollectionReference _usersCol;
    CollectionReference _indexesCol;
    CollectionReference _refreshToken;
    CollectionReference _toDelete;

    public PermissionEndpoint(IMapper mapper, ILogger<UserEndpointFD> logger)
    {
        logger.LogInformation($"constructor UserEndpointFD ");

        Db = FirestoreDb.Create("testnosqldb1");
        _mapper = mapper;
        _logger = logger;
        _listAggrCol = Db.Collection("listAggregator");
        _listCol = Db.Collection("list");
        _listItemCol = Db.Collection("listItem");
        _invitationsCol = Db.Collection("invitations");
        _userListAggrCol = Db.Collection("userListAggregator");
        _usersCol = Db.Collection("users");
        _indexesCol = Db.Collection("indexes");
        _refreshToken = Db.Collection("refreshTokens");
        _toDelete = Db.Collection("toDelete");
    }


    public async Task<MessageAndStatus> InviteUserPermission(int listAggregationId,
                UserPermissionToListAggregation item,  string signalRId)
    {
        MessageAndStatusAndData.


        return MessageAndStatus.
    }




    private async Task<User> GetUserByNameAsync(string userName)
    {
        var snapUserFD = await _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName).GetSnapshotAsync();

        if (snapUserFD.Count == 0) return null;

        var userFD = snapUserFD.First().ConvertTo<UserFD>();

        return _mapper.Map<User>(userFD);
    }
    private async Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId)
    {
        var userListAggrSnap = await _invitationsCol.WhereEqualTo(nameof(InvitationFD.EmailAddress), userName)
           .WhereEqualTo(nameof(InvitationFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();

        return userListAggrSnap.Documents.Any();
    }

    private async Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId)
    {
        var userListAggrSnap = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
           .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregatorId).GetSnapshotAsync();

        return userListAggrSnap.Documents.Count > 0;
    }
    public async Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName)
    {
        var invitationFD = new InvitationFD
        {
            EmailAddress = toUserName,
            ListAggregatorId = listAggregationId,
            PermissionLevel = permission,
            SenderName = fromSenderName
        };

        await Db.RunTransactionAsync(async transation =>
        {


            var indexRef = _indexesCol.Document("indexes");
            var indesSnap = await transation.GetSnapshotAsync(indexRef);
            var index = indesSnap.GetValue<long>("invitations");
            var indexNew = index + 1;


            var newDocRef = _invitationsCol.Document((indexNew).ToString());

            invitationFD.InvitationId = (int)indexNew;
            transation.Set(newDocRef, invitationFD);

            transation.Update(indexRef, "invitations", indexNew);


        });

    }
}


//public async Task<ActionResult> InviteUserPermission(int listAggregationId,
//           [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
//   {

//       var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

//       if (user == null)
//           return NotFound(new ProblemDetails { Title = "User not exist." });

//       var IsUserInvitatedToListAggregation = await _userEndpoint.IsUserInvitatedToListAggregationAsync(item.User.EmailAddress, listAggregationId);

//       if (IsUserInvitatedToListAggregation)
//           return Conflict(new ProblemDetails { Title = "Ivitation is on list" });

//       //bbbb = _context.UserListAggregators.AsQueryable().Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();

//       var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);

//       if (isUserHasListAgregation)
//           return Conflict(new ProblemDetails { Title = "User already has permission." });

//       var senderName = HttpContext.User.Identity.Name;

//       await _userEndpoint.AddInvitationAsync(item.User.EmailAddress, listAggregationId, item.Permission, senderName);


//       await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { user.UserId },
//           SiganalREventName.InvitationAreChanged, signalRId: signalRId);

//       return Ok("Ivitation was added.");
//   }