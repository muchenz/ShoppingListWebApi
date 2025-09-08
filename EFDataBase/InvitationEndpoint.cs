using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;

namespace EFDataBase
{
    public class InvitationEndpoint : IInvitationEndpoint
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public InvitationEndpoint(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

      

        public async Task<List<Invitation>> GetInvitationsListAsync(int userId)
        {

            // ------------- old wersion 
            //var list_Invi_Aggr = from invi in _context.Invitations
            //                     where invi.EmailAddress == userName
            //                     join aggr in _context.ListAggregators on invi.ListAggregatorId equals aggr.ListAggregatorId into invi_aggr
            //                     from aggrOrNull in invi_aggr.DefaultIfEmpty()
            //                     select new { Invitation = invi, ListAggr = aggrOrNull };


            //var list_Invi_Aggr = await _context.Invitations
            //                 .Where(i => i.UserId == userId)
            //                 .Include(i => i.ListAggregator)
            //                 .Include(i => i.User)
            //                 .ToListAsync();


            //----------- tiks because EF not support two left join in one query

            var invistations = _context.Invitations.AsQueryable().Where(a => a.UserId == userId).AsEnumerable();

            var list_Invi_Aggr = from invi in invistations
                                 join aggr in _context.ListAggregators on invi.ListAggregatorId equals aggr.ListAggregatorId into invi_aggr
                                 join usser in _context.Users on invi.UserId equals usser.UserId into invi_usser
                                 from aggrOrNull in invi_aggr.DefaultIfEmpty()
                                 from usseOrNull in invi_usser.DefaultIfEmpty()
                                 select new { Invitation = invi, ListAggr = aggrOrNull, Usser = usseOrNull };
            // ---------------------


            //----------- this not work  because EF not support two left join in one query

            //var list_Invi_Aggr = from invi in _context.Invitations
            //                     where invi.UserId == userId
            //                     join aggr in _context.ListAggregators on invi.ListAggregatorId equals aggr.ListAggregatorId into invi_aggr
            //                     join usser in _context.Users on invi.UserId equals usser.UserId into invi_usser
            //                     from aggrOrNull in invi_aggr.DefaultIfEmpty()
            //                     from usseOrNull in invi_usser.DefaultIfEmpty()
            //                     select new { Invitation = invi, ListAggr = aggrOrNull, Usser = usseOrNull };
            // ---------------------


            //  List<InvitationEntity> toDeleteEntity = new List<InvitationEntity>();


            //foreach (var item in list_Invi_Aggr)
            //{
            //    if (item.ListAggr == null || item.Usser == null)
            //    {
            //        _context.Remove(item.Invitation);
            //        //toDeleteEntity.Add(item.Invitation);
            //    }
            //}

            //await _context.SaveChangesAsync();


            var listInviAggr = list_Invi_Aggr;

            var invitationsList = listInviAggr.Select(a =>
            {
                var inv = _mapper.Map<Invitation>(a.Invitation);
                inv.ListAggregatorName = a.ListAggr.ListAggregatorName;

                return inv;

            }).ToList();

            return invitationsList;
        }

        public async Task RejectInvitaionAsync(Invitation invitation)
        {
            var invitationEntity = _mapper.Map<InvitationEntity>(invitation);

            _context.Remove(invitationEntity);
            await _context.SaveChangesAsync();
        }

        public async Task AcceptInvitationAsync(Invitation invitation, int userId)
        {
            var invitationEntity = _mapper.Map<InvitationEntity>(invitation);

            _context.Remove(invitationEntity);


            var userListAggregatorEntity = new UserListAggregatorEntity
            {
                ListAggregatorId = invitation.ListAggregatorId,
                UserId = userId,
                PermissionLevel = invitation.PermissionLevel
            };

            _context.Add(userListAggregatorEntity);


            await _context.SaveChangesAsync();
        }
    }
}
