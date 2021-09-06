using AutoMapper;
using Google.Cloud.Firestore;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase
{
    public class InvitationEndpointFD : IInvitationEndpoint
    {

        private readonly IMapper _mapper;
        FirestoreDb Db;

        CollectionReference _listAggrCol;
        CollectionReference _listCol;
        CollectionReference _listItemCol;
        CollectionReference _invitationsCol;
        CollectionReference _userListAggrCol;
        CollectionReference _usersCol;
        CollectionReference _indexesCol;

        public InvitationEndpointFD(IMapper mapper)
        {
            
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;


            _listAggrCol = Db.Collection("listAggregator");
            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");
            _invitationsCol = Db.Collection("invitations");
            _userListAggrCol = Db.Collection("userListAggregator");
            _usersCol = Db.Collection("users");
            _indexesCol = Db.Collection("indexes");

        }

       

        public async Task<List<Invitation>> GetInvitationsListAsync(string userName)
        {
            var querryInvitationSnap = await _invitationsCol.WhereEqualTo(nameof(InvitationFD.EmailAddress), userName)
                .GetSnapshotAsync();

            List<InvitationFD> listInvFD = querryInvitationSnap.Documents.Select(a => a.ConvertTo<InvitationFD>()).ToList();


            var invitationList = new List<Invitation>();

            foreach (var itemInvFD in listInvFD)
            {

                var snapDoc =  await _listAggrCol.Document(itemInvFD.ListAggregatorId.ToString()).GetSnapshotAsync();

                if (!snapDoc.Exists)
                { 
                    await _invitationsCol.Document(itemInvFD.InvitationId.ToString()).DeleteAsync();
                }
                else
                {
                    var tempInvitation = _mapper.Map<Invitation>(itemInvFD);
                    tempInvitation.ListAggregatorName = snapDoc.ConvertTo<ListAggregatorFD>().ListAggregatorName;

                    invitationList.Add(tempInvitation);
                }
            }

            return invitationList;
        }

        public async Task RejectInvitaionAsync(Invitation invitation)
        {
            await _invitationsCol.Document(invitation.InvitationId.ToString()).DeleteAsync();
        }

        public async Task AcceptInvitationAsync(Invitation invitation, int userId)
        {
            await Db.RunTransactionAsync(async transation =>
            {
                var invitationEntity = _mapper.Map<InvitationFD>(invitation);

                await transation.Database.Collection("invitations").Document(invitation.InvitationId.ToString()).DeleteAsync();

                var userListAggregatorFD = new UserListAggregatorFD
                {
                    ListAggregatorId = invitation.ListAggregatorId,
                    UserId = userId,
                    PermissionLevel = invitation.PermissionLevel
                };


                await transation.Database.Collection("userListAggregator").AddAsync(userListAggregatorFD);

            });
        }
    }
}
