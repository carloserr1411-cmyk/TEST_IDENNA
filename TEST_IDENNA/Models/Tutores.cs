using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TEST_IDENNA.Models
{
    public class Tutores
    {
        [Key]
        public int Id_Especialista { get; set; }

        [Required]
        public required string NombreCompleto { get; set; }

        [Required]
        public required string Cargo { get; set; }

        [Required]
        public required string Cedula { get; set; }

        [Required]
        public required bool IsActivo { get; set; }

    }
}
