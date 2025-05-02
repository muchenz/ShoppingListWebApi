using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using FirebaseDatabase;
using System;
using Shared.DataEndpoints.Models;

namespace ShoppingListWebApi.Data
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            CreateMap<User, UserEntity>();
            CreateMap<UserEntity, User>()
                .ForMember(a => a.ListAggregators, b => b.MapFrom(x => x.UserListAggregators.Select(y => y.ListAggregator)))
                .ForMember(a => a.Roles, b => b.MapFrom(x => x.UserRoles.Select(y => y.Role.RoleName)));


            CreateMap<User, UserFD>();
            CreateMap<UserFD, User>();

            CreateMap<ListItem, ListItemEntity>();
            CreateMap<ListItemEntity, ListItem>();

            CreateMap<ListItem, ListItemFD>();
            CreateMap<ListItemFD, ListItem>();

            CreateMap<Log, LogEntity>();
            CreateMap<LogEntity, Log>();

            CreateMap<Invitation, InvitationEntity>();
            CreateMap<InvitationEntity, Invitation>();

            CreateMap<Invitation, InvitationFD>();
            CreateMap<InvitationFD, Invitation>();

            CreateMap<UserListAggregator, UserListAggregatorEntity>();
            CreateMap<UserListAggregatorEntity, UserListAggregator>();

            CreateMap<UserListAggregator, UserListAggregatorFD>();
            CreateMap<UserListAggregatorFD, UserListAggregator>();

            CreateMap<List, ListEntity>();
            CreateMap<ListEntity, List>();

            CreateMap<List, ListFD>();
            CreateMap<ListFD, List>();

            CreateMap<ListAggregator, ListAggregatorEntity>();
            CreateMap<ListAggregatorEntity, ListAggregator>()
               .ForMember(a => a.PermissionLevel, b => b.MapFrom(x => x.UserListAggregators.Select(y => y.PermissionLevel).FirstOrDefault()));



            CreateMap<ListAggregator, ListAggregatorFD>()
                .ForMember(a => a.Lists, a => a.Ignore());
            CreateMap<ListAggregatorFD, ListAggregator>()
            .ForMember(a => a.Lists, a => a.Ignore());

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
