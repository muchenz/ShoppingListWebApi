using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shared;


namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationController : ControllerBase
    {

        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        
        public InvitationController(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;           
        }

        [HttpPost("GetInvitationsList")]
        public async Task<ActionResult<MessageAndStatus>> GetInvitationsList(string userName)
        {


            var list_Invi_Aggr = from invi in _context.Invitations
                                 where invi.EmailAddress == userName
                                 join aggr in _context.ListAggregators on invi.ListAggregatorId equals aggr.ListAggregatorId into invi_aggr
                                 from aggrOrNull in invi_aggr.DefaultIfEmpty()
                                 select new { Invitation = invi, ListAggr = aggrOrNull };


            //  List<InvitationEntity> toDeleteEntity = new List<InvitationEntity>();


            foreach (var item in list_Invi_Aggr)
            {
                if (item.ListAggr == null)
                {
                    _context.Remove(item.Invitation);
                    //toDeleteEntity.Add(item.Invitation);
                }
            }

            await _context.SaveChangesAsync();

            var listInviAggr = await list_Invi_Aggr.ToListAsync();

            var invitationsList = listInviAggr.Select(a =>
            {
                var inv = _mapper.Map<Invitation>(a.Invitation);
                inv.ListAggregatorName = a.ListAggr.ListAggregatorName;

                return inv;

            }).ToList();


            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(invitationsList) };
        }



        [HttpPost("RejectInvitaion")]
        public async Task<ActionResult<MessageAndStatus>> RejectInvitaion([FromBody] Invitation invitation)
        {

            var invitationEntity = _mapper.Map<InvitationEntity>(invitation);

            _context.Remove(invitationEntity);
            await _context.SaveChangesAsync();

            return await Task.FromResult(new MessageAndStatus { Status = "OK" });
        }

        [HttpPost("AcceptInvitation")]
        public async Task<ActionResult<MessageAndStatus>> AcceptInvitation([FromBody] Invitation invitation)
        {

            var invitationEntity = _mapper.Map<InvitationEntity>(invitation);

            _context.Remove(invitationEntity);

            var userId = _context.Users.Single(a => a.EmailAddress == HttpContext.User.Identity.Name).UserId;

            var userListAggregatorEntity = new UserListAggregatorEntity
            {
                ListAggregatorId = invitation.ListAggregatorId,
                UserId = userId,
                PermissionLevel=invitation.PermissionLevel
            };

            _context.Add(userListAggregatorEntity);


            await _context.SaveChangesAsync();

            return await Task.FromResult(new MessageAndStatus { Status = "OK" });
        }
    }
}