using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for StockTransferService.
/// </summary>
public class StockTransferServiceTests
{
    private readonly Mock<IRepository<StockTransferRequest>> _requestRepoMock;
    private readonly Mock<IRepository<TransferRequestLine>> _lineRepoMock;
    private readonly Mock<IRepository<StockTransferShipment>> _shipmentRepoMock;
    private readonly Mock<IRepository<StockTransferReceipt>> _receiptRepoMock;
    private readonly Mock<IRepository<TransferReceiptLine>> _receiptLineRepoMock;
    private readonly Mock<IRepository<TransferReceiptIssue>> _issueRepoMock;
    private readonly Mock<IRepository<TransferActivityLog>> _activityLogRepoMock;
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Inventory>> _inventoryRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IStockReservationService> _reservationServiceMock;
    private readonly StockTransferService _service;

    public StockTransferServiceTests()
    {
        _requestRepoMock = new Mock<IRepository<StockTransferRequest>>();
        _lineRepoMock = new Mock<IRepository<TransferRequestLine>>();
        _shipmentRepoMock = new Mock<IRepository<StockTransferShipment>>();
        _receiptRepoMock = new Mock<IRepository<StockTransferReceipt>>();
        _receiptLineRepoMock = new Mock<IRepository<TransferReceiptLine>>();
        _issueRepoMock = new Mock<IRepository<TransferReceiptIssue>>();
        _activityLogRepoMock = new Mock<IRepository<TransferActivityLog>>();
        _storeRepoMock = new Mock<IRepository<Store>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _inventoryRepoMock = new Mock<IRepository<Inventory>>();
        _userRepoMock = new Mock<IRepository<User>>();
        _reservationServiceMock = new Mock<IStockReservationService>();

        // Setup default reservation service behavior
        _reservationServiceMock.Setup(r => r.CreateBatchReservationsAsync(It.IsAny<CreateBatchReservationsDto>(), It.IsAny<int>()))
            .ReturnsAsync(new List<StockReservationDto>());
        _reservationServiceMock.Setup(r => r.ReleaseByReferenceAsync(It.IsAny<ReservationType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<StockReservationDto>());
        _reservationServiceMock.Setup(r => r.FulfillByReferenceAsync(It.IsAny<ReservationType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<StockReservationDto>());

        _service = new StockTransferService(
            _requestRepoMock.Object,
            _lineRepoMock.Object,
            _shipmentRepoMock.Object,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            _reservationServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRequestRepository_ThrowsArgumentNullException()
    {
        var action = () => new StockTransferService(
            null!,
            _lineRepoMock.Object,
            _shipmentRepoMock.Object,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            _reservationServiceMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestRepository");
    }

    [Fact]
    public void Constructor_WithNullLineRepository_ThrowsArgumentNullException()
    {
        var action = () => new StockTransferService(
            _requestRepoMock.Object,
            null!,
            _shipmentRepoMock.Object,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            _reservationServiceMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("lineRepository");
    }

    [Fact]
    public void Constructor_WithNullShipmentRepository_ThrowsArgumentNullException()
    {
        var action = () => new StockTransferService(
            _requestRepoMock.Object,
            _lineRepoMock.Object,
            null!,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            _reservationServiceMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("shipmentRepository");
    }

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        var action = () => new StockTransferService(
            _requestRepoMock.Object,
            _lineRepoMock.Object,
            _shipmentRepoMock.Object,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            null!,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            _reservationServiceMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("storeRepository");
    }

    [Fact]
    public void Constructor_WithNullReservationService_ThrowsArgumentNullException()
    {
        var action = () => new StockTransferService(
            _requestRepoMock.Object,
            _lineRepoMock.Object,
            _shipmentRepoMock.Object,
            _receiptRepoMock.Object,
            _receiptLineRepoMock.Object,
            _issueRepoMock.Object,
            _activityLogRepoMock.Object,
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _userRepoMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reservationService");
    }

    #endregion

    #region Transfer Request Tests

    [Fact]
    public async Task CreateTransferRequestAsync_CreatesRequest()
    {
        // Arrange
        var dto = new CreateTransferRequestDto
        {
            RequestingStoreId = 1,
            SourceLocationId = 2,
            SourceLocationType = TransferLocationType.Warehouse,
            Priority = TransferPriority.High,
            Reason = TransferReason.Replenishment,
            Lines = new List<CreateTransferRequestLineDto>()
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferRequest>());
        _requestRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new StockTransferRequest
            {
                Id = 1,
                RequestNumber = "TR-2024-00001",
                RequestingStoreId = 1,
                SourceLocationId = 2,
                Status = TransferRequestStatus.Draft
            });
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTransferRequestAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(TransferRequestStatus.Draft);
        _requestRepoMock.Verify(r => r.AddAsync(It.IsAny<StockTransferRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetTransferRequestAsync_WithExistingRequest_ReturnsRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Draft,
            Priority = TransferPriority.Normal
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _service.GetTransferRequestAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.RequestNumber.Should().Be("TR-2024-00001");
        result.RequestingStoreName.Should().Be("Store 1");
    }

    [Fact]
    public async Task GetTransferRequestAsync_WithNonExistingRequest_ReturnsNull()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((StockTransferRequest?)null);

        // Act
        var result = await _service.GetTransferRequestAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitRequestAsync_SubmitsDraftRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Draft
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, RequestedQuantity = 10 }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitRequestAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        _requestRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferRequest>(
            req => req.Status == TransferRequestStatus.Submitted)), Times.Once);
    }

    [Fact]
    public async Task SubmitRequestAsync_ThrowsForNonDraftRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            Status = TransferRequestStatus.Submitted
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);

        // Act
        var action = () => _service.SubmitRequestAsync(1, 1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public async Task CancelRequestAsync_CancelsRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Submitted
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelRequestAsync(1, 1, "Out of stock");

        // Assert
        result.Should().NotBeNull();
        _requestRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferRequest>(
            req => req.Status == TransferRequestStatus.Cancelled)), Times.Once);
    }

    #endregion

    #region Approval Tests

    [Fact]
    public async Task ApproveRequestAsync_ApprovesSubmittedRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Submitted
        };

        var line = new TransferRequestLine
        {
            Id = 1,
            TransferRequestId = 1,
            ProductId = 1,
            RequestedQuantity = 10
        };

        var dto = new ApproveTransferRequestDto
        {
            RequestId = 1,
            ApprovalNotes = "Approved",
            Lines = new List<ApproveLineDto>
            {
                new ApproveLineDto { LineId = 1, ApprovedQuantity = 10 }
            }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _lineRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(line);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine> { line });
        _lineRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TransferRequestLine>()))
            .Returns(Task.CompletedTask);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveRequestAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        _lineRepoMock.Verify(r => r.UpdateAsync(It.Is<TransferRequestLine>(
            l => l.ApprovedQuantity == 10)), Times.Once);
    }

    [Fact]
    public async Task RejectRequestAsync_RejectsSubmittedRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Submitted
        };

        var dto = new RejectTransferRequestDto
        {
            RequestId = 1,
            RejectionReason = "Insufficient stock"
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RejectRequestAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        _requestRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferRequest>(
            req => req.Status == TransferRequestStatus.Rejected &&
                   req.RejectionReason == "Insufficient stock")), Times.Once);
    }

    #endregion

    #region Shipment Tests

    [Fact]
    public async Task CreateShipmentAsync_CreatesShipment()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Approved
        };

        var dto = new CreateShipmentDto
        {
            TransferRequestId = 1,
            Carrier = "Internal",
            PackageCount = 5,
            Lines = new List<ShipmentLineDto>()
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _shipmentRepoMock.Setup(r => r.AddAsync(It.IsAny<StockTransferShipment>()))
            .Returns(Task.CompletedTask);
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateShipmentAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.Carrier.Should().Be("Internal");
        _shipmentRepoMock.Verify(r => r.AddAsync(It.IsAny<StockTransferShipment>()), Times.Once);
    }

    [Fact]
    public async Task DispatchShipmentAsync_DispatchesShipmentAndDeductsStock()
    {
        // Arrange
        var shipment = new StockTransferShipment
        {
            Id = 1,
            TransferRequestId = 1,
            ShipmentNumber = "SH-2024-00001"
        };

        var request = new StockTransferRequest
        {
            Id = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Approved
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, ShippedQuantity = 10, IsActive = true },
            new TransferRequestLine { Id = 2, TransferRequestId = 1, ProductId = 2, ShippedQuantity = 5, IsActive = true }
        };

        var inventories = new List<Inventory>
        {
            new Inventory { Id = 1, StoreId = 2, ProductId = 1, CurrentStock = 50, ReservedStock = 10 },
            new Inventory { Id = 2, StoreId = 2, ProductId = 2, CurrentStock = 30, ReservedStock = 5 }
        };

        _shipmentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(shipment);
        _shipmentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferShipment>()))
            .Returns(Task.CompletedTask);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _inventoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(inventories);
        _inventoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DispatchShipmentAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        _shipmentRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferShipment>(
            s => s.ShippedAt.HasValue)), Times.Once);
        _requestRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferRequest>(
            req => req.Status == TransferRequestStatus.InTransit)), Times.Once);
        // Verify stock was deducted (2 inventory updates)
        _inventoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Inventory>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchShipmentAsync_ThrowsForAlreadyDispatchedShipment()
    {
        // Arrange
        var shipment = new StockTransferShipment
        {
            Id = 1,
            TransferRequestId = 1,
            ShipmentNumber = "SH-2024-00001",
            ShippedAt = DateTime.UtcNow.AddHours(-1)
        };

        _shipmentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        // Act
        var action = () => _service.DispatchShipmentAsync(1, 1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already dispatched*");
    }

    #endregion

    #region Pick List Tests

    [Fact]
    public async Task GetPickListAsync_GeneratesPickListForApprovedRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Approved,
            Priority = TransferPriority.High
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, ApprovedQuantity = 10, ShippedQuantity = 0, IsActive = true },
            new TransferRequestLine { Id = 2, TransferRequestId = 1, ProductId = 2, ApprovedQuantity = 5, ShippedQuantity = 0, IsActive = true }
        };

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "P1", Barcode = "12345" },
            new Product { Id = 2, Name = "Product 2", SKU = "P2", Barcode = "67890" }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetPickListAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RequestNumber.Should().Be("TR-2024-00001");
        result.TotalItems.Should().Be(2);
        result.TotalQuantity.Should().Be(15);
        result.PickedQuantity.Should().Be(0);
        result.Status.Should().Be(PickListStatus.Pending);
        result.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPickListAsync_ThrowsForNonApprovedRequest()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            Status = TransferRequestStatus.Draft
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);

        // Act
        var action = () => _service.GetPickListAsync(1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved*");
    }

    [Fact]
    public async Task ConfirmPicksAsync_UpdatesPickedQuantities()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2,
            Status = TransferRequestStatus.Approved,
            Priority = TransferPriority.Normal
        };

        var line = new TransferRequestLine
        {
            Id = 1,
            TransferRequestId = 1,
            ProductId = 1,
            ApprovedQuantity = 10,
            ShippedQuantity = 0,
            IsActive = true
        };

        var dto = new ConfirmAllPicksDto
        {
            TransferRequestId = 1,
            Lines = new List<ConfirmPickDto>
            {
                new ConfirmPickDto { RequestLineId = 1, PickedQuantity = 10 }
            }
        };

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "P1" }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _lineRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(line);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine> { line });
        _lineRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TransferRequestLine>()))
            .Returns(Task.CompletedTask);
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfirmPicksAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        _lineRepoMock.Verify(r => r.UpdateAsync(It.Is<TransferRequestLine>(
            l => l.ShippedQuantity == 10)), Times.Once);
    }

    [Fact]
    public async Task ConfirmPicksAsync_ThrowsWhenPickedExceedsApproved()
    {
        // Arrange
        var request = new StockTransferRequest
        {
            Id = 1,
            Status = TransferRequestStatus.Approved
        };

        var line = new TransferRequestLine
        {
            Id = 1,
            TransferRequestId = 1,
            ApprovedQuantity = 10
        };

        var dto = new ConfirmAllPicksDto
        {
            TransferRequestId = 1,
            Lines = new List<ConfirmPickDto>
            {
                new ConfirmPickDto { RequestLineId = 1, PickedQuantity = 15 } // Exceeds approved
            }
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _lineRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(line);

        // Act
        var action = () => _service.ConfirmPicksAsync(dto, 1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public async Task GetPendingPickListsAsync_ReturnsApprovedRequestsWithoutDispatchedShipments()
    {
        // Arrange
        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestNumber = "TR-001", SourceLocationId = 2, RequestingStoreId = 1, Status = TransferRequestStatus.Approved, Priority = TransferPriority.High },
            new StockTransferRequest { Id = 2, RequestNumber = "TR-002", SourceLocationId = 2, RequestingStoreId = 1, Status = TransferRequestStatus.PartiallyApproved, Priority = TransferPriority.Normal },
            new StockTransferRequest { Id = 3, RequestNumber = "TR-003", SourceLocationId = 2, RequestingStoreId = 1, Status = TransferRequestStatus.InTransit, Priority = TransferPriority.Low }
        };

        var shipments = new List<StockTransferShipment>
        {
            new StockTransferShipment { Id = 1, TransferRequestId = 3, ShippedAt = DateTime.UtcNow } // Only request 3 has been shipped
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, ApprovedQuantity = 10, IsActive = true },
            new TransferRequestLine { Id = 2, TransferRequestId = 2, ProductId = 1, ApprovedQuantity = 5, IsActive = true }
        };

        var products = new List<Product> { new Product { Id = 1, Name = "Product 1", SKU = "P1" } };
        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _requestRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => requests.FirstOrDefault(r => r.Id == id));
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(shipments);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetPendingPickListsAsync(2);

        // Assert
        result.Should().HaveCount(2); // Only requests 1 and 2 (approved without dispatched shipments)
    }

    #endregion

    #region Transfer Document Tests

    [Fact]
    public async Task GenerateTransferDocumentAsync_GeneratesDocument()
    {
        // Arrange
        var shipment = new StockTransferShipment
        {
            Id = 1,
            TransferRequestId = 1,
            ShipmentNumber = "SH-2024-00001",
            ShippedAt = DateTime.UtcNow,
            ShippedByUserId = 1,
            Carrier = "Internal",
            PackageCount = 5
        };

        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine
            {
                Id = 1,
                TransferRequestId = 1,
                ProductId = 1,
                RequestedQuantity = 15,
                ApprovedQuantity = 10,
                ShippedQuantity = 10,
                UnitCost = 100,
                IsActive = true
            }
        };

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "P1", Barcode = "12345" }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1", Address = "123 Main St", Phone = "555-1234" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH", Address = "456 Warehouse Ave", Phone = "555-5678" }
        };

        var users = new List<User>
        {
            new User { Id = 1, Username = "warehouse_user" }
        };

        _shipmentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(shipment);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.GenerateTransferDocumentAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.ShipmentNumber.Should().Be("SH-2024-00001");
        result.RequestNumber.Should().Be("TR-2024-00001");
        result.SourceLocationName.Should().Be("Warehouse");
        result.DestinationStoreName.Should().Be("Store 1");
        result.TotalItems.Should().Be(1);
        result.TotalQuantity.Should().Be(10);
        result.TotalValue.Should().Be(1000);
        result.Lines.Should().HaveCount(1);
        result.Lines.First().LineNumber.Should().Be(1);
    }

    [Fact]
    public async Task GenerateTransferDocumentAsync_ThrowsForNonExistentShipment()
    {
        // Arrange
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((StockTransferShipment?)null);

        // Act
        var action = () => _service.GenerateTransferDocumentAsync(999);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetTransferDocumentForRequestAsync_ReturnsDocumentIfShipmentExists()
    {
        // Arrange
        var shipment = new StockTransferShipment
        {
            Id = 1,
            TransferRequestId = 1,
            ShipmentNumber = "SH-2024-00001",
            ShippedAt = DateTime.UtcNow
        };

        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-2024-00001",
            RequestingStoreId = 1,
            SourceLocationId = 2
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment> { shipment });
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(shipment);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _service.GetTransferDocumentForRequestAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.RequestNumber.Should().Be("TR-2024-00001");
    }

    [Fact]
    public async Task GetTransferDocumentForRequestAsync_ReturnsNullIfNoShipment()
    {
        // Arrange
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());

        // Act
        var result = await _service.GetTransferDocumentForRequestAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Source Location Tests

    [Fact]
    public async Task GetSourceLocationsAsync_ReturnsOtherStores()
    {
        // Arrange
        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1", IsActive = true },
            new Store { Id = 2, Name = "Store 2", Code = "S2", IsActive = true },
            new Store { Id = 3, Name = "Warehouse", Code = "WH", IsActive = true }
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetSourceLocationsAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(s => s.Id == 1);
    }

    [Fact]
    public async Task GetSourceStockAsync_ReturnsAvailableStock()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "P1", CostPrice = 100 },
            new Product { Id = 2, Name = "Product 2", SKU = "P2", CostPrice = 200 }
        };

        var inventories = new List<Inventory>
        {
            new Inventory { ProductId = 1, StoreId = 2, CurrentStock = 50, ReservedStock = 10 },
            new Inventory { ProductId = 2, StoreId = 2, CurrentStock = 30, ReservedStock = 0 }
        };

        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _inventoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(inventories);

        // Act
        var result = await _service.GetSourceStockAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result.First(p => p.ProductId == 1).TransferableQuantity.Should().Be(40);
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetStoreDashboardAsync_ReturnsDashboard()
    {
        // Arrange
        var store = new Store { Id = 1, Name = "Store 1", Code = "S1" };

        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestingStoreId = 1, SourceLocationId = 2, Status = TransferRequestStatus.Draft, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 2, RequestingStoreId = 1, SourceLocationId = 2, Status = TransferRequestStatus.Submitted, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 3, RequestingStoreId = 2, SourceLocationId = 1, Status = TransferRequestStatus.Submitted, CreatedAt = DateTime.UtcNow }
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store> { store, new Store { Id = 2, Name = "Store 2", Code = "S2" } });
        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferReceiptIssue>());

        // Act
        var result = await _service.GetStoreDashboardAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.StoreName.Should().Be("Store 1");
        result.OutgoingDraftCount.Should().Be(1);
        result.OutgoingSubmittedCount.Should().Be(1);
        result.IncomingPendingApprovalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChainDashboardAsync_ReturnsDashboard()
    {
        // Arrange
        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestingStoreId = 1, Status = TransferRequestStatus.Submitted, Priority = TransferPriority.High, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 2, RequestingStoreId = 2, Status = TransferRequestStatus.InTransit, Priority = TransferPriority.Normal, TotalEstimatedValue = 1000, CreatedAt = DateTime.UtcNow }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Store 2", Code = "S2" }
        };

        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferRequestLine>());
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferReceiptIssue>());

        // Act
        var result = await _service.GetChainDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveTransfers.Should().Be(2);
        result.TotalPendingApprovals.Should().Be(1);
        result.TotalInTransit.Should().Be(1);
        result.TotalValueInTransit.Should().Be(1000);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, Status = TransferRequestStatus.Received, TotalItemsApproved = 50, TotalEstimatedValue = 5000, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 2, Status = TransferRequestStatus.Received, TotalItemsApproved = 30, TotalEstimatedValue = 3000, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 3, Status = TransferRequestStatus.Cancelled, CreatedAt = DateTime.UtcNow },
            new StockTransferRequest { Id = 4, Status = TransferRequestStatus.Rejected, CreatedAt = DateTime.UtcNow }
        };

        var issues = new List<TransferReceiptIssue>
        {
            new TransferReceiptIssue { Id = 1, IsResolved = true },
            new TransferReceiptIssue { Id = 2, IsResolved = false }
        };

        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(issues);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(4);
        result.CompletedRequests.Should().Be(2);
        result.CancelledRequests.Should().Be(1);
        result.RejectedRequests.Should().Be(1);
        result.TotalItemsTransferred.Should().Be(80);
        result.TotalValueTransferred.Should().Be(8000);
        result.ResolvedIssues.Should().Be(1);
    }

    #endregion

    #region Number Generation Tests

    [Fact]
    public async Task GenerateRequestNumberAsync_GeneratesUniqueNumber()
    {
        // Arrange
        var existingRequests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestNumber = "TR-2024-00001", CreatedAt = DateTime.UtcNow }
        };

        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(existingRequests);

        // Act
        var result = await _service.GenerateRequestNumberAsync();

        // Assert
        result.Should().StartWith("TR-");
        result.Should().Contain("-00002");
    }

    [Fact]
    public async Task GenerateShipmentNumberAsync_GeneratesUniqueNumber()
    {
        // Arrange
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferShipment>());

        // Act
        var result = await _service.GenerateShipmentNumberAsync();

        // Assert
        result.Should().StartWith("SH-");
        result.Should().Contain("-00001");
    }

    [Fact]
    public async Task GenerateReceiptNumberAsync_GeneratesUniqueNumber()
    {
        // Arrange
        _receiptRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<StockTransferReceipt>());

        // Act
        var result = await _service.GenerateReceiptNumberAsync();

        // Assert
        result.Should().StartWith("RC-");
        result.Should().Contain("-00001");
    }

    #endregion

    #region Receipt and Receiving Tests

    [Fact]
    public async Task GetPendingReceiptsAsync_ReturnsInTransitRequests()
    {
        // Arrange
        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestNumber = "TR-001", RequestingStoreId = 1, SourceLocationId = 2, Status = TransferRequestStatus.InTransit, Priority = TransferPriority.High },
            new StockTransferRequest { Id = 2, RequestNumber = "TR-002", RequestingStoreId = 1, SourceLocationId = 2, Status = TransferRequestStatus.InTransit, Priority = TransferPriority.Normal },
            new StockTransferRequest { Id = 3, RequestNumber = "TR-003", RequestingStoreId = 1, SourceLocationId = 2, Status = TransferRequestStatus.Received, Priority = TransferPriority.Low }
        };

        var shipments = new List<StockTransferShipment>
        {
            new StockTransferShipment { Id = 1, TransferRequestId = 1, ShippedAt = DateTime.UtcNow.AddDays(-2), ShipmentNumber = "SH-001", DriverName = "Driver A" },
            new StockTransferShipment { Id = 2, TransferRequestId = 2, ShippedAt = DateTime.UtcNow.AddDays(-1), ShipmentNumber = "SH-002", DriverName = "Driver B" }
        };

        var lines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, ShippedQuantity = 10, UnitCost = 100, IsActive = true },
            new TransferRequestLine { Id = 2, TransferRequestId = 2, ProductId = 1, ShippedQuantity = 5, UnitCost = 100, IsActive = true }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _shipmentRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(shipments);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(lines);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetPendingReceiptsAsync(1);

        // Assert
        result.Should().HaveCount(2); // Only in-transit requests
        result.First().RequestNumber.Should().Be("TR-001");
        result.First().SourceLocationName.Should().Be("Warehouse");
    }

    [Fact]
    public async Task GetReceivingSummaryAsync_ReturnsVarianceDetails()
    {
        // Arrange
        var receipt = new StockTransferReceipt
        {
            Id = 1,
            TransferRequestId = 1,
            ReceiptNumber = "RC-001",
            ReceivedAt = DateTime.UtcNow,
            ReceivedByUserId = 1,
            IsComplete = false
        };

        var request = new StockTransferRequest
        {
            Id = 1,
            RequestNumber = "TR-001",
            RequestingStoreId = 1,
            SourceLocationId = 2
        };

        var receiptLines = new List<TransferReceiptLine>
        {
            new TransferReceiptLine { Id = 1, TransferReceiptId = 1, TransferRequestLineId = 1, ProductId = 1, ExpectedQuantity = 10, ReceivedQuantity = 8, IssueQuantity = 2 },
            new TransferReceiptLine { Id = 2, TransferReceiptId = 1, TransferRequestLineId = 2, ProductId = 2, ExpectedQuantity = 5, ReceivedQuantity = 5, IssueQuantity = 0 }
        };

        var requestLines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, ProductId = 1, UnitCost = 100 },
            new TransferRequestLine { Id = 2, ProductId = 2, UnitCost = 50 }
        };

        var issues = new List<TransferReceiptIssue>
        {
            new TransferReceiptIssue { Id = 1, TransferReceiptId = 1, TransferReceiptLineId = 1, IssueType = TransferIssueType.Shortage, AffectedQuantity = 2, IsResolved = false }
        };

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "P1" },
            new Product { Id = 2, Name = "Product 2", SKU = "P2" }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Name = "Store 1", Code = "S1" },
            new Store { Id = 2, Name = "Warehouse", Code = "WH" }
        };

        var users = new List<User> { new User { Id = 1, Username = "receiver" } };

        _receiptRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(receipt);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _receiptLineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(receiptLines);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requestLines);
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(issues);
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetReceivingSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptNumber.Should().Be("RC-001");
        result.TotalExpected.Should().Be(15);
        result.TotalReceived.Should().Be(13);
        result.TotalVariance.Should().Be(-2); // Shortage
        result.HasShortage.Should().BeTrue();
        result.UnresolvedIssueCount.Should().Be(1);
        result.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReceiptsWithVarianceAsync_ReturnsOnlyVarianceReceipts()
    {
        // Arrange
        var receipts = new List<StockTransferReceipt>
        {
            new StockTransferReceipt { Id = 1, TransferRequestId = 1, ReceiptNumber = "RC-001", ReceivedAt = DateTime.UtcNow, IsActive = true },
            new StockTransferReceipt { Id = 2, TransferRequestId = 2, ReceiptNumber = "RC-002", ReceivedAt = DateTime.UtcNow, IsActive = true }
        };

        var requests = new List<StockTransferRequest>
        {
            new StockTransferRequest { Id = 1, RequestNumber = "TR-001", RequestingStoreId = 1 },
            new StockTransferRequest { Id = 2, RequestNumber = "TR-002", RequestingStoreId = 1 }
        };

        var receiptLines = new List<TransferReceiptLine>
        {
            new TransferReceiptLine { Id = 1, TransferReceiptId = 1, TransferRequestLineId = 1, ProductId = 1, ExpectedQuantity = 10, ReceivedQuantity = 8 }, // Variance
            new TransferReceiptLine { Id = 2, TransferReceiptId = 2, TransferRequestLineId = 2, ProductId = 1, ExpectedQuantity = 5, ReceivedQuantity = 5 } // No variance
        };

        var requestLines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, ProductId = 1, UnitCost = 100 },
            new TransferRequestLine { Id = 2, ProductId = 2, UnitCost = 50 }
        };

        _receiptRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(receipts);
        _requestRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requests);
        _receiptLineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(receiptLines);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requestLines);
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferReceiptIssue>());
        _productRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product> { new Product { Id = 1, Name = "Product 1", SKU = "P1" } });
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store>());
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _service.GetReceiptsWithVarianceAsync();

        // Assert
        result.Should().HaveCount(1); // Only receipt 1 has variance
        result.First().ReceiptNumber.Should().Be("RC-001");
        result.First().TotalVarianceQuantity.Should().Be(-2);
    }

    [Fact]
    public async Task CompleteReceiptAsync_UpdatesInventoryAndStatus()
    {
        // Arrange
        var receipt = new StockTransferReceipt
        {
            Id = 1,
            TransferRequestId = 1,
            ReceiptNumber = "RC-001"
        };

        var request = new StockTransferRequest
        {
            Id = 1,
            RequestingStoreId = 1,
            Status = TransferRequestStatus.InTransit
        };

        var requestLines = new List<TransferRequestLine>
        {
            new TransferRequestLine { Id = 1, TransferRequestId = 1, ProductId = 1, ShippedQuantity = 10, ReceivedQuantity = 10, IsActive = true }
        };

        var inventories = new List<Inventory>
        {
            new Inventory { Id = 1, StoreId = 1, ProductId = 1, CurrentStock = 0 }
        };

        var stores = new List<Store> { new Store { Id = 1, Name = "Store 1", Code = "S1" } };

        _receiptRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(receipt);
        _receiptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferReceipt>()))
            .Returns(Task.CompletedTask);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(It.IsAny<StockTransferRequest>()))
            .Returns(Task.CompletedTask);
        _lineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(requestLines);
        _inventoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(inventories);
        _inventoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
            .Returns(Task.CompletedTask);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>());
        _receiptLineRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferReceiptLine>());
        _issueRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TransferReceiptIssue>());
        _activityLogRepoMock.Setup(r => r.AddAsync(It.IsAny<TransferActivityLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CompleteReceiptAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        _receiptRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferReceipt>(
            rec => rec.IsComplete == true)), Times.Once);
        _requestRepoMock.Verify(r => r.UpdateAsync(It.Is<StockTransferRequest>(
            req => req.Status == TransferRequestStatus.Received)), Times.Once);
        _inventoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Inventory>()), Times.Once);
    }

    #endregion
}
