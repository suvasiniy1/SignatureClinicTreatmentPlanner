using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SignatureClinicTreatmentPlanner.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<Surgeon> Surgeons { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<Surgeon_Treatment> Surgeon_Treatment { get; set; }
        public DbSet<Surgeon_Clinic> Surgeon_Clinic { get; set; }
        public DbSet<PatientSurgeons> PatientSurgeons { get; set; }
        //public DbSet<User> AspNetUsers { get; set; }
        public DbSet<Role> Roles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
        .ToTable("AspNetUsers")
        .HasDiscriminator<string>("Discriminator")
        .HasValue<User>("User");  // ✅ Explicitly map User entity


            // 🔹 Rename Identity Tables
            //modelBuilder.Entity<User>().ToTable("AspNetUsers");
            //modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            modelBuilder.Entity<Treatment>().ToTable("Treatments");
            modelBuilder.Entity<Surgeon>().ToTable("Surgeons");
            modelBuilder.Entity<Clinic>().ToTable("Clinics");
            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<Surgeon_Clinic>()
               .HasKey(sc => new { sc.SurgeonID, sc.ClinicID });
            modelBuilder.Entity<Surgeon_Treatment>()
              .HasKey(sc => new { sc.SurgeonID, sc.TreatmentID });

            // Define the composite key for the PatientSurgeon entity
            modelBuilder.Entity<PatientSurgeons>()
     .HasKey(ps => new { ps.PatientId, ps.SurgeonId }); // Composite key

            // Define the relationship between Patient and PatientSurgeon
            //modelBuilder.Entity<PatientSurgeons>()
            //    .HasOne(ps => ps.Patient)
            //    .WithMany(p => p.PatientSurgeons)
            //    .HasForeignKey(ps => ps.PatientId)
            //    .OnDelete(DeleteBehavior.Cascade);  // Cascade delete ensures that PatientSurgeon is deleted if the related Patient is deleted

            // Define the relationship between Surgeon and PatientSurgeon
            modelBuilder.Entity<PatientSurgeons>()
                .HasOne(ps => ps.Surgeon)
                .WithMany(s => s.PatientSurgeons)
                .HasForeignKey(ps => ps.SurgeonId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);
      


        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=y1sqlserverdb.database.windows.net;User ID=y1sqlserver;Password=Tr@n$f0rmDB321;Database=SignatureClinicDB;Persist Security Info=true;Encrypt=false;TrustServerCertificate=True;",
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,  // Number of retries
                            maxRetryDelay: TimeSpan.FromSeconds(10),  // Delay between retries
                            errorNumbersToAdd: null  // Handles transient errors
                        );
                    });
            }
        }
    }
}
