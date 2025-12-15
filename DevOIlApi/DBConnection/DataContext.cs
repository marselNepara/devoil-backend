using DevOIlApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DevOIlApi.DBConnection
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        //public DbSet<Application> Application { get; set; }
        //public DbSet<Certificate> Certificate { get; set; }
        //public DbSet<Product> Products { get; set; }
        //public DbSet<ProductApplication> ProductsApplication { get; set; }
        //public DbSet<ProductCertificate> ProductCertificates { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Bid> Bids { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.First_Name)
                    .HasColumnName("First_Name")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Last_Name)
                    .HasColumnName("Last_Name")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasAnnotation("EmailAddress", true);

                entity.Property(e => e.Phone_Number)    
                    .HasColumnName("Phone_Number")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Date_of_registration)
                    .HasColumnName("Date_of_registration")
                    .IsRequired();
            });


            modelBuilder.Entity<Bid>(entity =>
            {
                entity.ToTable("Bids");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Comment)
                    .HasColumnName("Comment")
                    .HasMaxLength(1000);

                entity.Property(e => e.IsProcessedByAdmin)
                    .HasColumnName("IsChecked")
                    .IsRequired();

                entity.Property(e => e.Id_Client)
                    .HasColumnName("Id_Client")
                    .IsRequired();
            });

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Client)
                .WithMany(u => u.ClientBids)
                .HasForeignKey(b => b.Id_Client)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Client>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(u => u.Phone_Number)
                .IsUnique();
        }
    }
}
