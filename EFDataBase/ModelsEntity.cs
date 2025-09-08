using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataBase
{
    public class LogEntity
    {
        public long LogId { get; set; }
        public string LogLevel { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string CreatedDate { get; set; }
        public long? UserId { get; set; }
        public long? InnerId { get; set; }
        public virtual LogEntity Inner { get; set; }

    }
    public class InvitationEntity
    {
        public int InvitationId { get; set; }
        public int UserId { get; set; }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string SenderName { get; set; }
        public virtual ListAggregator ListAggregators { get; set; }

        public virtual User Users { get; set; }

    }

    public class UserEntity
    {

        public UserEntity()
        {
            UserListAggregators = new HashSet<UserListAggregatorEntity>();
            UserRoles = new HashSet<UserRolesEntity>();


        }

        public int UserId { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }

        public byte LoginType { get; set; } // 1 - local 2 - facebook ()
        public virtual ICollection<UserListAggregatorEntity> UserListAggregators { get; set; }

        public virtual ICollection<UserRolesEntity> UserRoles { get; set; }

        // public virtual TokenItemEntity Token { get; set; }

    }

    public class UserRolesEntity
    {

        public int UserId { get; set; }
        public int RoleId { get; set; }
        public virtual UserEntity User { get; set; }
        public virtual RoleEntity Role { get; set; }

    }




    public class RoleEntity
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public virtual ICollection<UserRolesEntity> UserRoles { get; set; }


    }

    public class UserListAggregatorEntity
    {

        public int UserId { get; set; }
        public int ListAggregatorId { get; set; }
        public virtual UserEntity User { get; set; }
        public virtual ListAggregatorEntity ListAggregator { get; set; }
        public int PermissionLevel { get; set; }
        public int State { get; set; }


    }

    public class ListAggregatorEntity
    {

        public ListAggregatorEntity()
        {
            UserListAggregators = new HashSet<UserListAggregatorEntity>();
            Lists = new HashSet<ListEntity>();

        }

        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public int Order { get; set; }

        public virtual ICollection<UserListAggregatorEntity> UserListAggregators { get; set; }
        public virtual ICollection<ListEntity> Lists { get; set; }

    }

    public class ListEntity
    {
        public ListEntity()
        {
            ListItems = new HashSet<ListItemEntity>();


        }
        public int ListId { get; set; }
        public string ListName { get; set; }
        public int Order { get; set; }
        public int ListAggregatorId { get; set; }


        public virtual ICollection<ListItemEntity> ListItems { get; set; }
        public virtual ListAggregatorEntity ListAggregator { get; set; }


    }


    public class ListItemEntity
    {
        public int ListItemId { get; set; }
        public string ListItemName { get; set; }
        public int Order { get; set; }
        public int State { get; set; }

        public virtual ListEntity List { get; set; }

        public int ListId { get; set; }

    }

    public class TokenItemEntity
    {
        public int TokenId { get; set; }
        public string Token { get; set; }
        public int UserId { get; set; }

        public UserEntity User { get; set; }

    }

    public class RefreshTokenSessionEntity
    {
        public string Id { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessTokenJti { get; set; }
        public int UserId { get; set; } 
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRefreshTokenRevoked { get; set; } = false;
        public string? DeviceInfo { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public UserEntity User { get; set; }

        //[Timestamp] //  EF Core for automic concurrency handling
        //public byte[] RowVersion { get; set; }

    }

    public class ToDeleteEntity
    {
        public int Id { get; set; }
        public string ItemToDeleteId { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
    public static class RefreshTokenSessionExtensions
    {

        public static RefreshTokenSession ToRefreshTokenSession(this RefreshTokenSessionEntity token)
        {
            var session = new RefreshTokenSession()
            {
                Id = Guid.Parse(token.Id),
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                AccessTokenJti = token.AccessTokenJti,
                UserId = token.UserId.ToString(),
                Version = token.Version,
                CreatedAt = token.CreatedAt,
                DeviceInfo = token.DeviceInfo,
                IsRefreshTokenRevoked = token.IsRefreshTokenRevoked,
                ReplacedByToken = token.ReplacedByToken,
                RevokedAt = token.RevokedAt,

            };

            return session;
        }

        public static RefreshTokenSessionEntity ToRefreshTokenSessionEntity(this RefreshTokenSession token)
        {
            var session = new RefreshTokenSessionEntity()
            {
                Id = token.Id.ToString(),
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                AccessTokenJti = token.AccessTokenJti,
                UserId = int.Parse(token.UserId),
                Version = token.Version,
                CreatedAt = token.CreatedAt,
                DeviceInfo = token.DeviceInfo,
                IsRefreshTokenRevoked = token.IsRefreshTokenRevoked,
                ReplacedByToken = token.ReplacedByToken,
                RevokedAt = token.RevokedAt,

            };

            return session;
        }

    }
}
