using Microsoft.EntityFrameworkCore;
using SmartFileOrganizer.Core.Models;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SmartFileOrganizer.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<OperationHistoryEntry> OperationHistory { get; set; }
        public DbSet<ScanSession> ScanSessions { get; set; }
        public DbSet<DuplicateGroup> DuplicateGroups { get; set; }

        public AppDbContext( DbContextOptions<AppDbContext> options ) : base( options )
        {
        }

        protected override void OnModelCreating( ModelBuilder modelBuilder )
        {
            base.OnModelCreating( modelBuilder );
            
            modelBuilder.Entity< OperationHistoryEntry >( ).HasKey( e => e.Id );
            modelBuilder.Entity< ScanSession >( ).HasKey( e => e.Id );
            modelBuilder.Entity< DuplicateGroup >( ).HasKey( e => e.Id );
            modelBuilder.Entity< FileRecord >( ).HasKey( e => e.Id );
        }
    }
}
