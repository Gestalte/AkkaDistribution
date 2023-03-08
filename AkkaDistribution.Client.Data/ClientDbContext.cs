﻿using Microsoft.EntityFrameworkCore;

namespace AkkaDistribution.Client.Data
{
    public class ClientDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=filesync.sqlite");
        }

        public DbSet<FilePart> FileParts { get; set; }
    }
}
