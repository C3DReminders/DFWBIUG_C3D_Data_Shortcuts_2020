using Microsoft.EntityFrameworkCore;
using Quux.AcadUtilities.CommandParameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BIMDemo.SQLiteDatabase
{
    public class DemoDbContext : DbContext
    {
        public static string DatabaseFullPath { get; set; }
        public const string DatabaseFullPathKey = "DatabaseFullPathKey";
        public DbSet<DemoLayerMap> DemoLayerMaps { get; set; }
        public DbSet<Layer> Layers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DemoLayerMap>()
                .HasOne(d => d.Layer)
                .WithMany(l => l.DemoLayerMaps)
                .HasForeignKey(d => d.LayerId);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabaseFullPath}");
        }

        public static void EnsureDatabaseCreated()
        {
            DatabaseFullPath = CommandDefault.ReadCommandDefault(DatabaseFullPathKey, "", PersistenceLocation.RegistryOnly);

            if (!File.Exists(DatabaseFullPath))
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "SQLite files (*.db)|*.db|All files (*.*)|*.*";
                    openFileDialog.Title = "Select SQLite Database File";
                    openFileDialog.CheckFileExists = false;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        DatabaseFullPath = openFileDialog.FileName;
                        CommandDefault.WriteCommandDefault(DatabaseFullPathKey, DatabaseFullPath, PersistenceLocation.RegistryOnly);
                    }
                    else
                    {
                        throw new InvalidOperationException("No database file selected.");
                    }
                }

                using (var context = new DemoDbContext())
                {
                    context.Database.EnsureCreated();
                }
            }
        }
    }
}
