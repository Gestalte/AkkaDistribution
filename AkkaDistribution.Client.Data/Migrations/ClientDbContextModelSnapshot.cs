﻿// <auto-generated />
using System;
using AkkaDistribution.Client.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AkkaDistribution.Client.Data.Migrations
{
    [DbContext(typeof(ClientDbContext))]
    partial class ClientDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.3");

            modelBuilder.Entity("AkkaDistribution.Client.Data.FilePart", b =>
                {
                    b.Property<int>("FilePartId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Position")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalPieces")
                        .HasColumnType("INTEGER");

                    b.HasKey("FilePartId");

                    b.ToTable("FileParts");
                });

            modelBuilder.Entity("AkkaDistribution.Client.Data.Manifest", b =>
                {
                    b.Property<int>("ManifestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("ManifestId");

                    b.ToTable("Manifests");
                });

            modelBuilder.Entity("AkkaDistribution.Client.Data.ManifestEntry", b =>
                {
                    b.Property<int>("ManifestEntryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ManifestId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ManifestEntryId");

                    b.HasIndex("ManifestId");

                    b.ToTable("ManifestEntries");
                });

            modelBuilder.Entity("AkkaDistribution.Client.Data.ManifestEntry", b =>
                {
                    b.HasOne("AkkaDistribution.Client.Data.Manifest", null)
                        .WithMany("ManifestEntries")
                        .HasForeignKey("ManifestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AkkaDistribution.Client.Data.Manifest", b =>
                {
                    b.Navigation("ManifestEntries");
                });
#pragma warning restore 612, 618
        }
    }
}
