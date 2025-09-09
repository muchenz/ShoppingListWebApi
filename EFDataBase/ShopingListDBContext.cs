using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataBase
{
    // dotnet ef migrations add  Add_Token_ToDelete -p EFDataBase -s ShoppingListWebApi
    // dotnet ef database update -p EFDataBase -s ShoppingListWebApi

    // 
    // Database migration 0

    public partial class ShopingListDBContext : DbContext
    {
        public ShopingListDBContext()
        {

        }

        public ShopingListDBContext(DbContextOptions<ShopingListDBContext> options)
          : base(options)
        {
        }


        public virtual DbSet<UserEntity> Users { get; set; }
        public virtual DbSet<UserListAggregatorEntity> UserListAggregators { get; set; }
        public virtual DbSet<ListAggregatorEntity> ListAggregators { get; set; }
        public virtual DbSet<ListEntity> Lists { get; set; }
        public virtual DbSet<ListItemEntity> ListItems { get; set; }
        public virtual DbSet<InvitationEntity> Invitations { get; set; }
        public virtual DbSet<LogEntity> Logs { get; set; }
        public virtual DbSet<RefreshTokenSessionEntity> RefreshTokenSessions { get; set; }
        public virtual DbSet<ToDeleteEntity> ToDeletes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //optionsBuilder.UseSqlite("Name=ShopingListDB3");
                optionsBuilder.UseSqlite("data source=C:\\Users\\muchenz\\source\\repos\\ShoppingListWebApi\\ShippingListDB_SQLite\\ShippingListDB_SQLite2.db");
            }
        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<LogEntity>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.Property(e => e.LogId).HasColumnName("log_id");

                entity.Property(e => e.CreatedDate).HasColumnName("created_date");

                entity.Property(e => e.ExceptionMessage).HasColumnName("exception_message");
                
                entity.Property(e => e.Message).HasColumnName("message");

                entity.Property(e => e.LogLevel).HasColumnName("log_level");

                entity.Property(e => e.Source).HasColumnName("source");

                entity.Property(e => e.StackTrace).HasColumnName("stack_trace");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.InnerId).HasColumnName("inner_id");
            
            });

                  

            modelBuilder.Entity<UserEntity>(entity =>
            {

                entity.HasKey(e => e.UserId);

                entity.HasIndex(a => a.EmailAddress).IsUnique();

                entity.ToTable("User");

                entity.Property(e => e.UserId).HasColumnName("UserId");

                entity.Property(e => e.LoginType)
                   .HasColumnName("LoginType")
                   .HasDefaultValue(1);
                   

                entity.Property(e => e.EmailAddress)
                    .HasColumnName("EmailAddress")
                    .HasMaxLength(100)
                    .IsUnicode(true)
                    .IsRequired();

                entity.Property(e => e.Password)
                    .HasColumnName("Password")
                    .HasMaxLength(50)
                    .IsUnicode(true)
                    .IsRequired();

            });

            modelBuilder.Entity<InvitationEntity>(entity =>
                {
                    entity.HasKey(e => e.InvitationId);

                   // entity.HasIndex(a => a.EmailAddress).IsUnique();
                   // entity.Property(e => e.EmailAddress)
                   //.HasColumnName("EmailAddress")
                   //.HasMaxLength(100)
                   //.IsRequired();


                    entity.HasOne(d => d.ListAggregator)
                        .WithMany()
                        .HasForeignKey(d => d.ListAggregatorId);

                    entity.HasOne(d => d.User)
                        .WithMany()
                        .HasForeignKey(d => d.UserId);

                    entity.HasIndex(e => new { e.UserId, e.ListAggregatorId })
                        .IsUnique();

                    entity.Property(e => e.PermissionLevel)
                    .HasColumnName("PermissionLevel")
                    .IsRequired();

                    entity.Property(e => e.ListAggregatorId)
                    .HasColumnName("ListAggregatorId")
                    .IsRequired();

                    entity.Property(e => e.SenderName)
                .HasColumnName("SenderName")
                .HasMaxLength(100)
                .IsUnicode(true);

                });


            modelBuilder.Entity<UserListAggregatorEntity>()
  .HasKey(bc => new { bc.UserId, bc.ListAggregatorId });

            modelBuilder.Entity<UserListAggregatorEntity>()
                .Property(e => e.PermissionLevel).HasColumnName("PermissionLevel");
            modelBuilder.Entity<UserListAggregatorEntity>().Property(e => e.State).HasColumnName("State");

            modelBuilder.Entity<UserListAggregatorEntity>()
                .HasOne(bc => bc.ListAggregator)
                .WithMany(b => b.UserListAggregators)
                .HasForeignKey(bc => bc.ListAggregatorId);

            modelBuilder.Entity<UserListAggregatorEntity>()
                .HasOne(bc => bc.User)
                .WithMany(c => c.UserListAggregators)
                .HasForeignKey(bc => bc.UserId);

            //         modelBuilder.Entity<UserListAggregator>()
            //.HasKey(bc => new { bc.UserId, bc.ListId });

            //         modelBuilder.Entity<UserList>()
            //             .HasOne(bc => bc.List)
            //             .WithMany(b => b.UserLists)
            //             .HasForeignKey(bc => bc.ListId);

            //         modelBuilder.Entity<UserList>()
            //             .HasOne(bc => bc.User)
            //             .WithMany(c => c.UserLists)
            //             .HasForeignKey(bc => bc.UserId);


            modelBuilder.Entity<ListAggregatorEntity>(entity =>
            {
                entity.ToTable("ListAggregator");

                entity.HasKey(e => e.ListAggregatorId);

                entity.Property(e => e.ListAggregatorId).HasColumnName("ListAggregatorId");

                entity.Property(e => e.ListAggregatorName)
                    .HasColumnName("ListAggregatorName")
                    .HasMaxLength(100)
                    .IsUnicode(true);

                entity.Property(e => e.Order).HasColumnName("Order");

            });

            modelBuilder.Entity<ListEntity>(entity =>
            {
                entity.ToTable("List");

                entity.HasKey(e => e.ListId);

                entity.Property(e => e.ListId).HasColumnName("ListId");

                entity.Property(e => e.Order).HasColumnName("Order");

                entity.Property(e => e.ListName)
                    .HasColumnName("ListName")
                    .HasMaxLength(100)
                    .IsUnicode(true);

                entity.HasOne(d => d.ListAggregator)
                  .WithMany(p => p.Lists)
                  .HasForeignKey(d => d.ListAggregatorId);


            });

            modelBuilder.Entity<ListItemEntity>(entity =>
            {
                entity.ToTable("ListItem");

                entity.HasKey(e => e.ListItemId);

                entity.Property(e => e.ListItemId).HasColumnName("ListItemId");

                entity.Property(e => e.Order).HasColumnName("Order");
                entity.Property(e => e.State).HasColumnName("State");

                entity.Property(e => e.ListItemName)
                    .HasColumnName("ListItemName")
                    .HasMaxLength(100)
                    .IsUnicode(true);


                entity.HasOne(d => d.List)
                   .WithMany(p => p.ListItems)
                   .HasForeignKey(d => d.ListId);

            });


            modelBuilder.Entity<RoleEntity>(entity =>
            {
                entity.ToTable("Role");

                entity.HasKey(e => e.RoleId);

                entity.Property(e => e.RoleId).HasColumnName("RoleId");

                entity.Property(e => e.RoleName)
                    .HasColumnName("RoleName")
                    .HasMaxLength(100)
                    .IsUnicode(true);

            });

            modelBuilder.Entity<UserRolesEntity>()
                .HasKey(bc => new { bc.UserId, bc.RoleId });

            modelBuilder.Entity<UserRolesEntity>()
                .HasOne(bc => bc.Role)
                .WithMany(b => b.UserRoles)
                .HasForeignKey(bc => bc.RoleId);

            modelBuilder.Entity<UserRolesEntity>()
                .HasOne(bc => bc.User)
                .WithMany(c => c.UserRoles)
                .HasForeignKey(bc => bc.UserId);




            //modelBuilder.Entity<TokenItemEntity>(entity =>
            //{
            //    entity.ToTable("TokenItem");

            //    entity.HasKey(e => e.TokenId);


            //    entity.Property(e => e.Token)
            //        .HasColumnName("Token")
            //        .HasMaxLength(1024)
            //        .IsUnicode(true);


            //    entity.HasOne(d => d.User)
            //       .WithOne(p => p.Token);
            //});

            modelBuilder.Entity<RefreshTokenSessionEntity>(entity =>
            {
                entity.ToTable("RefreshTokenSession");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();


                entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId);


            });

            modelBuilder.Entity<ToDeleteEntity>(entity =>
            {
                entity.ToTable("ToDelete");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");


            });
        }
    }
}

