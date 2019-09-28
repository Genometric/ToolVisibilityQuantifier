﻿// <auto-generated />
using Genometric.TVQ.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genometric.TVQ.API.Infrastructure.Migrations
{
    [DbContext(typeof(TVQContext))]
    partial class TVQContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Genometric.TVQ.API.Model.Publication", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Citation");

                    b.Property<int>("CitedBy");

                    b.Property<string>("DOI");

                    b.Property<string>("PubMedID");

                    b.Property<string>("Title");

                    b.Property<int>("ToolID");

                    b.Property<string>("Year");

                    b.HasKey("ID");

                    b.HasIndex("ToolID");

                    b.ToTable("Publications");
                });

            modelBuilder.Entity("Genometric.TVQ.API.Model.Repository", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("Name");

                    b.Property<string>("URI")
                        .IsRequired();

                    b.HasKey("ID");

                    b.ToTable("Repositories");
                });

            modelBuilder.Entity("Genometric.TVQ.API.Model.Tool", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("CodeRepo");

                    b.Property<string>("Description");

                    b.Property<string>("Homepage");

                    b.Property<string>("IDinRepo");

                    b.Property<string>("Name");

                    b.Property<string>("Owner");

                    b.Property<int>("RepositoryID");

                    b.Property<int>("TimesDownloaded");

                    b.Property<string>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("RepositoryID");

                    b.ToTable("Tools");
                });

            modelBuilder.Entity("Genometric.TVQ.API.Model.Publication", b =>
                {
                    b.HasOne("Genometric.TVQ.API.Model.Tool", "Tool")
                        .WithMany("Publications")
                        .HasForeignKey("ToolID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Genometric.TVQ.API.Model.Tool", b =>
                {
                    b.HasOne("Genometric.TVQ.API.Model.Repository", "Repository")
                        .WithMany("Tools")
                        .HasForeignKey("RepositoryID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
