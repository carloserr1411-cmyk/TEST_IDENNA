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

        public async Task Actualizar(Beneficiario beneficiario)
        {
            // 1. Buscamos si el DbContext ya está rastreando una instancia con ese mismo ID
            var local = _context.Beneficiarios
                .Local
                .FirstOrDefault(entry => entry.Id_Beneficiario == beneficiario.Id_Beneficiario);

            // 2. Si existe, le decimos al contexto que deje de rastrearla (Detach)
            if (local != null)
            {
                _context.Entry(local).State = EntityState.Detached;
            }

            // 3. Ahora sí, marcamos nuestro objeto (el clon) como modificado
            _context.Entry(beneficiario).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        public async Task<List<Beneficiario>> ObtenerTodos()
        {
            return await _context.Set<Beneficiario>().ToListAsync();
        }

        public async Task<List<Beneficiario>> ObtenerPorNombre(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new List<Beneficiario>();

            // Convertimos la búsqueda a minúsculas una sola vez
            string busqueda = texto.ToLower().Trim();

            return await _context.Beneficiarios
                .Where(b =>
                    // Buscamos en Nombres
                    (b.Nombres.ToLower().Contains(busqueda) ||
                    // Buscamos en Apellidos
                    b.Apellidos.ToLower().Contains(busqueda) ||
                    // Concatenamos para permitir buscar "Nombre Apellido"
                    (b.Nombres.ToLower() + " " + b.Apellidos.ToLower()).Contains(busqueda)) &&
                    (b.Estatus == "Activo"))
                .OrderBy(b => b.Nombres) // Ordenamos alfabéticamente
                .Take(10) // Un límite mayor ayuda a la experiencia de usuario
                .ToListAsync();
        }
    }
}
