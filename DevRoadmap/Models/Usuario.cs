using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//Não sei se é correto deixar o namespace da mesma forma que no using 
//Estou seguindo o padrão do projeto Labra desenvolvido por Rafael
// Adapatando para ca (A fim de sumir com as flags de erro)

// Model deve mudar para acompanhar nova logica
namespace DevRoadmap.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
        public bool? EmailVerificado { get; set; }
        public int PerfilId { get; set; }
        public Perfis? Perfil { get; set; }

        // construtor impede a compilação!!
        // public Usuario()
        // {
            
        // }
    }
}