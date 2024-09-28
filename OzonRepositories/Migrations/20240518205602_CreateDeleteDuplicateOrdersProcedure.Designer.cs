﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OzonRepositories.Context;

#nullable disable

namespace OzonRepositories.Migrations
{
    [DbContext(typeof(OzonOrderContext))]
    [Migration("20240518205602_CreateDeleteDuplicateOrdersProcedure")]
    partial class CreateDeleteDuplicateOrdersProcedure
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("OzonDomains.Models.AppStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("AppStatuses");
                });

            modelBuilder.Entity("OzonDomains.Models.Currency", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Currencys");
                });

            modelBuilder.Entity("OzonDomains.Models.Order", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AppStatusId")
                        .HasColumnType("int");

                    b.Property<string>("Article")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Comment")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("DeliveryCity")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsAccepted")
                        .HasColumnType("bit");

                    b.Property<bool?>("IsReturnable")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<string>("Key")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("MaxCommissionInfo")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("MaxDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MaxOzonCommission")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MaxProfit")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("MinCommissionInfo")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("MinDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MinOzonCommission")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MinProfit")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("NewCategory")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("OrderNumberToSupplier")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("PeriodEnd")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime2")
                        .HasColumnName("PeriodEnd");

                    b.Property<DateTime>("PeriodStart")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime2")
                        .HasColumnName("PeriodStart");

                    b.Property<decimal?>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("ProcessingDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("ProductInfoId")
                        .HasColumnType("int");

                    b.Property<string>("ProductName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<decimal?>("PurchasePrice")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal?>("ShipmentAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("ShipmentNumber")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int?>("ShipmentWarehouseId")
                        .HasMaxLength(100)
                        .HasColumnType("int");

                    b.Property<DateTime?>("ShippingDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int?>("SupplierId")
                        .HasColumnType("int");

                    b.Property<int?>("TransactionId")
                        .HasColumnType("int");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UpdatedColumns")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("СurrencyId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppStatusId");

                    b.HasIndex("ProductInfoId");

                    b.HasIndex("ShipmentWarehouseId");

                    b.HasIndex("SupplierId");

                    b.HasIndex("TransactionId");

                    b.HasIndex("СurrencyId");

                    b.ToTable("Orders", (string)null);

                    b.ToTable(tb => tb.IsTemporal(ttb =>
                            {
                                ttb.UseHistoryTable("OrdersHistory");
                                ttb
                                    .HasPeriodStart("PeriodStart")
                                    .HasColumnName("PeriodStart");
                                ttb
                                    .HasPeriodEnd("PeriodEnd")
                                    .HasColumnName("PeriodEnd");
                            }));
                });

            modelBuilder.Entity("OzonDomains.Models.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Article")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("CommercialCategory")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<decimal?>("CurrentPriceWithDiscount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("FboOzonSkuId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("FbsOzonSkuId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("OzonProductId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<double?>("Volume")
                        .HasColumnType("float");

                    b.Property<double?>("VolumetricWeight")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("OzonDomains.Models.Supplier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Suppliers");
                });

            modelBuilder.Entity("OzonDomains.Models.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Comment")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("CreateBy")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime?>("CreatedDateTime")
                        .HasColumnType("datetime2");

                    b.Property<int?>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("OzonDomains.Models.Warehouse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Warehouses");
                });

            modelBuilder.Entity("OzonDomains.Models.Order", b =>
                {
                    b.HasOne("OzonDomains.Models.AppStatus", "AppStatus")
                        .WithMany()
                        .HasForeignKey("AppStatusId");

                    b.HasOne("OzonDomains.Models.Product", "ProductInfo")
                        .WithMany()
                        .HasForeignKey("ProductInfoId");

                    b.HasOne("OzonDomains.Models.Warehouse", "ShipmentWarehouse")
                        .WithMany()
                        .HasForeignKey("ShipmentWarehouseId");

                    b.HasOne("OzonDomains.Models.Supplier", "Supplier")
                        .WithMany()
                        .HasForeignKey("SupplierId");

                    b.HasOne("OzonDomains.Models.Transaction", null)
                        .WithMany("Orders")
                        .HasForeignKey("TransactionId");

                    b.HasOne("OzonDomains.Models.Currency", "Сurrency")
                        .WithMany()
                        .HasForeignKey("СurrencyId");

                    b.Navigation("AppStatus");

                    b.Navigation("ProductInfo");

                    b.Navigation("ShipmentWarehouse");

                    b.Navigation("Supplier");

                    b.Navigation("Сurrency");
                });

            modelBuilder.Entity("OzonDomains.Models.Transaction", b =>
                {
                    b.Navigation("Orders");
                });
#pragma warning restore 612, 618
        }
    }
}
