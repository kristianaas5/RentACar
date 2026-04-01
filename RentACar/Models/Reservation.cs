namespace RentACar.Models;
using System.ComponentModel.DataAnnotations;

public class Reservation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    public string CarId { get; set; }

    public Car Car { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
    public bool IsDeleted { get; set; }

    [Required]
    public bool IsReserved { get; set; }
}
