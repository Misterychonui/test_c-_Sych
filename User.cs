using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Login { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public Gender Gender { get; set; }

    public DateTime? Birthday { get; set; }

    [Required]
    public bool Admin { get; set; }

    [Required]
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string ModifiedBy { get; set; }

    public DateTime? RevokedOn { get; set; }

    public string RevokedBy { get; set; }

    public bool IsActive => !RevokedOn.HasValue;

    public int Age => CalculateAge();

    private int CalculateAge()
    {
        if (!Birthday.HasValue)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - Birthday.Value.Year;

        if (Birthday.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }
}

public enum Gender
{
    Female = 0,
    Male = 1,
    Unknown = 2
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }
    public bool IsActive { get; set; }
    public int Age { get; set; }
}