using Microsoft.EntityFrameworkCore;

namespace UAEIPP_Outward_MTMX_Worker
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContext) : base(dbContext)
        {
        }
        public DbSet<IppCreditTransferpaymentdetails> _ippCreditTransferpaymentdetails { get; set; }
       
    }
}
