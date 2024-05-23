using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RPCServer.Data
{
    public class RPCServer_Context : DbContext
    {
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Servico> Servicos { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=SDTP2324;Trusted_Connection=True;")
                .LogTo(Console.WriteLine, LogLevel.Information);

        //Utilizamos esta função para "povoar" a tabela dos serviços com alguns serviços
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Servico>().HasData(
                new Servico
                {
                    ServicoId = "SA_T1",
                    ServicoTipo = "Servico A",
                    Descricao = "Ronda na zona 1 da cidade",
                    Estado = "Nao alocado",
                    ClienteId = null,
                    AdminName = "admin"
                },
                new Servico
                {
                    ServicoId = "SA_T2",
                    ServicoTipo = "Servico A",
                    Descricao = "Ronda na zona 2 da cidade",
                    Estado = "Nao alocado",
                    ClienteId = null,
                    AdminName = "admin"
                },
                new Servico
                {
                    ServicoId = "SA_T3",
                    ServicoTipo = "Servico A",
                    Descricao = "Ronda na zona 3 da cidade",
                    Estado = "Nao alocado",
                    ClienteId = null,
                    AdminName = "admin"
                },
                new Servico
                {
                    ServicoId = "SB_T1",
                    ServicoTipo = "Servico B",
                    Descricao = "Verificar vegetação na zona dos passadiços do Corgo",
                    Estado = "Nao alocado",
                    ClienteId = null,
                    AdminName = "toze"
                },
                new Servico
                {
                    ServicoId = "SC_T1",
                    ServicoTipo = "Servico C",
                    Descricao = "Verificar árvores da Praça Diogo Cão",
                    Estado = "Nao alocado",
                    ClienteId = null,
                    AdminName = "maria"
                }
                );

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    Username = "admin",
                    Password = "1234",
                    Servico = "Servico A"
                },
                new Admin
                {
                    Username = "toze",
                    Password = "admin",
                    Servico = "Servico B"
                },
                new Admin
                {
                    Username = "maria",
                    Password = "4321",
                    Servico = "Servico C"
                });
            modelBuilder.Entity<Admin>().HasKey(s => s.Username);
        }
    }

    public class Cliente
    {
        public string ClienteId { get; set; }
        public string? Servico { get; set; }
        public int Idmota { get; set; }
        public Servico? ServicoId { get; set; }
    }
    public class Servico
    {
        public string ServicoId { get; set; }
        public string ServicoTipo { get; set; }
        public string Descricao { get; set; }
        public string Estado { get; set; }
        public string? ClienteId { get; set; }
        public string? AdminName { get; set; }
        public Admin? Admin { get; set; }
        public Cliente? Cliente { get; set; }

    }

    public class Admin
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Servico { get; set; }
    }
}
