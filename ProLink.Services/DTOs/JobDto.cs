﻿using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProLink.Application.DTOs
{
    public class JobDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        public /*IFormFile*/string? PostImage { get; set; }
    }
}
