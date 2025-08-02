using Google.Cloud.Firestore;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirebaseDatabase
{

    public class BaseFD
    {
        public string Id { get; set; }
    }

    [FirestoreData]
    public class UserFD : BaseFD
    {

        public UserFD()
        {
            Roles = new HashSet<string>();

        }
        [FirestoreProperty]
        public int UserId { get; set; }

        [FirestoreProperty]
        public string EmailAddress { get; set; }

        [FirestoreProperty]
        public string Password { get; set; }

        [FirestoreProperty]
        public byte LoginType { get; set; } // 1 - local 2 - facebook ()


        [FirestoreProperty]
        public ICollection<string> Roles { get; set; }

        // public virtual TokenItemEntity Token { get; set; }

    }

    [FirestoreData]
    public class UserListAggregatorFD
    {

        [FirestoreProperty]
        public int UserId { get; set; }

        [FirestoreProperty]
        public int ListAggregatorId { get; set; }

        [FirestoreProperty]
        public int PermissionLevel { get; set; }

        [FirestoreProperty]
        public int State { get; set; }


    }

    [FirestoreData]
    public class ListAggregatorFD
    {

        public ListAggregatorFD()
        {
            Lists = new HashSet<int>();

        }
        [FirestoreProperty]
        public int ListAggregatorId { get; set; }
        [FirestoreProperty]

        public string ListAggregatorName { get; set; }
        [FirestoreProperty]
        public int Order { get; set; }

        [FirestoreProperty]
        public ICollection<int> Lists { get; set; }

    }

    [FirestoreData]
    public class ListFD
    {
        public ListFD()
        {
            ListItems = new HashSet<int>();

        }

        [FirestoreProperty]
        public int ListId { get; set; }
        [FirestoreProperty]
        public string ListName { get; set; }
        [FirestoreProperty]
        public int Order { get; set; }
        [FirestoreProperty]
        public ICollection<int> ListItems { get; set; }
        [FirestoreProperty]
        public int ListAggrId { get; set; }

    }

    [FirestoreData]
    public class ListItemFD
    {
        [FirestoreProperty]
        public int ListItemId { get; set; }
        [FirestoreProperty]
        public string ListItemName { get; set; }
        [FirestoreProperty]
        public int Order { get; set; }
        [FirestoreProperty]
        public int State { get; set; }

        [FirestoreProperty]
        public int ListAggrId { get; set; }
        [FirestoreProperty]
        public int ListId { get; set; }
    }

    [FirestoreData]
    public class InvitationFD : BaseFD
    {
        [FirestoreProperty]
        public int InvitationId { get; set; }
        [FirestoreProperty]
        public string EmailAddress { get; set; }
        [FirestoreProperty]
        public int PermissionLevel { get; set; }
        [FirestoreProperty]
        public int ListAggregatorId { get; set; }
        [FirestoreProperty]
        public string SenderName { get; set; }

    }
    [FirestoreData]
    public class RefreshTokenSessionFD
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public string RefreshToken { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? AccessTokenJti { get; set; }
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public int Version { get; set; } 
        [FirestoreProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [FirestoreProperty]
        public DateTime ExpiresAt { get; set; }
        [FirestoreProperty]
        public bool IsRefreshTokenRevoked { get; set; } = false;
        [FirestoreProperty]
        public string? DeviceInfo { get; set; }
        [FirestoreProperty]
        public DateTime? RevokedAt { get; set; }
        [FirestoreProperty]
        public string? ReplacedByToken { get; set; }
        
    }

    public static class RefreshTokenSessionExtensions
    {

        public static RefreshTokenSession ToRefreshTokenSession(this RefreshTokenSessionFD token)
        {
            var session = new RefreshTokenSession()
            {
                Id = Guid.Parse(token.Id),
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                AccessTokenJti = token.AccessTokenJti,
                UserId = token.UserId,
                Version = token.Version,
                CreatedAt = token.CreatedAt,
                DeviceInfo = token.DeviceInfo,
                IsRefreshTokenRevoked = token.IsRefreshTokenRevoked,
                ReplacedByToken = token.ReplacedByToken,
                RevokedAt = token.RevokedAt,

            };

            return session;
        }

        public static RefreshTokenSessionFD ToRefreshTokenFDSession(this RefreshTokenSession token)
        {
            var session = new RefreshTokenSessionFD()
            {
                Id = token.Id.ToString(),
                RefreshToken = token.RefreshToken,
                ExpiresAt = token.ExpiresAt,
                AccessTokenJti = token.AccessTokenJti,
                UserId = token.UserId,
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
