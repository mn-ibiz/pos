using System.Text.RegularExpressions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for barcode, PLU, and scale operations.
/// </summary>
public class BarcodeService : IBarcodeService
{
    private readonly POSDbContext _context;
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(POSDbContext context, ILogger<BarcodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Barcode Lookup

    public async Task<BarcodeLookupResult> LookupBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return new BarcodeLookupResult { Found = false, ErrorMessage = "Barcode is empty" };
        }

        barcode = barcode.Trim();

        // Check if it's a weighted/priced barcode
        if (barcode.Length >= 12 && int.TryParse(barcode.Substring(0, 2), out var prefix) && prefix >= 20 && prefix <= 29)
        {
            var weightedResult = await ParseWeightedBarcodeAsync(barcode, cancellationToken);
            if (weightedResult.Success && !string.IsNullOrEmpty(weightedResult.ArticleCode))
            {
                // Look up by article code in PLU codes
                var plu = await GetPLUCodeAsync(weightedResult.ArticleCode, cancellationToken);
                if (plu != null)
                {
                    return new BarcodeLookupResult
                    {
                        Found = true,
                        Product = plu.Product,
                        PLUCode = plu,
                        IsWeighted = plu.IsWeighted,
                        Weight = weightedResult.IsPrice ? null : weightedResult.Value,
                        EmbeddedPrice = weightedResult.IsPrice ? weightedResult.Value : null,
                        TareWeight = plu.TareWeight,
                        Quantity = weightedResult.IsPrice ? 1 : (weightedResult.Value ?? 1),
                        DetectedType = BarcodeType.Internal
                    };
                }

                // Try looking up by article code as barcode
                var productBarcode = await _context.Set<ProductBarcode>()
                    .Include(pb => pb.Product)
                    .FirstOrDefaultAsync(pb => pb.Barcode.Contains(weightedResult.ArticleCode), cancellationToken);

                if (productBarcode != null)
                {
                    return new BarcodeLookupResult
                    {
                        Found = true,
                        Product = productBarcode.Product,
                        ProductBarcode = productBarcode,
                        IsWeighted = true,
                        Weight = weightedResult.IsPrice ? null : weightedResult.Value,
                        EmbeddedPrice = weightedResult.IsPrice ? weightedResult.Value : null,
                        Quantity = weightedResult.IsPrice ? 1 : (weightedResult.Value ?? 1),
                        DetectedType = BarcodeType.Internal
                    };
                }
            }
        }

        // Standard barcode lookup
        var standardBarcode = await _context.Set<ProductBarcode>()
            .Include(pb => pb.Product)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode, cancellationToken);

        if (standardBarcode != null)
        {
            return new BarcodeLookupResult
            {
                Found = true,
                Product = standardBarcode.Product,
                ProductBarcode = standardBarcode,
                Quantity = standardBarcode.PackSize,
                DetectedType = standardBarcode.BarcodeType
            };
        }

        // Try PLU lookup
        var pluLookup = await GetPLUCodeAsync(barcode, cancellationToken);
        if (pluLookup != null)
        {
            return new BarcodeLookupResult
            {
                Found = true,
                Product = pluLookup.Product,
                PLUCode = pluLookup,
                IsWeighted = pluLookup.IsWeighted,
                TareWeight = pluLookup.TareWeight,
                DetectedType = BarcodeType.Internal
            };
        }

        // Try product SKU
        var productBySku = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == barcode && p.IsActive, cancellationToken);

        if (productBySku != null)
        {
            return new BarcodeLookupResult
            {
                Found = true,
                Product = productBySku,
                DetectedType = BarcodeType.Internal
            };
        }

        return new BarcodeLookupResult
        {
            Found = false,
            ErrorMessage = $"Product not found for barcode: {barcode}"
        };
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var result = await LookupBarcodeAsync(barcode, cancellationToken);
        return result.Product;
    }

    public async Task<Product?> GetProductByPLUAsync(string pluCode, CancellationToken cancellationToken = default)
    {
        var plu = await GetPLUCodeAsync(pluCode, cancellationToken);
        return plu?.Product;
    }

    #endregion

    #region Product Barcodes

    public async Task<ProductBarcode> AddProductBarcodeAsync(int productId, string barcode, BarcodeType type, bool isPrimary = false, decimal packSize = 1, CancellationToken cancellationToken = default)
    {
        // Validate barcode format
        if (!await ValidateBarcodeAsync(barcode, productId, cancellationToken))
        {
            throw new InvalidOperationException($"Barcode {barcode} is invalid or already exists.");
        }

        // If setting as primary, unset existing primary
        if (isPrimary)
        {
            var existingPrimary = await _context.Set<ProductBarcode>()
                .Where(pb => pb.ProductId == productId && pb.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var pb in existingPrimary)
            {
                pb.IsPrimary = false;
            }
        }

        var productBarcode = new ProductBarcode
        {
            ProductId = productId,
            Barcode = barcode,
            BarcodeType = type,
            IsPrimary = isPrimary,
            PackSize = packSize
        };

        _context.Set<ProductBarcode>().Add(productBarcode);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added barcode {Barcode} to product {ProductId}", barcode, productId);
        return productBarcode;
    }

    public async Task<bool> RemoveProductBarcodeAsync(int barcodeId, CancellationToken cancellationToken = default)
    {
        var barcode = await _context.Set<ProductBarcode>().FindAsync(new object[] { barcodeId }, cancellationToken);
        if (barcode == null) return false;

        _context.Set<ProductBarcode>().Remove(barcode);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductBarcode>> GetProductBarcodesAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductBarcode>()
            .Where(pb => pb.ProductId == productId)
            .OrderByDescending(pb => pb.IsPrimary)
            .ThenBy(pb => pb.Barcode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ValidateBarcodeAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;

        // Check for duplicates
        var query = _context.Set<ProductBarcode>().Where(pb => pb.Barcode == barcode);
        if (excludeProductId.HasValue)
        {
            query = query.Where(pb => pb.ProductId != excludeProductId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        if (exists) return false;

        // Validate format based on length
        return barcode.Length switch
        {
            13 => IsValidEAN13(barcode),
            8 => IsValidEAN8(barcode),
            12 => IsValidUPCA(barcode),
            _ => barcode.All(char.IsDigit) // Allow any numeric barcode
        };
    }

    #endregion

    #region PLU Management

    public async Task<PLUCode> AddPLUCodeAsync(int productId, string code, bool isWeighted = false, decimal? tareWeight = null, CancellationToken cancellationToken = default)
    {
        if (!await ValidatePLUCodeAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException($"PLU code {code} is invalid or already exists.");
        }

        var plu = new PLUCode
        {
            ProductId = productId,
            Code = code,
            IsWeighted = isWeighted,
            TareWeight = tareWeight,
            IsActive = true
        };

        _context.Set<PLUCode>().Add(plu);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added PLU code {Code} to product {ProductId}", code, productId);
        return plu;
    }

    public async Task<PLUCode> UpdatePLUCodeAsync(PLUCode pluCode, CancellationToken cancellationToken = default)
    {
        _context.Set<PLUCode>().Update(pluCode);
        await _context.SaveChangesAsync(cancellationToken);
        return pluCode;
    }

    public async Task<bool> DeletePLUCodeAsync(int pluId, CancellationToken cancellationToken = default)
    {
        var plu = await _context.Set<PLUCode>().FindAsync(new object[] { pluId }, cancellationToken);
        if (plu == null) return false;

        _context.Set<PLUCode>().Remove(plu);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PLUCode>> GetAllPLUCodesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<PLUCode>()
            .Include(p => p.Product)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<PLUCode?> GetPLUCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PLUCode>()
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Code == code && p.IsActive, cancellationToken);
    }

    public async Task<bool> ValidatePLUCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (!Regex.IsMatch(code, @"^\d{4,6}$")) return false;

        var query = _context.Set<PLUCode>().Where(p => p.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<string> GenerateNextPLUCodeAsync(CancellationToken cancellationToken = default)
    {
        var lastPlu = await _context.Set<PLUCode>()
            .Where(p => p.Code.Length == 4 && p.Code.All(char.IsDigit))
            .OrderByDescending(p => p.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1001;
        if (lastPlu != null && int.TryParse(lastPlu.Code, out var lastNumber))
        {
            nextNumber = lastNumber + 1;
        }

        return nextNumber.ToString("D4");
    }

    #endregion

    #region Weighted Barcode Config

    public async Task<WeightedBarcodeConfig> SaveWeightedBarcodeConfigAsync(WeightedBarcodeConfig config, CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            _context.Set<WeightedBarcodeConfig>().Add(config);
        }
        else
        {
            _context.Set<WeightedBarcodeConfig>().Update(config);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<IReadOnlyList<WeightedBarcodeConfig>> GetWeightedBarcodeConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WeightedBarcodeConfig>()
            .OrderBy(c => c.Prefix)
            .ToListAsync(cancellationToken);
    }

    public async Task<WeightedBarcodeConfig?> GetWeightedBarcodeConfigAsync(WeightedBarcodePrefix prefix, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WeightedBarcodeConfig>()
            .FirstOrDefaultAsync(c => c.Prefix == prefix && c.IsActive, cancellationToken);
    }

    public async Task<WeightedBarcodeParseResult> ParseWeightedBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (barcode.Length < 12 || !int.TryParse(barcode.Substring(0, 2), out var prefixValue))
        {
            return new WeightedBarcodeParseResult { Success = false, ErrorMessage = "Invalid barcode format" };
        }

        if (!Enum.IsDefined(typeof(WeightedBarcodePrefix), prefixValue) || prefixValue == 0)
        {
            return new WeightedBarcodeParseResult { Success = false, ErrorMessage = "Not a weighted barcode" };
        }

        var prefix = (WeightedBarcodePrefix)prefixValue;
        var config = await GetWeightedBarcodeConfigAsync(prefix, cancellationToken);

        if (config == null)
        {
            // Use default parsing
            config = new WeightedBarcodeConfig
            {
                Prefix = prefix,
                Format = prefix >= WeightedBarcodePrefix.Prefix23 ? WeightedBarcodeFormat.StandardWeight : WeightedBarcodeFormat.StandardPrice,
                ArticleCodeStart = 2,
                ArticleCodeLength = 5,
                ValueStart = 7,
                ValueLength = 5,
                ValueDecimals = prefix >= WeightedBarcodePrefix.Prefix23 ? 3 : 2,
                IsPrice = prefix < WeightedBarcodePrefix.Prefix23
            };
        }

        try
        {
            var articleCode = barcode.Substring(config.ArticleCodeStart, config.ArticleCodeLength);
            var valueStr = barcode.Substring(config.ValueStart, config.ValueLength);

            if (!decimal.TryParse(valueStr, out var value))
            {
                return new WeightedBarcodeParseResult { Success = false, ErrorMessage = "Cannot parse value" };
            }

            // Apply decimal places
            value /= (decimal)Math.Pow(10, config.ValueDecimals);

            return new WeightedBarcodeParseResult
            {
                Success = true,
                ArticleCode = articleCode.TrimStart('0'),
                Value = value,
                IsPrice = config.IsPrice,
                Prefix = prefix
            };
        }
        catch (Exception ex)
        {
            return new WeightedBarcodeParseResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region Scale Configuration

    public async Task<ScaleConfiguration> SaveScaleConfigurationAsync(ScaleConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            _context.Set<ScaleConfiguration>().Add(config);
        }
        else
        {
            _context.Set<ScaleConfiguration>().Update(config);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<ScaleConfiguration?> GetActiveScaleConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ScaleConfiguration>()
            .FirstOrDefaultAsync(s => s.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<ScaleConfiguration>> GetAllScaleConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ScaleConfiguration>()
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ActivateScaleAsync(int scaleId, CancellationToken cancellationToken = default)
    {
        var scales = await _context.Set<ScaleConfiguration>().ToListAsync(cancellationToken);

        foreach (var scale in scales)
        {
            scale.IsActive = scale.Id == scaleId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TestScaleConnectionAsync(int scaleId, CancellationToken cancellationToken = default)
    {
        var scale = await _context.Set<ScaleConfiguration>().FindAsync(new object[] { scaleId }, cancellationToken);
        if (scale == null) return false;

        // Simulate connection test
        await Task.Delay(500, cancellationToken);

        scale.LastStatus = ScaleStatus.Connected;
        scale.LastConnectedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scale {ScaleName} connection test successful", scale.Name);
        return true;
    }

    #endregion

    #region Scale Reading

    public async Task<ScaleReadResult> ReadScaleAsync(CancellationToken cancellationToken = default)
    {
        var scale = await GetActiveScaleConfigurationAsync(cancellationToken);

        if (scale == null)
        {
            return new ScaleReadResult
            {
                Success = false,
                Status = ScaleStatus.Disconnected,
                ErrorMessage = "No active scale configured"
            };
        }

        // Simulate scale reading (in real implementation, this would communicate with hardware)
        await Task.Delay(100, cancellationToken);

        // For demo purposes, return a simulated weight
        return new ScaleReadResult
        {
            Success = true,
            Weight = 0.000m, // Would come from actual scale
            Unit = scale.WeightUnit,
            IsStable = true,
            Status = ScaleStatus.Connected
        };
    }

    public async Task<decimal> TareScaleAsync(CancellationToken cancellationToken = default)
    {
        // Simulate tare operation
        await Task.Delay(200, cancellationToken);
        _logger.LogInformation("Scale tared");
        return 0;
    }

    public async Task<bool> ZeroScaleAsync(CancellationToken cancellationToken = default)
    {
        // Simulate zero operation
        await Task.Delay(200, cancellationToken);
        _logger.LogInformation("Scale zeroed");
        return true;
    }

    #endregion

    #region Internal Barcode Generation

    public async Task<string> GenerateInternalBarcodeAsync(int productId, CancellationToken cancellationToken = default)
    {
        var sequence = await GetBarcodeSequenceAsync(cancellationToken);

        sequence.LastSequenceNumber++;
        var sequenceStr = sequence.LastSequenceNumber.ToString().PadLeft(sequence.SequenceDigits, '0');
        var barcodeWithoutCheck = sequence.Prefix + sequenceStr;

        // Calculate check digit
        var checkDigit = CalculateCheckDigit(barcodeWithoutCheck);
        var fullBarcode = barcodeWithoutCheck + checkDigit;

        await UpdateBarcodeSequenceAsync(sequence, cancellationToken);

        // Add barcode to product
        await AddProductBarcodeAsync(productId, fullBarcode, BarcodeType.Internal, true, 1, cancellationToken);

        _logger.LogInformation("Generated internal barcode {Barcode} for product {ProductId}", fullBarcode, productId);
        return fullBarcode;
    }

    public async Task<InternalBarcodeSequence> GetBarcodeSequenceAsync(CancellationToken cancellationToken = default)
    {
        var sequence = await _context.Set<InternalBarcodeSequence>().FirstOrDefaultAsync(cancellationToken);

        if (sequence == null)
        {
            sequence = new InternalBarcodeSequence
            {
                Prefix = "200",
                LastSequenceNumber = 0,
                SequenceDigits = 9
            };
            _context.Set<InternalBarcodeSequence>().Add(sequence);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sequence;
    }

    public async Task<InternalBarcodeSequence> UpdateBarcodeSequenceAsync(InternalBarcodeSequence sequence, CancellationToken cancellationToken = default)
    {
        _context.Set<InternalBarcodeSequence>().Update(sequence);
        await _context.SaveChangesAsync(cancellationToken);
        return sequence;
    }

    #endregion

    #region Barcode Validation

    public bool IsValidEAN13(string barcode)
    {
        if (barcode.Length != 13 || !barcode.All(char.IsDigit)) return false;

        var check = CalculateCheckDigit(barcode.Substring(0, 12));
        return check == barcode[12].ToString();
    }

    public bool IsValidEAN8(string barcode)
    {
        if (barcode.Length != 8 || !barcode.All(char.IsDigit)) return false;

        var check = CalculateEAN8CheckDigit(barcode.Substring(0, 7));
        return check == barcode[7].ToString();
    }

    public bool IsValidUPCA(string barcode)
    {
        if (barcode.Length != 12 || !barcode.All(char.IsDigit)) return false;

        var check = CalculateUPCACheckDigit(barcode.Substring(0, 11));
        return check == barcode[11].ToString();
    }

    public string CalculateCheckDigit(string barcode)
    {
        if (barcode.Length != 12) throw new ArgumentException("Barcode must be 12 digits for EAN-13 check digit");

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = int.Parse(barcode[i].ToString());
            sum += digit * (i % 2 == 0 ? 1 : 3);
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    private static string CalculateEAN8CheckDigit(string barcode)
    {
        if (barcode.Length != 7) throw new ArgumentException("Barcode must be 7 digits for EAN-8 check digit");

        var sum = 0;
        for (var i = 0; i < 7; i++)
        {
            var digit = int.Parse(barcode[i].ToString());
            sum += digit * (i % 2 == 0 ? 3 : 1);
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    private static string CalculateUPCACheckDigit(string barcode)
    {
        if (barcode.Length != 11) throw new ArgumentException("Barcode must be 11 digits for UPC-A check digit");

        var sum = 0;
        for (var i = 0; i < 11; i++)
        {
            var digit = int.Parse(barcode[i].ToString());
            sum += digit * (i % 2 == 0 ? 3 : 1);
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    #endregion
}
