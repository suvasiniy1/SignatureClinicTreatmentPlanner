using System.IO;
using System.Net;
using System.Net.Mail;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    [Route("Pdf/{action}/{id?}")]
    public class PdfController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly EmailSettings _emailSettings;

        public PdfController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailSettings = emailSettings.Value;
        }


        public IActionResult GeneratePdf(int id)
        {
            try
            {
                var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
                if (patient == null) return NotFound("Patient not found.");

                var clinic = _context.Clinics.FirstOrDefault(c => c.ClinicID == patient.Clinic);
                var treatment = _context.Treatments.FirstOrDefault(t => t.TreatmentID == patient.Treatment);
                var surgeon = _context.Surgeons.FirstOrDefault(s => s.SurgeonID == patient.SurgeonID);
                if (clinic == null || treatment == null || surgeon == null)
                    return NotFound("Clinic, Treatment, or Surgeon not found.");

                string surgeonName = surgeon?.SurgeonName ?? "Not Assigned";
                string formattedDateTimeForPdf = patient.Date.ToString("yyyy-MM-dd @ HH:mm");
                string formattedDateTimeForFile = patient.Date.ToString("yyyy-MM-dd_HH-mm");
                string safePatientName = patient.FirstName.Replace(" ", "_");
                string safeTreatmentName = treatment.TreatmentName.Replace(" ", "_");
                string fileName = $"TreatmentPlan_{safePatientName}_{safeTreatmentName}_{formattedDateTimeForFile}.pdf";

                // Load PDF HTML template
                string pdfTemplatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "Signature_Coverpage_Update.html");
                string htmlContent = System.IO.File.ReadAllText(pdfTemplatePath);

                // Load logo
                string localImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "header.PNG");
                string logoBase64 = System.IO.File.Exists(localImagePath) ? ConvertImageToBase64(localImagePath) : null;
                htmlContent = htmlContent.Replace("##LOGO##", !string.IsNullOrEmpty(logoBase64) ? $"data:image/png;base64,{logoBase64}" : "https://www.y1crm.com/images/header.PNG");

                // Replace placeholders in PDF HTML
                htmlContent = htmlContent
                    .Replace("##USERNAME##", patient.FirstName)
                    .Replace("##TREATMENT##", treatment.TreatmentName)
                    .Replace("##AMOUNT##", patient.Price.ToString("C2", new CultureInfo("en-GB")))
                    .Replace("##Treatment DATE&TIME##", formattedDateTimeForPdf)
                    .Replace("##SURGEON##", surgeonName)
                    .Replace("##CLINIC##", clinic.ClinicName);

                // Convert to PDF
                byte[] coverPagePdf = ConvertHtmlToPdf(htmlContent, HttpContext);

                List<byte[]> pdfsToMerge = new List<byte[]> { coverPagePdf };
                AddPdfIfExists(pdfsToMerge, "PdfTemplates/Treatments", $"{safeTreatmentName}.pdf");
                AddPdfIfExists(pdfsToMerge, "PdfTemplates/Surgeons", $"{surgeonName}.pdf");
                AddPdfIfExists(pdfsToMerge, "PdfTemplates/Clinics", $"{clinic.ClinicName}.pdf");

                byte[] finalPdf = MergePdfs(pdfsToMerge);
                string outputPath = Path.Combine(_webHostEnvironment.WebRootPath, "GeneratedPdfs", fileName);
                System.IO.File.WriteAllBytes(outputPath, finalPdf);

                // ✅ Email section
                string emailTemplatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "EmailTemplate.html");
                string emailHtml = System.IO.File.ReadAllText(emailTemplatePath)
                    .Replace("##LOGO##", !string.IsNullOrEmpty(logoBase64) ? $"data:image/png;base64,{logoBase64}" : "https://www.y1crm.com/images/header.PNG")
                    .Replace("##USERNAME##", patient.FirstName)
                    .Replace("##TREATMENT##", treatment.TreatmentName)
                    .Replace("##AMOUNT##", patient.Price.ToString("C2", new CultureInfo("en-GB")))
                    .Replace("##Treatment DATE&TIME##", formattedDateTimeForPdf)
                    .Replace("##SURGEON##", surgeonName)
                    .Replace("##CLINIC##", clinic.ClinicName);

                SendEmailWithPdfAttachment(patient.Email, "Your Signature Clinic Treatment Plan", emailHtml, finalPdf, fileName);
                return Ok();

                TempData["ToastMessage"] = "Email sent successfully!";

                // return File(finalPdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        private void SendEmailWithPdfAttachment(string toEmail, string subject, string htmlBody, byte[] pdfAttachment, string fileName)
        {
            using (var message = new MailMessage())
            {
                message.To.Add(new MailAddress(toEmail));
                message.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.DisplayName);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using (var stream = new MemoryStream(pdfAttachment))
                {
                    var attachment = new Attachment(stream, fileName, "application/pdf");
                    message.Attachments.Add(attachment);

                    using (var smtp = new SmtpClient(_emailSettings.SmtpServer))
                    {
                        smtp.Port = _emailSettings.Port;
                        smtp.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                        smtp.EnableSsl = true;
                        smtp.Send(message);
                    }
                }
            }
        }


        private string ConvertImageToBase64(string path)
        {
            try
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(path);
                return Convert.ToBase64String(imageBytes);
            }
            catch
            {
                return null;
            }
        }

        private byte[] ConvertHtmlToPdf(string htmlContent, HttpContext httpContext)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ConverterProperties properties = new ConverterProperties();
                properties.SetBaseUri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}/");
                using (PdfWriter writer = new PdfWriter(ms))
                {
                    PdfDocument pdf = new PdfDocument(writer);
                    HtmlConverter.ConvertToPdf(htmlContent, pdf, properties);
                }
                return ms.ToArray();
            }
        }

        private byte[] MergePdfs(List<byte[]> pdfs)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                PdfDocument outputPdf = new PdfDocument(new PdfWriter(ms));
                PdfMerger merger = new PdfMerger(outputPdf);
                foreach (var pdfBytes in pdfs)
                {
                    using (PdfDocument tempPdf = new PdfDocument(new PdfReader(new MemoryStream(pdfBytes))))
                    {
                        merger.Merge(tempPdf, 1, tempPdf.GetNumberOfPages());
                    }
                }
                outputPdf.Close();
                return ms.ToArray();
            }
        }

        private void AddPdfIfExists(List<byte[]> pdfList, string directory, string fileName)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, directory, fileName);
            if (System.IO.File.Exists(filePath))
            {
                pdfList.Add(System.IO.File.ReadAllBytes(filePath));
            }
        }
    }
}
