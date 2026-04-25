using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TEST_IDENNA.Data;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Repositories
{
    public class BeneficiarioRepository : IBeneficiarioRepository
    {
        private readonly AppDbContext _context;

        public BeneficiarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Registrar(Beneficiario beneficiario)
        {
            // EF Core marca el objeto como "Added"
            await _context.Beneficiarios.AddAsync(beneficiario);

            // EF Core genera el SQL INSERT y lo envía a SQLite
            await _context.SaveChangesAsync();
        }

        public async Task<List<Beneficiario>> ObtenerTodos()
        {
            return await _context.Set<Beneficiario>().ToListAsync();
        }

        public async Task<List<Beneficiario>> ObtenerPorNombre(string texto)
        {
            return await _context.Beneficiarios
                .Where(b => b.Nombres.Contains(texto) || b.Apellidos.Contains(texto))
                .Take(5) // Limitamos a 5 para que sea rápido
                .ToListAsync();
        }
    }
}
