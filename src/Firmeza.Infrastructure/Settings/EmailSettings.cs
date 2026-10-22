namespace Firmeza.Infrastructure.Settings;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = "firmeza.construccion@gmail.com";
    public string SenderName { get; set; } = "Firmeza S.A.S. - Materiales & Maquinaria";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
