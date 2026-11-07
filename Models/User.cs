using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APULMS.Models
{
    public class User
    {

        public int Id { get; set; }



        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;



        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;



        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;



        [NotMapped]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;



        public string Role { get; set; } = string.Empty;



        public DateTime CreatedAt { get; set; }

    }
}