using System;
using System.ComponentModel.DataAnnotations;

namespace TallyJ.Models.DTOs
{
    /// <summary>
    /// Person data transfer object for API operations
    /// </summary>
    public class PersonDto
    {
        /// <summary>
        /// Unique identifier for the person
        /// </summary>
        public Guid PersonGuid { get; set; }

        /// <summary>
        /// Election this person belongs to
        /// </summary>
        public Guid ElectionGuid { get; set; }

        /// <summary>
        /// Person's last name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        /// <summary>
        /// Person's first name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        /// <summary>
        /// Other last names
        /// </summary>
        [MaxLength(100)]
        public string OtherLastNames { get; set; }

        /// <summary>
        /// Other names
        /// </summary>
        [MaxLength(100)]
        public string OtherNames { get; set; }

        /// <summary>
        /// Additional information about the person
        /// </summary>
        [MaxLength(500)]
        public string OtherInfo { get; set; }

        /// <summary>
        /// Geographic area
        /// </summary>
        [MaxLength(100)]
        public string Area { get; set; }

        /// <summary>
        /// Bahai ID number
        /// </summary>
        [MaxLength(50)]
        public string BahaiId { get; set; }

        /// <summary>
        /// Age group classification
        /// </summary>
        [MaxLength(20)]
        public string AgeGroup { get; set; }

        /// <summary>
        /// Whether person is eligible to vote
        /// </summary>
        public bool? CanVote { get; set; }

        /// <summary>
        /// Whether person is eligible to be voted for
        /// </summary>
        public bool? CanReceiveVotes { get; set; }
    }

    /// <summary>
    /// Create person request model
    /// </summary>
    public class CreatePersonRequest
    {
        /// <summary>
        /// Person's last name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        /// <summary>
        /// Person's first name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        /// <summary>
        /// Other last names
        /// </summary>
        [MaxLength(100)]
        public string OtherLastNames { get; set; }

        /// <summary>
        /// Other names
        /// </summary>
        [MaxLength(100)]
        public string OtherNames { get; set; }

        /// <summary>
        /// Additional information
        /// </summary>
        [MaxLength(500)]
        public string OtherInfo { get; set; }

        /// <summary>
        /// Geographic area
        /// </summary>
        [MaxLength(100)]
        public string Area { get; set; }

        /// <summary>
        /// Bahai ID number
        /// </summary>
        [MaxLength(50)]
        public string BahaiId { get; set; }

        /// <summary>
        /// Age group
        /// </summary>
        [MaxLength(20)]
        public string AgeGroup { get; set; }
    }
}