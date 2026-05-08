using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using Microsoft.EntityFrameworkCore;

namespace TEST_IDENNA.Repositories
{
    public class EgresoRepository : IEgresoRepository
    {
        private readonly AppDbContext _context;

        public EgresoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Egreso>> ObtenerTodos()
        {
            return await _context.Egresos
                .Include(e => e.Beneficiario)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task Registrar(Egreso egreso)
        {
            _context.Egresos.Add(egreso);
            await _context.SaveChangesAsync();
        }
    }
}
