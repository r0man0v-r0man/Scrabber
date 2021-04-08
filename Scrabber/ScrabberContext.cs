using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabber
{
    public class ScrabberContext : DbContext
    {
        public DbSet<Advert> Adverts { get; set; }

        public ScrabberContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("DataSource=D:\\halupa\\scrabber_db.db");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
