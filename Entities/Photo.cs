using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;


//Entity framework letrehozza a tablat, viszont a dbcontext-nel nem definialjuk mert nem akarunk queryzni rajta, viszont a tabla amugy is letrejon akkor legyen ez a neve
[Table("Photos")]
public class Photo
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public bool IsMain { get; set; }
    //Cloud id
    public string? PublicId { get; set; }
    //Navigation properties
    public int AppUserId { get; set; }
    //NOT NULLABLE = null! ezt jelenti- this is required
    public AppUser AppUser { get; set; } = null!;
}