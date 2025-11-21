using Microsoft.AspNetCore.Mvc;
using CMCSPart2.Data;
using CMCSPart2.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSPart2.Controllers
{
    public class FilesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly FileEncryptionService _fs;
        public FilesController(AppDbContext db, FileEncryptionService fs) { _db = db; _fs = fs; }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _db.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var bytes = await _fs.DecryptAsync(doc.FilePath, doc.EncryptionIVBase64);
            return File(bytes, doc.FileType, fileDownloadName: doc.FileName);
        }
    }
}


