using Microsoft.EntityFrameworkCore;
using DevRoadmap.Models;

namespace DevRoadmap.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Perfis> Perfis => Set<Perfis>();
        public DbSet<Trials> Trials => Set<Trials>();
        public DbSet<Module> Modules => Set<Module>();
       //public DbSet<Content> Contents => Set<Content>();
        public DbSet<Archive> Archives => Set<Archive>();
        public DbSet<Video> Videos => Set<Video>();
        public DbSet<StudentTrial> StudentTrials => Set<StudentTrial>();
        public DbSet<ModuleProgress> ModuleProgresses => Set<ModuleProgress>();
        public DbSet<Notifications> Notifications => Set<Notifications>();
        public DbSet<VerificacaoEmail> VerificacoesEmail => Set<VerificacaoEmail>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();

            //builder.Entity<Content>()
                //.HasOne(c => c.Module)
                //.WithOne(m => m.Content)
                //.HasForeignKey<Content>(c => c.ModuleId);

            builder.Entity<Module>()
                .HasIndex(m => new { m.TrialId, m.Order }).IsUnique();

            builder.Entity<ModuleProgress>()
                .HasKey(p => new { p.StudentTrialId, p.ModuleId });

            builder.Entity<StudentTrial>()
                .HasIndex(s => new { s.UsuarioId, s.TrialId }).IsUnique();

            builder.Entity<VerificacaoEmail>()
                .HasKey(v => new { v.UsuarioId, v.Finalidade });

            builder.Entity<Perfis>().HasData(
                new Perfis { Id = 1, Cargo = "Student" },
                new Perfis { Id = 2, Cargo = "Teacher" },
                new Perfis { Id = 3, Cargo = "Admin" });
        }
    }
}