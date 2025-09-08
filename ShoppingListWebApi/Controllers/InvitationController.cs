using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using SignalRService;


namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationController : ControllerBase
    {

        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly SignarRService _signarRService;
        private readonly IInvitationEndpoint _invitationEndpoint;

        public InvitationController(IMapper mapper, SignarRService signarRService
            , IInvitationEndpoint invitationEndpoint)
        {
            _mapper = mapper;
            _signarRService = signarRService;
            _invitationEndpoint = invitationEndpoint;
        }

        [HttpGet("InvitationsList")]
        [Authorize]
        public async Task<List<Invitation>> GetInvitationsList()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;

            //TODO: 'userName' should be get from context

            var invitationsList = await _invitationEndpoint.GetInvitationsListAsync(int.Parse(userId));

            return invitationsList;
        }



        [HttpPost("RejectInvitaion")]
        [Authorize]
        public async Task<ActionResult> RejectInvitaion([FromBody] Invitation invitation, [FromHeader] string signalRId)
        {
            //TODO: better permssion check (eg. user name in invitation)
            await _invitationEndpoint.RejectInvitaionAsync(invitation);   

            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault()?.Value;
            //TODO: for what sending 'new_invitation'?? for refresh list with invitation??
            if (userId != null)
                await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { int.Parse(userId) }, 
                    SiganalREventName.InvitationAreChanged, signalRId: signalRId);

            return Ok();
        }

        [HttpPost("AcceptInvitation")]
        [Authorize]
        public async Task<ActionResult> AcceptInvitation([FromBody] Invitation invitation, [FromHeader] string signalRId)
        {
            //TODO: better permssion check (eg. user name in invitation)

            var userId = int.Parse(User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value);

            await _invitationEndpoint.AcceptInvitationAsync(invitation, userId);

           await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { userId }, 
               SiganalREventName.InvitationAreChanged, signalRId: signalRId);

            //TODO: for what sending 'new_invitation'?? for refresh list with invitation??

            return Ok();
        }
    }
}