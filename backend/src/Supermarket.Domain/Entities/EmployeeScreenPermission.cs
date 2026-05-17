namespace Supermarket.Domain.Entities
{
    public class EmployeeScreenPermission
    {
        public long Id { get; set; }
        public long EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        
        public int ScreenId { get; set; }
        public AppScreen? Screen { get; set; }
        
        public bool CanAccess { get; set; }
    }
}
