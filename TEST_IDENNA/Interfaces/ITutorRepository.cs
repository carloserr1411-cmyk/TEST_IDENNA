using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{
    public interface ITutorRepository
    {
        Task<IEnumerable<Tutores>> GetAllAsync();
        Task AddAsync(Tutores tutor);

        Task UpdateAsync(Tutores tutor);
    }
}
