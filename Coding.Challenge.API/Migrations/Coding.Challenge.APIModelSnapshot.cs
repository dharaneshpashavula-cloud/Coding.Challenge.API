using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Coding.Challenge.API.Data;

#nullable disable

namespace Coding.Challenge.API.Migrations
{
    [DbContext(typeof(AnalyticsDbContext))]
    partial class CodingChallengeAPIModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Coding.Challenge.API.Models.SensorEvent", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<string>("Gate")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("TEXT");

                b.Property<DateTime>("Timestamp")
                    .HasColumnType("TEXT");

                b.Property<int>("NumberOfPeople")
                    .HasColumnType("INTEGER");

                b.Property<string>("Type")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.HasIndex("Gate", "Type");

                b.ToTable("SensorEvents");
            });
        }
    }
}
