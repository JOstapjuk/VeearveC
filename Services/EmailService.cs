using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;
using Veearve.Data;
using Veearve.Models;

namespace Veearve.Services
{
    public interface IEmailService
    {
        Task<EmailReminderDto> SendReminderAsync(string billId);
        Task<BulkEmailResultDto> SendAllRemindersAsync();
    }

    public class EmailService : IEmailService
    {
        private readonly MongoDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            MongoDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<EmailReminderDto> SendReminderAsync(string billId)
        {
            var reading = await _context.Readings
                .Find(r => r.Id == billId)
                .FirstOrDefaultAsync();

            if (reading == null)
            {
                throw new Exception("Bill not found");
            }

            if (reading.IsPaid)
            {
                throw new Exception("Bill is already paid");
            }

            var user = await _context.Users
                .Find(u => u.Id == reading.UserId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new Exception("User not found");
            }

            var emailBody = GenerateEmailBody(reading, user);
            var success = await SendEmailAsync(
                user.Email,
                $"Veearvesti Arve Meeldetuletus - Korter {reading.ApartmentNumber}",
                emailBody
            );

            return new EmailReminderDto
            {
                EmailSent = success,
                Message = success ? "Reminder email sent successfully" : "Failed to send email",
                Recipient = user.Email
            };
        }

        public async Task<BulkEmailResultDto> SendAllRemindersAsync()
        {
            var unpaidReadings = await _context.Readings
                .Find(r => r.IsPaid == false)
                .ToListAsync();

            if (unpaidReadings.Count == 0)
            {
                return new BulkEmailResultDto
                {
                    Message = "No unpaid bills found",
                    TotalUnpaidBills = 0,
                    EmailsSent = 0,
                    EmailsFailed = 0,
                    Details = new List<EmailDetailDto>()
                };
            }

            int successCount = 0;
            int failCount = 0;
            var details = new List<EmailDetailDto>();

            foreach (var reading in unpaidReadings)
            {
                var user = await _context.Users
                    .Find(u => u.Id == reading.UserId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    failCount++;
                    details.Add(new EmailDetailDto
                    {
                        ApartmentNumber = reading.ApartmentNumber,
                        Email = "N/A",
                        Status = "failed",
                        Reason = "User not found"
                    });
                    continue;
                }

                var emailBody = GenerateEmailBody(reading, user);
                var success = await SendEmailAsync(
                    user.Email,
                    $"Veearvesti Arve Meeldetuletus - Korter {reading.ApartmentNumber}",
                    emailBody
                );

                if (success)
                {
                    successCount++;
                    details.Add(new EmailDetailDto
                    {
                        ApartmentNumber = reading.ApartmentNumber,
                        Email = user.Email,
                        Status = "sent"
                    });
                }
                else
                {
                    failCount++;
                    details.Add(new EmailDetailDto
                    {
                        ApartmentNumber = reading.ApartmentNumber,
                        Email = user.Email,
                        Status = "failed",
                        Reason = "SMTP error"
                    });
                }
            }

            return new BulkEmailResultDto
            {
                Message = $"Sent {successCount} emails successfully, {failCount} failed",
                TotalUnpaidBills = unpaidReadings.Count,
                EmailsSent = successCount,
                EmailsFailed = failCount,
                Details = details
            };
        }

        private async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"✅ Email sent to: {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending email to {to}: {ex.Message}");
                return false;
            }
        }

        private string GenerateEmailBody(Reading reading, User user)
        {
            var billDate = reading.Date.ToString("dd.MM.yyyy");

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .bill-details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #4CAF50; }}
                    .amount {{ font-size: 24px; font-weight: bold; color: #e53935; }}
                    .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
                    td {{ padding: 8px; border-bottom: 1px solid #ddd; }}
                    .label {{ font-weight: bold; width: 40%; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Veearvesti Arve Meeldetuletus</h1>
                    </div>
                    <div class='content'>
                        <p>Tere, <strong>{user.Name}</strong>!</p>
            
                        <p>See on meeldetuletus teie maksmata veearvest.</p>
            
                        <div class='bill-details'>
                            <h3>Arve Detailid</h3>
                            <table>
                                <tr>
                                    <td class='label'>Korter:</td>
                                    <td>{reading.ApartmentNumber}</td>
                                </tr>
                                <tr>
                                    <td class='label'>Kuupäev:</td>
                                    <td>{billDate}</td>
                                </tr>
                                <tr>
                                    <td class='label'>Külm vesi:</td>
                                    <td>{reading.ColdWater} m³</td>
                                </tr>
                                <tr>
                                    <td class='label'>Soe vesi:</td>
                                    <td>{reading.HotWater} m³</td>
                                </tr>
                                <tr>
                                    <td class='label'>Maksmisele kuuluv summa:</td>
                                    <td class='amount'>€{reading.Amount:F2}</td>
                                </tr>
                            </table>
                        </div>
            
                        <p>Palun tasuge arve võimalikult kiiresti.</p>
            
                        <p>Kui teil on küsimusi, võtke meiega ühendust.</p>
            
                        <p>Lugupidamisega,<br>Majahaldus</p>
                    </div>
                    <div class='footer'>
                        <p>See on automaatne meeldetuletus. Palun ärge vastake sellele meilile.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
