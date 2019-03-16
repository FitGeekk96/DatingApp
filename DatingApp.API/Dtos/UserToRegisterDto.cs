using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos
{
    public class UserToRegisterDto
    {
        //I will also call it Name to not make any confusion
        //Also any validation will be done here not in the original model

        [Required]
        public string Name { get; set; }
        
        [Required]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "pass must be between 4 and 8")]
        public string Password { get; set; }
    }
}