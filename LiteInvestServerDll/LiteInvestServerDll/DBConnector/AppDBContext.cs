using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ReportEntity;
using LiteInvest.Entity.ServerEntity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;

namespace LiteInvestServerDll
{
    public class AppDBContext : DbContext
    {
        //private readonly IConfiguration configuration;

        public DbSet<AccountCapex> AccountCapex { get; set; } = null!;

        public DbSet<AccountTrade> AccountTrade { get; set; } = null!;
        
        public DbSet<MyTrade> MyTrades { get; set; } = null!;

        public DbSet<User> Users { get; set; } = null!;
                
        public DbSet<ReportFinRezBoard> ReportFinRezBoard { get; set; } = null!;
        
        public DbSet<ReportFinRezInstrument> ReportFinRezInstrument {  get; set; } = null!;
        
        public DbSet<ReportCapexHistoryTransaction> ReportCapexHistoryTransaction { get; set; } = null!;

        public DbSet<ReportTradeDay> ReportOrderDay { get; set; }   = null!; 

        public DbSet<ReportPositionTransfer> ReportPositionTransfer { get; set; } = null!;

        public DbSet<SecurityApi> SecurityApi { get; set; } = null!;

        static  bool firstStart = true;

        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
            if (AppDBContext.firstStart)
            {
               // Database.EnsureDeleted();
               // Database.EnsureCreated();
                AppDBContext.firstStart = false;
            }
        }

    }
}
