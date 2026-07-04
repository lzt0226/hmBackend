using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PatientMonitor.Api.Models;

[Table("behavior_log")]
public class BehaviorLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("patient_id")]
    [JsonPropertyName("patientId")]
    public long PatientId { get; set; }

    [Column("behavior_type")]
    [JsonPropertyName("behaviorType")]
    public string BehaviorType { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("is_abnormal")]
    [JsonPropertyName("isAbnormal")]
    public int IsAbnormal { get; set; }

    [Column("record_time")]
    [JsonPropertyName("recordTime")]
    public DateTime RecordTime { get; set; } = DateTime.Now;

    [ForeignKey("PatientId")]
    [JsonIgnore]
    public Patient? Patient { get; set; }
}
