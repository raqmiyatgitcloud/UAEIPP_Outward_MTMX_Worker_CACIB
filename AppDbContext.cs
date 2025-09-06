using Microsoft.EntityFrameworkCore;
using UAEIPP_Outward_MTMX_Worker.Model;

namespace UAEIPP_Outward_MTMX_Worker
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContext) : base(dbContext)
        {
        }
        public DbSet<IppCreditTransferpaymentdetails> _ippCreditTransferpaymentdetails { get; set; }
       public DbSet<MasterAccounts> masterAccounts { get; set; }
        


    }
}
