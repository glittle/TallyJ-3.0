using System;
using System.ComponentModel.DataAnnotations;

namespace TallyJ.Models.DTOs
{
    /// <summary>
    /// Election data transfer object for API operations
    /// </summary>
    public class ElectionDto
    {
        /// <summary>
        /// Unique identifier for the election
        /// </summary>
        public Guid ElectionGuid { get; set; }

        /// <summary>
        /// Name of the election
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Name of the election convenor
        /// </summary>
        [MaxLength(100)]
        public string Convenor { get; set; }

        /// <summary>
        /// Date when the election is scheduled
        /// </summary>
        public DateTime? DateOfElection { get; set; }

        /// <summary>
        /// Type of election (Normal, ByElection, etc.)
        /// </summary>
        [MaxLength(50)]
        public string ElectionType { get; set; }

        /// <summary>
        /// Election mode (InPerson, Online, etc.)
        /// </summary>
        [MaxLength(50)]
        public string ElectionMode { get; set; }

        /// <summary>
        /// Number of positions to elect
        /// </summary>
        public int? NumberToElect { get; set; }

        /// <summary>
        /// Number of extra positions
        /// </summary>
        public int? NumberExtra { get; set; }

        /// <summary>
        /// Current tally status
        /// </summary>
        [MaxLength(50)]
        public string TallyStatus { get; set; }

        /// <summary>
        /// Whether to show full report
        /// </summary>
        public bool? ShowFullReport { get; set; }
    }

    /// <summary>
    /// Create election request model
    /// </summary>
    public class CreateElectionRequest
    {
        /// <summary>
        /// Name of the election
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Name of the election convenor
        /// </summary>
        [MaxLength(100)]
        public string Convenor { get; set; }

        /// <summary>
        /// Date when the election is scheduled
        /// </summary>
        public DateTime? DateOfElection { get; set; }

        /// <summary>
        /// Type of election
        /// </summary>
        [MaxLength(50)]
        public string ElectionType { get; set; }

        /// <summary>
        /// Number of positions to elect
        /// </summary>
        public int? NumberToElect { get; set; }
    }
}