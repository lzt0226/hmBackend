using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PatientMonitor.Api.Models;

[Table("patient")]
public class Patient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("age")]
    public int Age { get; set; }

    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Column("room_number")]
    [JsonPropertyName("roomNumber")]
    public string RoomNumber { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "normal";

    [Column("severity")]
    public int Severity { get; set; } = 0;

    [Column("created_at")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
