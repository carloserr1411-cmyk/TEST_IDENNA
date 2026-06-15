using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using Microsoft.EntityFrameworkCore;

namespace TEST_IDENNA.Repositories
{
    public class TutorRepository(AppDbContext context) : ITutorRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<IEnumerable<Tutores>> GetAllAsync()
            => await _context.Tutores.ToListAsync();

        public async Task AddAsync(Tutores tutor)
        {
            _context.Tutores.Add(tutor);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Tutores tutor)
        {
            _context.Tutores.Update(tutor);
            await _context.SaveChangesAsync();
        }
    }
}
