using System.Collections.Generic;

namespace Supermarket.Contracts.Sessions
{
    public class SessionHistoryResponse
    {
        public List<SessionHistoryItem> Items { get; set; } = new List<SessionHistoryItem>();
        public int TotalCount { get; set; }
    }
}
