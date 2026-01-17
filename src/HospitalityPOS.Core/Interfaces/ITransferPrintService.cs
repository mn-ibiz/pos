namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for printing stock transfer documents.
/// </summary>
public interface ITransferPrintService
{
    /// <summary>
    /// Prints a transfer request document.
    /// </summary>
    /// <param name="transferRequestId">The ID of the transfer request to print.</param>
    /// <returns>True if the print job was successful.</returns>
    Task<bool> PrintTransferRequestAsync(int transferRequestId);

    /// <summary>
    /// Prints a transfer receipt document.
    /// </summary>
    /// <param name="transferReceiptId">The ID of the transfer receipt to print.</param>
    /// <returns>True if the print job was successful.</returns>
    Task<bool> PrintTransferReceiptAsync(int transferReceiptId);

    /// <summary>
    /// Prints a transfer shipment label.
    /// </summary>
    /// <param name="transferRequestId">The ID of the transfer request.</param>
    /// <param name="copies">Number of copies to print.</param>
    /// <returns>True if the print job was successful.</returns>
    Task<bool> PrintShipmentLabelAsync(int transferRequestId, int copies = 1);

    /// <summary>
    /// Prints a packing list for a transfer.
    /// </summary>
    /// <param name="transferRequestId">The ID of the transfer request.</param>
    /// <returns>True if the print job was successful.</returns>
    Task<bool> PrintPackingListAsync(int transferRequestId);
}
