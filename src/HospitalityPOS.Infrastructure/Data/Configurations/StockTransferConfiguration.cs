using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for StockTransferRequest.
/// </summary>
public class StockTransferRequestConfiguration : IEntityTypeConfiguration<StockTransferRequest>
{
    public void Configure(EntityTypeBuilder<StockTransferRequest> builder)
    {
        builder.ToTable("StockTransferRequests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.SourceLocationType)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(TransferRequestStatus.Draft);

        builder.Property(e => e.Priority)
            .HasConversion<int>()
            .HasDefaultValue(TransferPriority.Normal);

        builder.Property(e => e.Reason)
            .HasConversion<int>()
            .HasDefaultValue(TransferReason.Replenishment);

        builder.Property(e => e.ApprovalNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(e => e.TotalEstimatedValue)
            .HasPrecision(18, 2);

        // Foreign key to requesting store
        builder.HasOne(e => e.RequestingStore)
            .WithMany()
            .HasForeignKey(e => e.RequestingStoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to source location (store)
        builder.HasOne(e => e.SourceLocation)
            .WithMany()
            .HasForeignKey(e => e.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to lines
        builder.HasMany(e => e.Lines)
            .WithOne(l => l.TransferRequest)
            .HasForeignKey(l => l.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one with shipment
        builder.HasOne(e => e.Shipment)
            .WithOne(s => s.TransferRequest)
            .HasForeignKey<StockTransferShipment>(s => s.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on request number
        builder.HasIndex(e => e.RequestNumber)
            .IsUnique()
            .HasDatabaseName("IX_StockTransferRequests_RequestNumber");

        // Index on status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_StockTransferRequests_Status");

        // Index on requesting store
        builder.HasIndex(e => e.RequestingStoreId)
            .HasDatabaseName("IX_StockTransferRequests_RequestingStore");

        // Index on source location
        builder.HasIndex(e => e.SourceLocationId)
            .HasDatabaseName("IX_StockTransferRequests_SourceLocation");

        // Index on submitted date
        builder.HasIndex(e => e.SubmittedAt)
            .HasDatabaseName("IX_StockTransferRequests_SubmittedAt");

        // Composite index for status and priority
        builder.HasIndex(e => new { e.Status, e.Priority })
            .HasDatabaseName("IX_StockTransferRequests_Status_Priority");
    }
}

/// <summary>
/// Entity configuration for TransferRequestLine.
/// </summary>
public class TransferRequestLineConfiguration : IEntityTypeConfiguration<TransferRequestLine>
{
    public void Configure(EntityTypeBuilder<TransferRequestLine> builder)
    {
        builder.ToTable("TransferRequestLines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.ApprovalNotes)
            .HasMaxLength(500);

        // Foreign key to request
        builder.HasOne(e => e.TransferRequest)
            .WithMany(r => r.Lines)
            .HasForeignKey(e => e.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on request ID
        builder.HasIndex(e => e.TransferRequestId)
            .HasDatabaseName("IX_TransferRequestLines_RequestId");

        // Index on product ID
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_TransferRequestLines_ProductId");
    }
}

/// <summary>
/// Entity configuration for StockTransferShipment.
/// </summary>
public class StockTransferShipmentConfiguration : IEntityTypeConfiguration<StockTransferShipment>
{
    public void Configure(EntityTypeBuilder<StockTransferShipment> builder)
    {
        builder.ToTable("StockTransferShipments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ShipmentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Carrier)
            .HasMaxLength(100);

        builder.Property(e => e.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(e => e.VehicleDetails)
            .HasMaxLength(200);

        builder.Property(e => e.DriverName)
            .HasMaxLength(100);

        builder.Property(e => e.DriverContact)
            .HasMaxLength(50);

        builder.Property(e => e.TotalWeightKg)
            .HasPrecision(10, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Foreign key to request (1-to-1)
        builder.HasOne(e => e.TransferRequest)
            .WithOne(r => r.Shipment)
            .HasForeignKey<StockTransferShipment>(e => e.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on shipment number
        builder.HasIndex(e => e.ShipmentNumber)
            .IsUnique()
            .HasDatabaseName("IX_StockTransferShipments_ShipmentNumber");

        // Index on shipped date
        builder.HasIndex(e => e.ShippedAt)
            .HasDatabaseName("IX_StockTransferShipments_ShippedAt");
    }
}

/// <summary>
/// Entity configuration for StockTransferReceipt.
/// </summary>
public class StockTransferReceiptConfiguration : IEntityTypeConfiguration<StockTransferReceipt>
{
    public void Configure(EntityTypeBuilder<StockTransferReceipt> builder)
    {
        builder.ToTable("StockTransferReceipts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Foreign key to request
        builder.HasOne(e => e.TransferRequest)
            .WithMany()
            .HasForeignKey(e => e.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to lines
        builder.HasMany(e => e.Lines)
            .WithOne(l => l.TransferReceipt)
            .HasForeignKey(l => l.TransferReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to issues
        builder.HasMany(e => e.Issues)
            .WithOne()
            .HasForeignKey(i => i.TransferReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on receipt number
        builder.HasIndex(e => e.ReceiptNumber)
            .IsUnique()
            .HasDatabaseName("IX_StockTransferReceipts_ReceiptNumber");

        // Index on received date
        builder.HasIndex(e => e.ReceivedAt)
            .HasDatabaseName("IX_StockTransferReceipts_ReceivedAt");

        // Index on has issues
        builder.HasIndex(e => e.HasIssues)
            .HasDatabaseName("IX_StockTransferReceipts_HasIssues");
    }
}

/// <summary>
/// Entity configuration for TransferReceiptLine.
/// </summary>
public class TransferReceiptLineConfiguration : IEntityTypeConfiguration<TransferReceiptLine>
{
    public void Configure(EntityTypeBuilder<TransferReceiptLine> builder)
    {
        builder.ToTable("TransferReceiptLines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Foreign key to receipt
        builder.HasOne(e => e.TransferReceipt)
            .WithMany(r => r.Lines)
            .HasForeignKey(e => e.TransferReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to request line
        builder.HasOne(e => e.TransferRequestLine)
            .WithMany()
            .HasForeignKey(e => e.TransferRequestLineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on receipt ID
        builder.HasIndex(e => e.TransferReceiptId)
            .HasDatabaseName("IX_TransferReceiptLines_ReceiptId");
    }
}

/// <summary>
/// Entity configuration for TransferReceiptIssue.
/// </summary>
public class TransferReceiptIssueConfiguration : IEntityTypeConfiguration<TransferReceiptIssue>
{
    public void Configure(EntityTypeBuilder<TransferReceiptIssue> builder)
    {
        builder.ToTable("TransferReceiptIssues");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IssueType)
            .HasConversion<int>();

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.PhotoPath)
            .HasMaxLength(500);

        builder.Property(e => e.ResolutionNotes)
            .HasMaxLength(1000);

        // Foreign key to receipt line
        builder.HasOne(e => e.TransferReceiptLine)
            .WithMany()
            .HasForeignKey(e => e.TransferReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on issue type
        builder.HasIndex(e => e.IssueType)
            .HasDatabaseName("IX_TransferReceiptIssues_IssueType");

        // Index on resolved status
        builder.HasIndex(e => e.IsResolved)
            .HasDatabaseName("IX_TransferReceiptIssues_Resolved");

        // Index on receipt ID
        builder.HasIndex(e => e.TransferReceiptId)
            .HasDatabaseName("IX_TransferReceiptIssues_ReceiptId");
    }
}

/// <summary>
/// Entity configuration for TransferActivityLog.
/// </summary>
public class TransferActivityLogConfiguration : IEntityTypeConfiguration<TransferActivityLog>
{
    public void Configure(EntityTypeBuilder<TransferActivityLog> builder)
    {
        builder.ToTable("TransferActivityLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Activity)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PreviousStatus)
            .HasConversion<int?>();

        builder.Property(e => e.NewStatus)
            .HasConversion<int?>();

        builder.Property(e => e.Details)
            .HasMaxLength(2000);

        // Foreign key to request
        builder.HasOne(e => e.TransferRequest)
            .WithMany()
            .HasForeignKey(e => e.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on request ID
        builder.HasIndex(e => e.TransferRequestId)
            .HasDatabaseName("IX_TransferActivityLogs_RequestId");

        // Index on performed date
        builder.HasIndex(e => e.PerformedAt)
            .HasDatabaseName("IX_TransferActivityLogs_PerformedAt");
    }
}

/// <summary>
/// Entity configuration for StockReservation.
/// </summary>
public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LocationType)
            .HasConversion<int>();

        builder.Property(e => e.ReferenceType)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ReservationStatus.Active);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Foreign key to location (store)
        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on location
        builder.HasIndex(e => e.LocationId)
            .HasDatabaseName("IX_StockReservations_LocationId");

        // Index on product
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_StockReservations_ProductId");

        // Index on status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_StockReservations_Status");

        // Index on reference (for finding reservations by transfer request)
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId })
            .HasDatabaseName("IX_StockReservations_Reference");

        // Index on expiration for cleanup queries
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_StockReservations_ExpiresAt");

        // Composite index for checking available stock
        builder.HasIndex(e => new { e.LocationId, e.ProductId, e.Status })
            .HasDatabaseName("IX_StockReservations_Location_Product_Status");
    }
}
