using System.ComponentModel.DataAnnotations;

namespace ChatApp.Contracts.Requests;

public sealed class SendMessageRequest
{
    [Required(ErrorMessage = "Имя отправителя обязательно")]
    [MinLength(1)]
    [MaxLength(50)]
    public String SenderName { get; set; } = String.Empty;

    [Required(ErrorMessage = "Содержимое сообщения обязательно")]
    [MinLength(1)]
    [MaxLength(500)]
    public String Content { get; set; } = String.Empty;
}
