using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.InventoryQueries
{
    public class ActionCenterResponseDto
    {
        public ActionCenterSummaryDto Summary { get; set; } = new();
        public List<TopUrgentActionDto> TopUrgentActions { get; set; } = new();
        
        public List<ProductActionItemDto> OutOfStock { get; set; } = new();
        public List<ProductActionItemDto> LowStock { get; set; } = new();
        public List<BatchActionItemDto> ExpiringSoon { get; set; } = new();
        public List<BatchActionItemDto> Expired { get; set; } = new();
        public List<ProductActionItemDto> InactiveWithStock { get; set; } = new();
        public List<ProductActionItemDto> OfferCandidates { get; set; } = new();
        public List<RestockSuggestionDto> RestockSuggestions { get; set; } = new();
    }

    public class ActionCenterSummaryDto
    {
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiredBatchesCount { get; set; }
        public int ExpiringSoonBatchesCount { get; set; }
        public int InactiveWithStockCount { get; set; }
        public int OfferCandidatesCount { get; set; }
        public int RestockSuggestionsCount { get; set; }
    }

    public class TopUrgentActionDto
    {
        public string Type { get; set; } = string.Empty; 
        public string Severity { get; set; } = string.Empty; 
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class ProductActionItemDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
    }

    public class BatchActionItemDto : ProductActionItemDto
    {
        public long BatchId { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class RestockSuggestionDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal SoldLast30Days { get; set; }
        public decimal AvgDailySales { get; set; }
        public decimal? DaysRemaining { get; set; }
        public decimal SuggestedQty { get; set; }
        public string Confidence { get; set; } = string.Empty;
        public string RecommendationType { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }
}
