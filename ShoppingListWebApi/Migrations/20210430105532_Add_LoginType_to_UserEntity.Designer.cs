﻿// <auto-generated />
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ShoppingListWebApi.Migrations
{
    [DbContext(typeof(ShopingListDBContext))]
    [Migration("20210430105532_Add_LoginType_to_UserEntity")]
    partial class Add_LoginType_to_UserEntity
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5");

            modelBuilder.Entity("EFDataBase.InvitationEntity", b =>
                {
                    b.Property<int>("InvitationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasColumnName("EmailAddress")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("ListAggregatorId")
                        .HasColumnName("ListAggregatorId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PermissionLevel")
                        .HasColumnName("PermissionLevel")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SenderName")
                        .HasColumnName("SenderName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.HasKey("InvitationId");

                    b.HasIndex("EmailAddress")
                        .IsUnique();

                    b.ToTable("Invitations");
                });

            modelBuilder.Entity("EFDataBase.ListAggregatorEntity", b =>
                {
                    b.Property<int>("ListAggregatorId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("ListAggregatorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ListAggregatorName")
                        .HasColumnName("ListAggregatorName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.Property<int>("Order")
                        .HasColumnName("Order")
                        .HasColumnType("INTEGER");

                    b.HasKey("ListAggregatorId");

                    b.ToTable("ListAggregator");
                });

            modelBuilder.Entity("EFDataBase.ListEntity", b =>
                {
                    b.Property<int>("ListId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("ListId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ListAggregatorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ListName")
                        .HasColumnName("ListName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.Property<int>("Order")
                        .HasColumnName("Order")
                        .HasColumnType("INTEGER");

                    b.HasKey("ListId");

                    b.HasIndex("ListAggregatorId");

                    b.ToTable("List");
                });

            modelBuilder.Entity("EFDataBase.ListItemEntity", b =>
                {
                    b.Property<int>("ListItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("ListItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ListId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ListItemName")
                        .HasColumnName("ListItemName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.Property<int>("Order")
                        .HasColumnName("Order")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnName("State")
                        .HasColumnType("INTEGER");

                    b.HasKey("ListItemId");

                    b.HasIndex("ListId");

                    b.ToTable("ListItem");
                });

            modelBuilder.Entity("EFDataBase.RoleEntity", b =>
                {
                    b.Property<int>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("RoleId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RoleName")
                        .HasColumnName("RoleName")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.HasKey("RoleId");

                    b.ToTable("Role");
                });

            modelBuilder.Entity("EFDataBase.UserEntity", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasColumnName("EmailAddress")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100)
                        .IsUnicode(true);

                    b.Property<byte>("LoginType")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("LoginType")
                        .HasColumnType("INTEGER")
                        .HasDefaultValue((byte)1);

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnName("Password")
                        .HasColumnType("TEXT")
                        .HasMaxLength(50)
                        .IsUnicode(true);

                    b.HasKey("UserId");

                    b.HasIndex("EmailAddress")
                        .IsUnique();

                    b.ToTable("User");
                });

            modelBuilder.Entity("EFDataBase.UserListAggregatorEntity", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ListAggregatorId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PermissionLevel")
                        .HasColumnName("PermissionLevel")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "ListAggregatorId");

                    b.HasIndex("ListAggregatorId");

                    b.ToTable("UserListAggregators");
                });

            modelBuilder.Entity("EFDataBase.UserRolesEntity", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RoleId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("UserRolesEntity");
                });

            modelBuilder.Entity("EFDataBase.ListEntity", b =>
                {
                    b.HasOne("EFDataBase.ListAggregatorEntity", "ListAggregator")
                        .WithMany("Lists")
                        .HasForeignKey("ListAggregatorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("EFDataBase.ListItemEntity", b =>
                {
                    b.HasOne("EFDataBase.ListEntity", "List")
                        .WithMany("ListItems")
                        .HasForeignKey("ListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("EFDataBase.UserListAggregatorEntity", b =>
                {
                    b.HasOne("EFDataBase.ListAggregatorEntity", "ListAggregator")
                        .WithMany("UserListAggregators")
                        .HasForeignKey("ListAggregatorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EFDataBase.UserEntity", "User")
                        .WithMany("UserListAggregators")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("EFDataBase.UserRolesEntity", b =>
                {
                    b.HasOne("EFDataBase.RoleEntity", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EFDataBase.UserEntity", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
