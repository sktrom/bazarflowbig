using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Suppliers.Interfaces;
using Supermarket.Contracts.Suppliers;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Suppliers.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _repository;

        public SupplierService(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<SupplierListResponse> GetAllAsync()
        {
            var suppliers = await _repository.GetAllAsync();

            return new SupplierListResponse
            {
                Items = suppliers.Select(s => new SupplierListItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Phone = s.Phone,
                    Email = s.Email,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList()
            };
        }

        public async Task<SupplierDetailResponse> GetByIdAsync(long id)
        {
            var supplier = await _repository.GetByIdAsync(id);
            if (supplier == null) throw new InvalidOperationException("SUPPLIER_NOT_FOUND");

            return MapToDetailResponse(supplier);
        }

        public async Task<SupplierDetailResponse> CreateAsync(CreateSupplierRequest request)
        {
            var name = NormalizeRequiredName(request.Name);

            if (await _repository.ActiveNameExistsAsync(name))
                throw new InvalidOperationException("SUPPLIER_NAME_ALREADY_EXISTS");

            var now = DateTime.UtcNow;
            var supplier = new Supplier
            {
                Name = name,
                Phone = NormalizeOptional(request.Phone),
                Email = NormalizeOptional(request.Email),
                Address = NormalizeOptional(request.Address),
                Notes = NormalizeOptional(request.Notes),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.CreateAsync(supplier);
            return MapToDetailResponse(created);
        }

        public async Task<SupplierDetailResponse> UpdateAsync(long id, UpdateSupplierRequest request)
        {
            var supplier = await _repository.GetByIdAsync(id);
            if (supplier == null) throw new InvalidOperationException("SUPPLIER_NOT_FOUND");

            var name = NormalizeRequiredName(request.Name);

            if (request.IsActive && await _repository.ActiveNameExistsAsync(name, id))
                throw new InvalidOperationException("SUPPLIER_NAME_ALREADY_EXISTS");

            supplier.Name = name;
            supplier.Phone = NormalizeOptional(request.Phone);
            supplier.Email = NormalizeOptional(request.Email);
            supplier.Address = NormalizeOptional(request.Address);
            supplier.Notes = NormalizeOptional(request.Notes);
            supplier.IsActive = request.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(supplier);
            return MapToDetailResponse(supplier);
        }

        public async Task<DeleteSupplierResponse> DeleteAsync(long id)
        {
            var supplier = await _repository.GetByIdAsync(id);
            if (supplier == null) throw new InvalidOperationException("SUPPLIER_NOT_FOUND");

            if (await _repository.IsSupplierUsedAsync(id))
            {
                supplier.IsActive = false;
                supplier.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(supplier);

                return new DeleteSupplierResponse
                {
                    Success = true,
                    Action = "DEACTIVATED",
                    Message = "Supplier is used by purchase records and was deactivated instead of deleted."
                };
            }

            await _repository.DeleteAsync(supplier);

            return new DeleteSupplierResponse
            {
                Success = true,
                Action = "DELETED",
                Message = "Supplier deleted successfully."
            };
        }

        private static string NormalizeRequiredName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("SUPPLIER_NAME_REQUIRED");

            return name.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static SupplierDetailResponse MapToDetailResponse(Supplier supplier)
        {
            return new SupplierDetailResponse
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                Notes = supplier.Notes,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt
            };
        }
    }
}
