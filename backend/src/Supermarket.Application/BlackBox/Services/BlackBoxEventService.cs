using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.BlackBox.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.BlackBox;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.BlackBox.Services
{
    public class BlackBoxEventService : IBlackBoxEventService
    {
        private readonly IBlackBoxEventRepository _repository;
        private readonly IBlackBoxMetadataSanitizer _metadataSanitizer;
        private readonly ISessionContextAccessor _sessionContextAccessor;

        public BlackBoxEventService(
            IBlackBoxEventRepository repository,
            IBlackBoxMetadataSanitizer metadataSanitizer,
            ISessionContextAccessor sessionContextAccessor)
        {
            _repository = repository;
            _metadataSanitizer = metadataSanitizer;
            _sessionContextAccessor = sessionContextAccessor;
        }

        public async Task<CreateBlackBoxEventResponse> CreateAsync(
            CreateBlackBoxEventRequest request,
            string? ipAddress,
            string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(request.ActionType))
            {
                throw new InvalidOperationException("BLACK_BOX_ACTION_TYPE_REQUIRED");
            }

            if (string.IsNullOrWhiteSpace(request.Result))
            {
                throw new InvalidOperationException("BLACK_BOX_RESULT_REQUIRED");
            }

            var metadataJson = _metadataSanitizer.Sanitize(request.Metadata, out var metadataTruncated);
            var session = _sessionContextAccessor.Current;

            var blackBoxEvent = new BlackBoxEvent
            {
                EmployeeId = session.IsAuthenticated ? session.EmployeeId : null,
                SessionId = session.IsAuthenticated ? session.SessionId : null,
                DeviceCode = session.IsAuthenticated ? TrimToNull(session.DeviceCode, 100) : null,
                Route = TrimToNull(request.Route, 300),
                PageName = TrimToNull(request.PageName, 150),
                ActionType = request.ActionType.Trim(),
                ElementKey = TrimToNull(request.ElementKey, 150),
                EntityType = TrimToNull(request.EntityType, 100),
                EntityId = TrimToNull(request.EntityId, 100),
                Result = request.Result.Trim(),
                Message = TrimToNull(request.Message, 500),
                MetadataJson = metadataJson,
                MetadataTruncated = metadataTruncated,
                IpAddress = TrimToNull(ipAddress, 64),
                UserAgent = TrimToNull(userAgent, 500),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.CreateAsync(blackBoxEvent);

            return new CreateBlackBoxEventResponse
            {
                Success = true,
                Id = blackBoxEvent.Id
            };
        }

        public async Task<BlackBoxEventListResponse> GetPagedAsync(BlackBoxEventQuery query)
        {
            NormalizeQuery(query);
            var (items, totalCount) = await _repository.GetPagedAsync(query);

            return new BlackBoxEventListResponse
            {
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                Items = items.Select(MapToListItem).ToList()
            };
        }

        public async Task<BlackBoxEventDetailResponse?> GetByIdAsync(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return null;
            }

            var response = new BlackBoxEventDetailResponse
            {
                MetadataJson = item.MetadataJson,
                IpAddress = item.IpAddress,
                UserAgent = item.UserAgent
            };

            CopyListFields(MapToListItem(item), response);
            return response;
        }

        private static BlackBoxEventListItem MapToListItem(BlackBoxEvent item)
        {
            return new BlackBoxEventListItem
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                EmployeeName = item.Employee?.FullName,
                SessionId = item.SessionId,
                DeviceCode = item.DeviceCode,
                Route = item.Route,
                PageName = item.PageName,
                ActionType = item.ActionType,
                ElementKey = item.ElementKey,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                Result = item.Result,
                Message = item.Message,
                HasMetadata = !string.IsNullOrWhiteSpace(item.MetadataJson),
                MetadataTruncated = item.MetadataTruncated,
                CreatedAtUtc = item.CreatedAtUtc
            };
        }

        private static void CopyListFields(BlackBoxEventListItem source, BlackBoxEventDetailResponse target)
        {
            target.Id = source.Id;
            target.EmployeeId = source.EmployeeId;
            target.EmployeeName = source.EmployeeName;
            target.SessionId = source.SessionId;
            target.DeviceCode = source.DeviceCode;
            target.Route = source.Route;
            target.PageName = source.PageName;
            target.ActionType = source.ActionType;
            target.ElementKey = source.ElementKey;
            target.EntityType = source.EntityType;
            target.EntityId = source.EntityId;
            target.Result = source.Result;
            target.Message = source.Message;
            target.HasMetadata = source.HasMetadata;
            target.MetadataTruncated = source.MetadataTruncated;
            target.CreatedAtUtc = source.CreatedAtUtc;
        }

        private static void NormalizeQuery(BlackBoxEventQuery query)
        {
            if (query.Page < 1) query.Page = 1;
            if (query.PageSize < 1) query.PageSize = 50;
            if (query.PageSize > 200) query.PageSize = 200;
        }

        private static string? TrimToNull(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}
