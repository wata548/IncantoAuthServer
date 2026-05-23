using System.Net;
using System.Net.Mail;
using Setting;

namespace Extension;

public static class Mail {

    public static async Task Send(string pTarget, string pSubject, string pContext) {
        //message
        using var mail = new MailMessage();
        mail.From = new MailAddress(Env.Get("ServerMailId"));
        mail.To.Add(pTarget);
        mail.Subject = pSubject;
        mail.Body = pContext;
        
        //initialize client
        using var client = new SmtpClient("smtp.gmail.com", 587);
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(
            Env.Get("ServerMailId"), 
            Env.Get("ServerMailPw")
        );
        client.EnableSsl = true;
        
        try {
            await client.SendMailAsync(mail);
            Console.WriteLine($"Send Mail to {pTarget}");
        }
        catch (Exception e) {
            Console.WriteLine($"Error: {e}");
        }
    }
}