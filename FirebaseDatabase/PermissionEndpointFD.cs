using AutoMapper;
using EFDataBase;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
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
                int permissionLvl, string userName, string senderName, int senderId)
    {

        InvitationResult messageAndStatus = null;

        User user = null;
        Invitation invitation = null;


        await Db.RunTransactionAsync(async transation =>
        {

            //if (!await IsUserIsAdminOfListAggregatorAsync(transation, senderId, listAggregationId))
            //{
            //    messageAndStatus = InvitationResult.Failure(Error.Forbidden("Sender has no permission."));
            //    return;
            //}

            user = await GetUserByNameAsync(transation, userName);

            if (user == null)
            {
                messageAndStatus = InvitationResult.Failure(Error.NotFound("User not exist."));
                return;
            }

            var IsUserInvitatedToListAggregation = await IsUserInvitatedToListAggregationAsync(transation, userName, listAggregationId);

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


            invitation =  await AddInvitationAsync(transation, user, listAggregationId, permissionLvl, senderName);

        });



        if (messageAndStatus is null)
        {
            return InvitationResult.Ok((user,invitation));
        }

        return messageAndStatus;
    }



    //public async Task<bool> IsUserIsAdminOfListAggregatorAsync(Transaction transaction, int userId, int listAggregatorId)
    //{
    //    var userListAggrQuery = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
    //       .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregatorId);

    //    var userListAggrSnap = await transaction.GetSnapshotAsync(userListAggrQuery);


    //    if (userListAggrSnap.Documents.Count == 0) return false;

    //    var userListAggr = userListAggrSnap.Documents.First().ConvertTo<UserListAggregatorFD>();

    //    return userListAggrSnap.Documents.First().ConvertTo<UserListAggregatorFD>().PermissionLevel == 1;
    //}
    private async Task<User> GetUserByNameAsync(Transaction transaction, string userName)
    {
        var userFDQuery = _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName);
        var userFDSnap = await transaction.GetSnapshotAsync(userFDQuery);

        if (userFDSnap.Count == 0) return null;

        var userFD = userFDSnap.First().ConvertTo<UserFD>();
        var userTemp = _mapper.Map<User>(userFD);
        return userTemp;
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

    public async Task<List<UserListAggregator>> TryGetTwoAdministratorsOfListAggregationsAsync(Transaction transaction, 
        int listAggregationId)
    {
        var querryRef = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId)
           .WhereEqualTo(nameof(UserListAggregatorFD.PermissionLevel), 1).Limit(2);

        var querrySnapshot = await transaction.GetSnapshotAsync(querryRef);

        var admins = querrySnapshot.Select(a => a.ConvertTo<UserListAggregatorFD>()).ToList();
        return _mapper.Map<List<UserListAggregator>>(admins);

    }

    public async Task SetUserPermissionToListAggrAsync(Transaction transation,  int userId, int listAggregationId, int permission)
    {

        var userListAggrRef = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
           .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId).Limit(1);

        var userListAggrSnap = await transation.GetSnapshotAsync(userListAggrRef);


        transation.Update(userListAggrSnap.First().Reference, nameof(UserListAggregatorFD.PermissionLevel), permission);
    }

    private async Task<Invitation> AddInvitationAsync(Transaction transation, User user, int listAggregationId, int permission, string fromSenderName)
    {
        var invitationFD = new InvitationFD
        {
            EmailAddress = user.EmailAddress,
            UserId = user.UserId,
            ListAggregatorId = listAggregationId,
            PermissionLevel = permission,
            SenderName = fromSenderName
        };

        var listAggRef= _listAggrCol.Document(listAggregationId.ToString());
        var listAggSnap =  await transation.GetSnapshotAsync(listAggRef);


        var indexRef = _indexesCol.Document("indexes");
        var indesSnap = await transation.GetSnapshotAsync(indexRef);
        var index = indesSnap.GetValue<long>("invitations");
        var indexNew = index + 1;


        var newDocRef = _invitationsCol.Document((indexNew).ToString());

        invitationFD.InvitationId = (int)indexNew;
        transation.Set(newDocRef, invitationFD);

        transation.Update(indexRef, "invitations", indexNew);

        var invMapped = _mapper.Map<Invitation>(invitationFD);
        invMapped.ListAggregatorName = listAggSnap.ConvertTo<ListAggregatorFD>().ListAggregatorName;

        return invMapped;
    }
    


    public async Task<Result> ChangeUserPermission(int listAggregationId,
               UserPermissionToListAggregation item, int senderId)
    {


        InvitationResult messageAndStatus = null;


        await Db.RunTransactionAsync(async transation =>
        {

            //if (!await IsUserIsAdminOfListAggregatorAsync(transation, senderId, listAggregationId))
            //{
            //    messageAndStatus = InvitationResult.Failure(Error.Forbidden("Sender has no permission."));
            //    return;
            //}

            var user = await GetUserByNameAsync(transation, item.User.EmailAddress);

            if (user == null)
            {
                messageAndStatus = InvitationResult.Failure(Error.NotFound("User not exist."));
                return;
            }

            var admins = await TryGetTwoAdministratorsOfListAggregationsAsync(transation, listAggregationId);

            if (admins.Count == 1 && user.UserId == admins.First().UserId)
            {
                messageAndStatus= InvitationResult.Failure(Error.Conflict("Only one Admin left - not delete." ));
                return;
            }

            var isUserHasListAgregation = await IsUserHasListAggregatorAsync(transation, user.UserId, listAggregationId);

            if (!isUserHasListAgregation)
            {
                messageAndStatus = InvitationResult.Failure(Error.Conflict("User permission not found."));
                return;
            }

            await SetUserPermissionToListAggrAsync(transation, user.UserId, listAggregationId, item.Permission);


        });


        if (messageAndStatus is null)
        {
            return InvitationResult.Ok();
        }

        return messageAndStatus;
    }

    //[HttpPost("ChangeUserPermission")]
    //[SecurityLevel(1)]
    //public async Task<ActionResult> ChangeUserPermission(int listAggregationId
    //    , [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
    //{
    //    var senderId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

    //    if (await _userEndpoint.IsUserIsAdminOfListAggregatorAsync(senderId, listAggregationId) is not true)
    //    {
    //        return Problem(title: "User has no permission.", statusCode: 403);
    //    }

    //    var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

    //    if (user == null)
    //        return NotFound(new ProblemDetails { Title = "User not exist." });


    //    var admins = await _userEndpoint.TryGetTwoAdministratorsOfListAggregationsAsync(listAggregationId);

    //    if (admins.Count == 1 && user.UserId == admins.First().UserId)
    //        return Conflict(new ProblemDetails { Title = "Only one Admin left - not delete." });


    //    //var userListAggr = await _context.UserListAggregators.AsQueryable().Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId)
    //    //    .FirstOrDefaultAsync();

    //    //if (userListAggr == null)
    //    //    return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." };
    //    //else
    //    //    userListAggr.PermissionLevel = item.Permission;


    //    ////  _context.Update(userListAggr);
    //    //await _context.SaveChangesAsync();

    //    bool isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);


    //    if (!isUserHasListAggregator)
    //        return NotFound(new ProblemDetails { Title = "User permission not found." });

    //    await _userEndpoint.SetUserPermissionToListAggrAsync(user.UserId, listAggregationId, item.Permission);

    //    await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

    //    return Ok("Permission has changed.");
    //}


    public async Task<Result> DeleteUserPermission(int listAggregationId,
             UserPermissionToListAggregation item, int senderId)
    {


        InvitationResult messageAndStatus = null;


        await Db.RunTransactionAsync(async transation =>
        {

            //if (!await IsUserIsAdminOfListAggregatorAsync(transation, senderId, listAggregationId))
            //{
            //    messageAndStatus = InvitationResult.Failure(Error.Forbidden("Sender has no permission."));
            //    return;
            //}

            var user = await GetUserByNameAsync(transation, item.User.EmailAddress);

            if (user == null)
            {
                messageAndStatus = InvitationResult.Failure(Error.NotFound("User not exist."));
                return;
            }

            var admins = await TryGetTwoAdministratorsOfListAggregationsAsync(transation, listAggregationId);

            if (admins.Count == 1 && user.UserId == admins.First().UserId)
            {
                messageAndStatus = InvitationResult.Failure(Error.Conflict("Only one Admin left - not delete."));
                return;
            }

            var isUserHasListAgregation = await IsUserHasListAggregatorAsync(transation, user.UserId, listAggregationId);

            if (!isUserHasListAgregation)
            {
                messageAndStatus = InvitationResult.Failure(Error.Conflict("User permission not found."));
                return;
            }

            await DeleteUserListAggrAscync(transation, user.UserId, listAggregationId);


        });


        if (messageAndStatus is null)
        {
            return InvitationResult.Ok();
        }

        return messageAndStatus;
    }

    public async Task DeleteUserListAggrAscync(Transaction transaction, int userId, int listAggregationId)
    {

        var querrySnapshot = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
            .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();


        var doc = querrySnapshot.FirstOrDefault();
        if (doc is not null)
        {
            transaction.Delete(querrySnapshot.First().Reference);
        }
    }

    //[HttpPost("DeleteUserPermission")]
    //[SecurityLevel(1)]
    //public async Task<ActionResult> DeleteUserPermission(int listAggregationId
    //   , [FromBody] UserPermissionToListAggregation item, [FromHeader] string signalRId)
    //{
    //    var senderId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

    //    if (await _userEndpoint.IsUserIsAdminOfListAggregatorAsync(senderId, listAggregationId) is not true)
    //    {
    //        return Problem(title: "User has no permission.", statusCode: 403);
    //    }

    //    var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

    //    if (user == null)
    //        return NotFound(new ProblemDetails { Title = "User not exist." });


    //    var admins = await _userEndpoint.TryGetTwoAdministratorsOfListAggregationsAsync(listAggregationId);


    //    if (admins.Count == 1 && user.UserId == admins.First().UserId)
    //        return Conflict(new ProblemDetails { Title = "Only one Admin left - not delete." });

    //    var isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(item.User.UserId, listAggregationId);

    //    if (!isUserHasListAggregator)
    //        return NotFound(new ProblemDetails { Title = "User permission not found." });

    //    await _userEndpoint.DeleteUserListAggrAscync(user.UserId, listAggregationId);


    //    await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

    //    return Ok("User permission was deleted.");
    //}


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