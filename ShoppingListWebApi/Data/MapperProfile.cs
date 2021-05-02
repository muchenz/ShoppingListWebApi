using AutoMapper;
using EFDataBase;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
           
                CreateMap<User, UserEntity>();
                CreateMap<UserEntity, User>()
                    .ForMember(a => a.ListAggregators, b => b.MapFrom(x => x.UserListAggregators.Select(y => y.ListAggregator)))
                    .ForMember(a => a.Roles, b => b.MapFrom(x => x.UserRoles.Select(y => y.Role.RoleName)));
                             
                                

                CreateMap<ListItem, ListItemEntity>();
                CreateMap<ListItemEntity, ListItem>();

            CreateMap<Invitation, InvitationEntity>();
            CreateMap<InvitationEntity, Invitation>();

            CreateMap<List, ListEntity>();
                CreateMap<ListEntity, List>();

                CreateMap<ListAggregator, ListAggregatorEntity>();
                CreateMap<ListAggregatorEntity, ListAggregator>()
                .ForMember(a=>a.PermissionLevel, b=>b.MapFrom(x=>x.UserListAggregators.Select(y=>y.PermissionLevel).FirstOrDefault()));

           
                CreateMap<RoleEntity, Role>();

            CreateMap<KeyValuePair<UserEntity, int>, KeyValuePair<User, int>>()
                .ConstructUsing(a => new KeyValuePair<User, int>(new User
                {
                    EmailAddress = a.Key.EmailAddress,
                    UserId = a.Key.UserId,                   
                   
                }, a.Value));

        }

    }
}
