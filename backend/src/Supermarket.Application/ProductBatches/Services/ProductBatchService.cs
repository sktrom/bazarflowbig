using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Contracts.ProductBatches;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.ProductBatches.Services
{
    public class ProductBatchService : IProductBatchService
    {
        private readonly IBatchManagementRepository _repository;
        private readonly ISessionContext _sessionContext;

        public ProductBatchService(IBatchManagementRepository repository, ISessionContext sessionContext)
        {
            _repository = repository;
            _sessionContext = sessionContext;
        }

        public async Task<BatchListResponse> GetAllByProductIdAsync(long productId)
        {
            if (!await _repository.ProductExistsAsync(productId))
                throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            var batches = await _repository.GetAllByProductIdAsync(productId);
            return new BatchListResponse
            {
                Items = batches.Select(b => new BatchListItem
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    QuantityReceived = b.QuantityReceived,
                    QuantityAvailable = b.QuantityAvailable,
                    EntryDate = b.EntryDate,
                    ExpiryDate = b.ExpiryDate,
                    EntryInvoiceNumber = b.EntryInvoiceNumber,
                    EnteredByEmployeeId = b.EnteredByEmployeeId
                }).ToList()
            };
        }

        public async Task<BatchDetailResponse> CreateAsync(long productId, CreateBatchRequest request)
        {
            if (request.QuantityReceived < 0 || request.QuantityAvailable < 0)
                throw new InvalidOperationException("VALIDATION_ERROR");

            if (!await _repository.ProductExistsAsync(productId))
                throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            // Ensure we have a valid session employee
            if (_sessionContext.EmployeeId <= 0)
                throw new InvalidOperationException("NO_ACTIVE_SESSION");

            var batch = new ProductBatch
            {
                ProductId = productId,
                QuantityReceived = request.QuantityReceived,
                QuantityAvailable = request.QuantityAvailable,
                EntryDate = request.EntryDate,
                ExpiryDate = request.ExpiryDate,
                EntryInvoiceNumber = request.EntryInvoiceNumber,
                EnteredByEmployeeId = _sessionContext.EmployeeId
            };

            var created = await _repository.CreateAsync(batch);

            return new BatchDetailResponse
            {
                Id = created.Id,
                ProductId = created.ProductId,
                QuantityReceived = created.QuantityReceived,
                QuantityAvailable = created.QuantityAvailable,
                EntryDate = created.EntryDate,
                ExpiryDate = created.ExpiryDate,
                EntryInvoiceNumber = created.EntryInvoiceNumber,
                EnteredByEmployeeId = created.EnteredByEmployeeId
            };
        }
    }
}
