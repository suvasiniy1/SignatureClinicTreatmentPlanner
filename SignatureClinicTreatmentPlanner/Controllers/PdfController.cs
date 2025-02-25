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
    var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
    if (patient == null) return NotFound();

    var clinic = _context.Clinics.FirstOrDefault(c => c.ClinicID == patient.Clinic);
    var treatment = _context.Treatments.FirstOrDefault(t => t.TreatmentID == patient.Treatment);
    var surgeon = _context.Surgeons.FirstOrDefault(s => s.SurgeonID == patient.SurgeonID);

    if (clinic == null || treatment == null || surgeon == null)
        return NotFound("Clinic, Treatment, or Surgeon not found.");

    string surgeonName = surgeon != null ? surgeon.SurgeonName : "Not Assigned";

    // ✅ 1. Keep this format for display inside the PDF
    string formattedDateTimeForPdf = patient.Date.ToString("yyyy-MM-dd @ HH:mm");

    // ✅ 2. Use a safe format for the filename
    string formattedDateTimeForFile = patient.Date.ToString("yyyy-MM-dd_HH-mm"); 

    string safePatientName = patient.FirstName.Replace(" ", "_"); 
    string safeTreatmentName = treatment.TreatmentName.Replace(" ", "_");

    string fileName = $"TreatmentPlan_{safePatientName}_{safeTreatmentName}_{formattedDateTimeForFile}.pdf";

    // Read and replace placeholders in the HTML template
    string htmlTemplatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "Signature_Coverpage_Update.html");
    string htmlContent = System.IO.File.ReadAllText(htmlTemplatePath);

        
            htmlContent = htmlContent
        .Replace("##USERNAME##", patient.FirstName)
        .Replace("##TREATMENT##", treatment.TreatmentName)
       .Replace("##AMOUNT##", patient.Price.ToString("C2", new System.Globalization.CultureInfo("en-GB"))) // ✅ Correct formatting
        .Replace("##Treatment DATE&TIME##", formattedDateTimeForPdf)  // ✅ Show as "yyyy-MM-dd @ HH:mm"
        .Replace("##SURGEON##", surgeonName)
        .Replace("##CLINIC##", clinic.ClinicName);

    // Convert HTML to PDF
    byte[] coverPagePdf = ConvertHtmlToPdf(htmlContent);

    // Prepare list of PDFs to merge
    List<byte[]> pdfsToMerge = new List<byte[]> { coverPagePdf };

    string treatmentPdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "PdfTemplates/Treatments", $"{safeTreatmentName}.pdf");
    if (System.IO.File.Exists(treatmentPdfPath))
    {
        pdfsToMerge.Add(System.IO.File.ReadAllBytes(treatmentPdfPath));
    }

    string surgeonPdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "PdfTemplates/Surgeons", $"{surgeonName}.pdf");
    if (System.IO.File.Exists(surgeonPdfPath))
    {
        pdfsToMerge.Add(System.IO.File.ReadAllBytes(surgeonPdfPath));
    }

    string clinicPdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "PdfTemplates/Clinics", $"{clinic.ClinicName}.pdf");
    if (System.IO.File.Exists(clinicPdfPath))
    {
        pdfsToMerge.Add(System.IO.File.ReadAllBytes(clinicPdfPath));
    }

    // Merge all PDFs
    byte[] finalPdf = MergePdfs(pdfsToMerge);

    // Save for debugging (optional)
    string outputPath = Path.Combine(_webHostEnvironment.WebRootPath, "GeneratedPdfs", fileName);
    System.IO.File.WriteAllBytes(outputPath, finalPdf);

    return File(finalPdf, "application/pdf", fileName);
}


        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ConverterProperties properties = new ConverterProperties();

                // Fix: Use a relative base URI that works in a web environment
                properties.SetBaseUri(_webHostEnvironment.WebRootPath);

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
    }
}
