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
using Shared.DataEndpoints.Abstaractions;

using InvitationResult = Shared.DataEndpoints.Models.Result<(Shared.DataEndpoints.Models.User, Shared.DataEndpoints.Models.Invitation)>;

namespace FirebaseDatabase;

internal class PermissionEndpointFD : IPermissionEndpoint
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

    public PermissionEndpointFD(IMapper mapper, ILogger<UserEndpointFD> logger)
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


    public async Task<Result<(User InvitedUser, Invitation Invitation)>> InviteUserPermission(int listAggregationId,
                UserPermissionToListAggregation item, string senderName, int senderId)
    {

        InvitationResult messageAndStatus = null;

        User user = null;
        Invitation invitation = null;


        await Db.RunTransactionAsync(async transation =>
        {

            if (await IsUserIsAdminOfListAggregatorAsync(transation, senderId, listAggregationId))
            {
                messageAndStatus = InvitationResult.Failure(Error.Forbidden("Sender has no permission."));
            }

            var userFD = await GetUserByNameAsync(transation, item.User.EmailAddress);

            if (userFD == null)
            {
                messageAndStatus = InvitationResult.Failure(Error.NotFound("User not exist."));
                return;
            }
            user = _mapper.Map<User>(userFD);

            var IsUserInvitatedToListAggregation = await IsUserInvitatedToListAggregationAsync(transation, item.User.EmailAddress, listAggregationId);

            if (IsUserInvitatedToListAggregation)
            {
                messageAndStatus = InvitationResult.Failure(Error.Conflict("Ivitation is on list."));
                return;
            }

            var isUserHasListAgregation = await IsUserHasListAggregatorAsync(transation, user.UserId, listAggregationId);

            if (isUserHasListAgregation)
            {
                messageAndStatus = InvitationResult.Failure(Error.Conflict("User already has permission."));
                return;
            }


            invitation =  await AddInvitationAsync(transation, item.User.EmailAddress, listAggregationId, item.Permission, senderName);

        });



        if (messageAndStatus is null)
        {
            return InvitationResult.Ok((user,invitation));
        }

        return messageAndStatus;
    }



    public async Task<bool> IsUserIsAdminOfListAggregatorAsync(Transaction transaction, int userId, int listAggregatorId)
    {
        var userListAggrQuery = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
           .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregatorId);

        var userListAggrSnap = await transaction.GetSnapshotAsync(userListAggrQuery);


        if (userListAggrSnap.Documents.Count == 0) return false;

        return userListAggrSnap.Documents.First().ConvertTo<UserListAggregatorFD>().PermissionLevel == 1;
    }
    private async Task<UserFD> GetUserByNameAsync(Transaction transaction, string userName)
    {
        var userFDQuery = _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName);
        var userFDSnap = await transaction.GetSnapshotAsync(userFDQuery);

        if (userFDSnap.Count == 0) return null;

        var userFD = userFDSnap.First().ConvertTo<UserFD>();

        return userFD;
    }

    private async Task<bool> IsUserInvitatedToListAggregationAsync(Transaction transaction, string userName, int listAggregationId)
    {
        var userListAggrRef = _invitationsCol.WhereEqualTo(nameof(InvitationFD.EmailAddress), userName)
           .WhereEqualTo(nameof(InvitationFD.ListAggregatorId), listAggregationId).Limit(1);

        var userListAggrSnap = await transaction.GetSnapshotAsync(userListAggrRef);

        return userListAggrSnap.Documents.Any();
    }

    private async Task<bool> IsUserHasListAggregatorAsync(Transaction transaction, int userId, int listAggregatorId)
    {
        var userListAggrRef = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
           .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregatorId).Limit(1);

        var userListAggrSnap = await transaction.GetSnapshotAsync(userListAggrRef);

        return userListAggrSnap.Documents.Count > 0;
    }
    private async Task<Invitation> AddInvitationAsync(Transaction transation, string toUserName, int listAggregationId, int permission, string fromSenderName)
    {
        var invitationFD = new InvitationFD
        {
            EmailAddress = toUserName,
            ListAggregatorId = listAggregationId,
            PermissionLevel = permission,
            SenderName = fromSenderName
        };




        var indexRef = _indexesCol.Document("indexes");
        var indesSnap = await transation.GetSnapshotAsync(indexRef);
        var index = indesSnap.GetValue<long>("invitations");
        var indexNew = index + 1;


        var newDocRef = _invitationsCol.Document((indexNew).ToString());

        invitationFD.InvitationId = (int)indexNew;
        transation.Set(newDocRef, invitationFD);

        transation.Update(indexRef, "invitations", indexNew);


        return _mapper.Map<Invitation>(invitationFD);
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