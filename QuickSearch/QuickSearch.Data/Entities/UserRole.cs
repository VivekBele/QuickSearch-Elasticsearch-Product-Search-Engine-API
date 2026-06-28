using System.ComponentModel.DataAnnotations.Schema;

namespace QuickSearch.Data.Entities
{
    [Table("UserRoles")]
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
}
