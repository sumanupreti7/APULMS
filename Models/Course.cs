using System;
using System.ComponentModel.DataAnnotations;

namespace APULMS.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course title is required.")]
        [StringLength(
            255,
            MinimumLength = 3,
            ErrorMessage = "Course title must be between 3 and 255 characters."
        )]
        public string Title { get; set; } = string.Empty;

        [StringLength(
            2000,
            ErrorMessage = "Description cannot exceed 2000 characters."
        )]
        public string Description { get; set; } = string.Empty;

        [StringLength(
            100,
            ErrorMessage = "Category cannot exceed 100 characters."
        )]
        public string Category { get; set; } = string.Empty;

        [StringLength(
            255,
            ErrorMessage = "Thumbnail path cannot exceed 255 characters."
        )]
        public string Thumbnail { get; set; } = string.Empty;

        [StringLength(
            255,
            ErrorMessage = "Video URL cannot exceed 255 characters."
        )]
        public string VideoUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a lecturer.")]
        [Display(Name = "Lecturer")]
        public int? LecturerId { get; set; }

        public string LecturerName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}