using System.ComponentModel.DataAnnotations;

namespace OrderApi.Domain.Entities
{
    public class Buyer
    {
        [Required]
        public string Email { get; private set; }

        public Buyer(string email)
        {
            Email = !string.IsNullOrEmpty(email) ? email : throw new ArgumentNullException(nameof(email));
        }
    }
}
