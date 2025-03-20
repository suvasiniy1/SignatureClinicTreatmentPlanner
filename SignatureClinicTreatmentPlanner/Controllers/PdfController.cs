using System.IO;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using System.Collections.Generic;
using System.Globalization;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class PdfController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PdfController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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

            // ✅ Formatted date-time for display inside the PDF
            string formattedDateTimeForPdf = patient.Date.ToString("yyyy-MM-dd @ HH:mm");

            // ✅ Formatted date-time for the filename (safe format)
            string formattedDateTimeForFile = patient.Date.ToString("yyyy-MM-dd_HH-mm");

            string safePatientName = patient.FirstName.Replace(" ", "_");
            string safeTreatmentName = treatment.TreatmentName.Replace(" ", "_");

            string fileName = $"TreatmentPlan_{safePatientName}_{safeTreatmentName}_{formattedDateTimeForFile}.pdf";

                string htmlTemplatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "Signature_Coverpage_Update.html");

                // Log the path for debugging
                Console.WriteLine($"[DEBUG] Checking template path: {htmlTemplatePath}");

                if (!System.IO.File.Exists(htmlTemplatePath))
                {
                    return NotFound($"Template file not found at: {htmlTemplatePath} (WebRootPath: {_webHostEnvironment.WebRootPath})");
                }

                string htmlContent = System.IO.File.ReadAllText(htmlTemplatePath);

                // ✅ Determine absolute path based on environment
                string localImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "header.PNG");
                string serverImagePath = "C:\\inetpub\\wwwroot\\SignatureTreatmentPlanner\\wwwroot\\images\\header.PNG";

                // ✅ Convert logo to Base64 or use fallback URL
                string logoPlaceholder = "##LOGO##";
                string logoBase64 = null;

                // Check which path is available (Local or Server)
                if (System.IO.File.Exists(localImagePath))
                {
                    logoBase64 = ConvertImageToBase64(localImagePath);
                }
                else if (System.IO.File.Exists(serverImagePath))
                {
                    logoBase64 = ConvertImageToBase64(serverImagePath);
                }

                if (!string.IsNullOrEmpty(logoBase64))
                {
                    htmlContent = htmlContent.Replace(logoPlaceholder, $"data:image/png;base64,{logoBase64}");
                }
                else
                {
                    // If image is missing in both local and server, use fallback URL
                    htmlContent = htmlContent.Replace(logoPlaceholder, "https://www.y1crm.com/images/header.PNG");
                }




                // ✅ Replace placeholders with actual values
                htmlContent = htmlContent
                .Replace("##USERNAME##", patient.FirstName)
                .Replace("##TREATMENT##", treatment.TreatmentName)
                .Replace("##AMOUNT##", patient.Price.ToString("C2", new CultureInfo("en-GB"))) // ✅ Correct currency formatting
                .Replace("##Treatment DATE&TIME##", formattedDateTimeForPdf)  // ✅ Display as "yyyy-MM-dd @ HH:mm"
                .Replace("##SURGEON##", surgeonName)
                .Replace("##CLINIC##", clinic.ClinicName);

            // ✅ Convert HTML to PDF (Passing HttpContext to fix Base URI issue)
            byte[] coverPagePdf = ConvertHtmlToPdf(htmlContent, HttpContext);

            // ✅ Prepare list of PDFs to merge
            List<byte[]> pdfsToMerge = new List<byte[]> { coverPagePdf };

            // ✅ Check and add additional PDFs if they exist
            AddPdfIfExists(pdfsToMerge, "PdfTemplates/Treatments", $"{safeTreatmentName}.pdf");
            AddPdfIfExists(pdfsToMerge, "PdfTemplates/Surgeons", $"{surgeonName}.pdf");
            AddPdfIfExists(pdfsToMerge, "PdfTemplates/Clinics", $"{clinic.ClinicName}.pdf");

            // ✅ Merge all PDFs
            byte[] finalPdf = MergePdfs(pdfsToMerge);

            // ✅ Save for debugging (Optional)
            string outputPath = Path.Combine(_webHostEnvironment.WebRootPath, "GeneratedPdfs", fileName);
                // DEBUG: Check if the folder is writable
                if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                {
                    return Content($"Error: Folder does not exist - {Path.GetDirectoryName(outputPath)}");
                }

                try
                {
                    System.IO.File.WriteAllText(outputPath + ".test", "Test Write Access");
                    System.IO.File.Delete(outputPath + ".test");
                }
                catch (Exception ex)
                {
                    return Content($"Error: Cannot write to the directory. {ex.Message}");
                }
                System.IO.File.WriteAllBytes(outputPath, finalPdf);

            return File(finalPdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        // ✅ Convert Image to Base64
        private string ConvertImageToBase64(string relativePath)
        {
            try
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (System.IO.File.Exists(imagePath))
                {
                    byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(_webHostEnvironment.WebRootPath, "logs", "error_log.txt");
                System.IO.File.AppendAllText(errorLogPath, $"Error loading image: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            return null;
        }

        /// <summary>
        /// Converts HTML content into a PDF using iText.
        /// </summary>
        private byte[] ConvertHtmlToPdf(string htmlContent, HttpContext httpContext)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ConverterProperties properties = new ConverterProperties();

                // ✅ Use the full URL as Base URI for CSS and assets (Fixes missing resources issue)
                string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/";
                properties.SetBaseUri("https://www.y1crm.com/");

                using (PdfWriter writer = new PdfWriter(ms))
                {
                    PdfDocument pdf = new PdfDocument(writer);
                    HtmlConverter.ConvertToPdf(htmlContent, pdf, properties);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Merges multiple PDFs into a single file.
        /// </summary>
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

        /// <summary>
        /// Adds a PDF to the merge list if it exists in the specified directory.
        /// </summary>
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
