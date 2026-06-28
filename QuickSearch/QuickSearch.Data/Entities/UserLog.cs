using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickSearch.Data.Entities
{
    [Table("user_logs")]
    public class UserLog
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("login_time")]
        public DateTime LoginTime { get; set; }
    }
}
