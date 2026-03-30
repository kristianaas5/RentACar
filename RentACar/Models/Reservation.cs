namespace RentACar.Models;
using System.ComponentModel.DataAnnotations;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public User User { get; set; }

    [Required]
    public int CarId { get; set; }

    public Car Car { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public bool IsReserved { get; set; }
}
